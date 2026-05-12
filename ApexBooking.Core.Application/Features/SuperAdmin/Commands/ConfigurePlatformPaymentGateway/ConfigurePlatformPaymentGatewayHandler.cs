using System.Net.Http.Headers;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ConfigurePlatformPaymentGateway;

internal sealed class ConfigurePlatformPaymentGatewayHandler
    : ICommandHandler<ConfigurePlatformPaymentGatewayCommand, BaseResponse<PlatformPaymentGatewayDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ConfigurePlatformPaymentGatewayHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<PlatformPaymentGatewayDto>> Handle(
        ConfigurePlatformPaymentGatewayCommand command,
        CancellationToken ct)
    {
        var isValid = await ValidatePayPalCredentialsAsync(command.ClientId, command.SecretKey, command.Mode);
        if (!isValid)
            return BaseResponse<PlatformPaymentGatewayDto>.Failure(
                "Gateway credentials are invalid. Please check your PayPal Client ID and Secret.",
                "GATEWAY_CREDENTIALS_INVALID");

        var existing = await _unitOfWork.PlatformPaymentGatewayRepository.GetActiveAsync(ct);
        if (existing is not null)
            existing.Deactivate();

        var gateway = PlatformPaymentGateway.Create(
            command.GatewayProvider,
            command.ClientId,
            command.SecretKey,
            command.WebhookId,
            command.Mode);

        gateway.MarkValidated();

        if (existing is not null)
            _unitOfWork.PlatformPaymentGatewayRepository.Update(existing);

        _unitOfWork.PlatformPaymentGatewayRepository.Add(gateway);
        await _unitOfWork.CompleteAsync(ct);

        return BaseResponse<PlatformPaymentGatewayDto>.Success(gateway.ToPlatformPaymentGatewayDto());
    }

    private static async Task<bool> ValidatePayPalCredentialsAsync(
        string clientId,
        string secretKey,
        GatewayMode mode)
    {
        var baseUrl = mode == GatewayMode.Live
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        using var client = new HttpClient();
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{clientId}:{secretKey}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        try
        {
            var response = await client.PostAsync($"{baseUrl}/v1/oauth2/token", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
