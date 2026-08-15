using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Features.Account.Commands.ChangeMyPassword;
using ApexBooking.Core.Application.Features.Account.Commands.RemoveMyProfilePhoto;
using ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfile;
using ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfilePhoto;
using ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    // No policy restriction beyond being authenticated at all — unlike TenantController
    // ([Authorize(Policy = "ManagementOnly")], which sits behind TenantMiddleware's tenant
    // resolution), this controller must also serve SuperAdmin, who belongs to no tenant.
    [ApiController]
    [Route("api/account")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class AccountController : ControllerBase
    {
        private static readonly string[] AllowedPhotoContentTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(MyProfileDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetMyProfileQuery(), ct);
            return Ok(result);
        }

        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileCommand command, CancellationToken ct)
        {
            await _mediator.Send(command, ct);
            return NoContent();
        }

        [HttpPost("me/photo")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // { photoUrl: string }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadMyProfilePhoto([FromForm] IFormFile photo, CancellationToken ct)
        {
            if (photo is null || photo.Length == 0)
                return Problem(title: "Validation Error", detail: "A photo file is required.", statusCode: StatusCodes.Status400BadRequest);

            if (photo.Length > MaxPhotoSizeBytes)
                return Problem(title: "Validation Error", detail: "Photo must be 5MB or smaller.", statusCode: StatusCodes.Status400BadRequest);

            if (Array.IndexOf(AllowedPhotoContentTypes, photo.ContentType) < 0)
                return Problem(title: "Validation Error", detail: "Photo must be a JPEG, PNG, or WebP image.", statusCode: StatusCodes.Status400BadRequest);

            var extension = photo.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg",
            };

            await using var stream = photo.OpenReadStream();
            var photoUrl = await _mediator.Send(new UpdateMyProfilePhotoCommand(stream, photo.ContentType, extension), ct);

            return Ok(new { photoUrl });
        }

        [HttpDelete("me/photo")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RemoveMyProfilePhoto(CancellationToken ct)
        {
            await _mediator.Send(new RemoveMyProfilePhotoCommand(), ct);
            return NoContent();
        }

        [HttpPost("me/change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangeMyPasswordCommand command, CancellationToken ct)
        {
            await _mediator.Send(command, ct);
            return NoContent();
        }
    }
}
