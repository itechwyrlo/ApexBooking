using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfilePhoto
{
    // Content-type and size are validated by the controller before this command is ever
    // dispatched (see AccountController.UploadMyProfilePhoto) — FluentValidation has no natural
    // way to inspect a raw Stream, so that check happens at the edge instead of here.
    public record UpdateMyProfilePhotoCommand(
        Stream Content,
        string ContentType,
        string FileExtension
    ) : ICommand<string>;
}
