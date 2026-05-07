using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.WebApi.Dtos
{
    public record RegisterAdminRequestDto(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string OrganizationName,
        string Industry,
        string Phone,
        string Country);
}