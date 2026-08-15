using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
    {
        public void Configure(EntityTypeBuilder<TenantMember> builder)
        {
            // 1. Table and Primary Key Mapping
            builder.ToTable("TenantMembers");
            
            builder.HasKey(tm => tm.TenantMemberId);

            // 2. Aggregate Root ID Conversions
            builder.Property(tm => tm.TenantMemberId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantMemberId(value))
                .IsRequired();

            builder.Property(tm => tm.TenantId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantId(value))
                .IsRequired();

            builder.Property(tm => tm.BranchId)
                .HasConversion(
                    id => id.Value,
                    value => new BranchId(value))
                .IsRequired()
                .HasColumnName("branch_id");

            builder.HasIndex(tm => new { tm.TenantId, tm.BranchId });

            // 3. Property Constraints and Requirements
            builder.Property(tm => tm.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(tm => tm.LastName).HasMaxLength(100).IsRequired();
            builder.Property(tm => tm.Email).HasMaxLength(255).IsRequired();
            builder.Property(tm => tm.ContactNumber).HasMaxLength(30).IsRequired();
            builder.Property(tm => tm.CustomJobTitle).HasMaxLength(100);
            builder.Property(tm => tm.PhotoUrl).HasMaxLength(2048);

            // 4. Enums Mapping
            builder.Property(tm => tm.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(tm => tm.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

            // 5. Audit & Ignores
            builder.Property(tm => tm.CreatedAt).IsRequired();
            builder.Property(tm => tm.UpdatedAt).IsRequired();
            builder.Ignore(tm => tm.IsActive);
            builder.Ignore(tm => tm.DomainEvents);

            // 6. Navigation Backing Fields Mapping
            builder.HasMany(tm => tm.MemberServices)
                .WithOne(ms => ms.TenantMember)
                .HasForeignKey(ms => ms.TenantMemberId)
                .Metadata.PrincipalToDependent?.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(tm => tm.Appointments)
                .WithOne()
                .HasForeignKey(b => b.StaffId)
                .OnDelete(DeleteBehavior.Restrict)
                .Metadata.PrincipalToDependent?.SetPropertyAccessMode(PropertyAccessMode.Field);

            // 7. Owned Collections with Value Object ID Conversions
            
            // Weekly Schedule (Uses a composite shadow primary key using parent ID + DayOfWeek enum)
            builder.OwnsMany(tm => tm.WeeklySchedule, scheduleBuilder =>
            {
                scheduleBuilder.ToTable("TenantMemberWeeklySchedules");
                scheduleBuilder.WithOwner().HasForeignKey("TenantMemberId");
                scheduleBuilder.HasKey("TenantMemberId", "DayOfWeek"); 
                
                scheduleBuilder.Property(s => s.DayOfWeek).HasConversion<string>().HasMaxLength(15).IsRequired();
                scheduleBuilder.Property(s => s.StartTime).IsRequired();
                scheduleBuilder.Property(s => s.EndTime).IsRequired();
                scheduleBuilder.Property(s => s.IsOff).IsRequired();
            });

            // Staff Breaks (Uses StaffBreakId Value Object)
            builder.OwnsMany(tm => tm.Breaks, breakBuilder =>
            {
                breakBuilder.ToTable("TenantMemberBreaks");
                breakBuilder.WithOwner().HasForeignKey("TenantMemberId");
                
                // Critical Value Object conversion for the Child ID
                breakBuilder.Property(b => b.Id)
                    .HasConversion(
                        id => id.Value,
                        value => new StaffBreakId(value))
                    .IsRequired();
                    
                breakBuilder.HasKey(b => b.Id);
                breakBuilder.Property(b => b.Name).HasMaxLength(100).IsRequired();
                breakBuilder.Property(b => b.StartTime).IsRequired();
                breakBuilder.Property(b => b.EndTime).IsRequired();
            });

            // Time Off Requests (Uses StaffTimeOffRequestId Value Object)
            builder.OwnsMany(tm => tm.TimeOffRequests, timeOffBuilder =>
            {
                timeOffBuilder.ToTable("TenantMemberTimeOffRequests");
                timeOffBuilder.WithOwner().HasForeignKey("TenantMemberId");
                
                // Critical Value Object conversion for the Child ID
                timeOffBuilder.Property(t => t.Id)
                    .HasConversion(
                        id => id.Value,
                        value => new StaffTimeOffRequestId(value))
                    .IsRequired();

                timeOffBuilder.HasKey(t => t.Id);
                timeOffBuilder.Property(t => t.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
                timeOffBuilder.Property(t => t.StartDate).IsRequired();
                timeOffBuilder.Property(t => t.EndDate).IsRequired();
                timeOffBuilder.Property(t => t.StartTime);
                timeOffBuilder.Property(t => t.EndTime);
                timeOffBuilder.Property(t => t.Reason).HasMaxLength(500);
            });
        }
    }
} // Close namespace block
