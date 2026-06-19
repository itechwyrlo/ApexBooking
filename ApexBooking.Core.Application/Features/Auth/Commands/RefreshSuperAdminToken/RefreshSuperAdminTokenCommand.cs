using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.RefreshSuperAdminToken;

public record RefreshSuperAdminTokenCommand() : ICommand<RefreshSuperAdminTokenResponseDto>;
