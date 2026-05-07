using ApexBooking.Core.Domain.Entities;
using ApexBooking.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Application.Features.Auth.Specifications
{
    public class UserWithNestedEntitiesSpecification : BaseSpecification<User>
    {
        public UserWithNestedEntitiesSpecification(Guid userId)
        {
            Criteria = user => user.Id == userId;
            
            // Include RefreshTokens
            AddInclude(user => user
                .Include(u => u.RefreshTokens));
        }
    }
}
