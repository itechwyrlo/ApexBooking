using ApexBooking.Core.Domain.ValueObjects;

namespace ApexBooking.Core.Domain.Entities
{
    public class MemberService
    {
        public TenantMemberId TenantMemberId { get; private set; } = default!;
        public virtual TenantMember TenantMember { get; set; } = null!;

        public ServiceId ServiceId { get; private set; } = default!;
        public virtual Service Service { get; private set; } = null!;

        protected MemberService() { }

        internal static MemberService Create(TenantMemberId tenantMemberId, ServiceId serviceId)
        {
            return new MemberService
            {
                TenantMemberId = tenantMemberId,
                ServiceId = serviceId,
            };
        }
    }
}