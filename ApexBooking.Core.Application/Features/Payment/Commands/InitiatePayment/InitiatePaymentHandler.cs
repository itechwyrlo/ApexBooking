using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Payment.Commands.InitiatePayment;

internal sealed class InitiatePaymentHandler
    : ICommandHandler<InitiatePaymentCommand, InitiatePaymentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppUrlService _appUrlService;

    public InitiatePaymentHandler(
        IUnitOfWork unitOfWork,
        IAppUrlService appUrlService)
    {
        _unitOfWork = unitOfWork;
        _appUrlService = appUrlService;
    }

    public async Task<InitiatePaymentDto> Handle(
        InitiatePaymentCommand command,
        CancellationToken ct)
    {
        var booking = await _unitOfWork.BookingRepository
            .GetByIdAsync(new BookingId(command.BookingId));

        if (booking is null)
            throw new NotFoundException("Booking not found.");

        booking.EnsureAwaitingPayment();

        var platformGateway = await _unitOfWork.PlatformPaymentGatewayRepository.GetActiveAsync(ct);

        if (platformGateway is null)
            throw new NotFoundException("No active payment gateway configured.");

        var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(booking.TenantId);

        if (tenant is null)
            throw new NotFoundException("Tenant not found.");

        var returnUrl = _appUrlService.GetPaymentReturnUrl(tenant.Slug, booking.BookingId.Value);
        var cancelUrl = _appUrlService.GetPaymentCancelUrl(tenant.Slug, booking.BookingId.Value);

        // Subordinate to known infrastructure violation: HTTP calls from Application layer
        var accessToken = await GetPayPalAccessTokenAsync(
            platformGateway.ClientId,
            platformGateway.SecretKeyEncrypted,
            platformGateway.Mode);

        if (accessToken is null)
            throw new InvalidOperationException("Failed to authenticate with PayPal.");

        var baseUrl = platformGateway.Mode == GatewayMode.Live
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        var orderId = await CreatePayPalOrderAsync(
            accessToken,
            baseUrl,
            booking.PriceSnapshot,
            booking.CurrencyCode,
            booking.BookingReference,
            returnUrl,
            cancelUrl);

        if (orderId is null)
            throw new InvalidOperationException("Failed to create PayPal order.");

        var approvalUrl = await GetApprovalUrlAsync(accessToken, baseUrl, orderId);

        if (approvalUrl is null)
            throw new InvalidOperationException("Failed to retrieve PayPal approval URL.");

        var transaction = PaymentTransaction.Create(
            booking.TenantId,
            booking.BookingId,
            GatewayProvider.PayPal,
            orderId,
            booking.PriceSnapshot,
            booking.CurrencyCode);

        _unitOfWork.PaymentTransactionRepository.Add(transaction);
        await _unitOfWork.CompleteAsync(ct);

        return new InitiatePaymentDto(
            ApprovalUrl: approvalUrl,
            GatewayTransactionId: orderId,
            BookingReference: booking.BookingReference);
    }

    private static async Task<string?> GetPayPalAccessTokenAsync(
        string clientId,
        string secretKey,
        GatewayMode mode)
    {
        var baseUrl = mode == GatewayMode.Live
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        using var client = new HttpClient();
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{secretKey}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        try
        {
            var response = await client.PostAsync($"{baseUrl}/v1/oauth2/token", content);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> CreatePayPalOrderAsync(
        string accessToken,
        string baseUrl,
        decimal amount,
        string currencyCode,
        string bookingReference,
        string returnUrl,
        string cancelUrl)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = bookingReference,
                    amount = new
                    {
                        currency_code = currencyCode,
                        value = amount.ToString("F2")
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        try
        {
            var response = await client.PostAsync(
                $"{baseUrl}/v2/checkout/orders",
                new StringContent(
                    JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetString();
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> GetApprovalUrlAsync(
        string accessToken,
        string baseUrl,
        string orderId)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await client.GetAsync($"{baseUrl}/v2/checkout/orders/{orderId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            foreach (var link in doc.RootElement.GetProperty("links").EnumerateArray())
            {
                if (link.GetProperty("rel").GetString() == "approve")
                    return link.GetProperty("href").GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
