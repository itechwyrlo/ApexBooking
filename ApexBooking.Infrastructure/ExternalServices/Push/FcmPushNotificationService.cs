using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Infrastructure.Configuration;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApexBooking.Infrastructure.ExternalServices.Push;

public class FcmPushNotificationService : IPushNotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FcmPushNotificationService> _logger;
    private readonly bool _initialized;

    public FcmPushNotificationService(
        IUnitOfWork unitOfWork,
        ILogger<FcmPushNotificationService> logger,
        IOptions<FirebaseSettings> firebaseSettings)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;

        var settings = firebaseSettings.Value;

        if (string.IsNullOrWhiteSpace(settings.ServiceAccountJson))
        {
            _logger.LogWarning("Firebase ServiceAccountJson is not configured — FCM push notifications are disabled.");
            _initialized = false;
            return;
        }

        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(settings.ServiceAccountJson)
            });
        }

        _initialized = true;
    }

    public async Task SendAsync(
        IReadOnlyList<Guid> recipientIds,
        string title,
        string message,
        string eventType,
        CancellationToken ct)
    {
        if (!_initialized)
            return;

        foreach (var recipientId in recipientIds)
        {
            try
            {
                var tokens = await _unitOfWork.FcmTokenRepository.GetTokensByUserIdAsync(recipientId, ct);
                if (tokens.Count == 0)
                    continue;

                var multicastMessage = new MulticastMessage
                {
                    Tokens = tokens.ToList(),
                    Notification = new Notification { Title = title, Body = message },
                    Data = new Dictionary<string, string> { ["eventType"] = eventType }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicastMessage, ct);

                for (var i = 0; i < response.Responses.Count; i++)
                {
                    var sendResponse = response.Responses[i];
                    if (sendResponse.IsSuccess)
                        continue;

                    var errorCode = sendResponse.Exception?.MessagingErrorCode;
                    if (errorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                    {
                        try
                        {
                            await _unitOfWork.FcmTokenRepository.DeleteByUserIdAsync(recipientId, tokens[i], ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete stale FCM token for user {UserId}.", recipientId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "FCM send failed for user {UserId} with error code {ErrorCode}: {Message}",
                            recipientId,
                            errorCode,
                            sendResponse.Exception?.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM push notification failed for user {UserId}.", recipientId);
            }
        }
    }
}
