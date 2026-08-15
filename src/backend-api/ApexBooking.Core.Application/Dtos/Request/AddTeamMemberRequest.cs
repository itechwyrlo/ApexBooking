using System;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos.Request
{
    public record AddTeamMemberRequest(Guid branchId,
    string email,
    string firstName,
    string lastName,
    string? contactNumber,
    SystemRole role,
    string customJobTitle);
}