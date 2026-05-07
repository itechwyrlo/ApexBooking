using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class UserResourceAssignmentConfiguration : IEntityTypeConfiguration<UserResourceAssignment>
    {
        public void Configure(EntityTypeBuilder<UserResourceAssignment> builder)
        {
            builder.ToTable("user_resource_assignment");

            builder.HasKey(x => x.UserResourceAssignmentId);

            builder.Property(x => x.UserResourceAssignmentId)
                .HasConversion(id => id.Value, v => new UserResourceAssignmentId(v));

            builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

            builder.Property(x => x.ResourceId)
                .HasConversion(id => id.Value, v => new ResourceId(v));

            builder.Property(x => x.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v));

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");

            builder.HasIndex(x => new { x.UserId, x.ResourceId }).IsUnique();
        }
    }
}
