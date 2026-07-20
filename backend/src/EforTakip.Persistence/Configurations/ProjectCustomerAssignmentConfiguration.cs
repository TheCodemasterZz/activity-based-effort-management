using EforTakip.Domain.Customers;
using EforTakip.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class ProjectCustomerAssignmentConfiguration : IEntityTypeConfiguration<ProjectCustomerAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectCustomerAssignment> builder)
    {
        builder.ToTable("ProjectCustomers");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.ProjectId, a.CustomerId }).IsUnique();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
