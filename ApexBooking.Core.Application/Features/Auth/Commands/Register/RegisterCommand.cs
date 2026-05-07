using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Auth.Commands.Register
{
    public sealed record RegisterAdminCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string OrganizationName,
    string Industry,
    string Phone,
    string Country) : ICommand<BaseResponse<AuthResponseDto>>;

}