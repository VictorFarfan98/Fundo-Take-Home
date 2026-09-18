using Fundo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Infrastructure;

public sealed class FundoDbContext(DbContextOptions<FundoDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);
            entity.HasIndex(customer => customer.NormalizedSsn).IsUnique();
            entity.Property(customer => customer.NormalizedSsn).HasMaxLength(9).IsRequired();
            entity.Property(customer => customer.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(customer => customer.LastName).HasMaxLength(100).IsRequired();
            entity.Property(customer => customer.Address).HasMaxLength(300).IsRequired();
            entity.Property(customer => customer.State).HasMaxLength(2).IsRequired();
            entity.Property(customer => customer.CompanyName).HasMaxLength(200).IsRequired();
            entity.HasOne(customer => customer.Application)
                .WithOne(application => application.Customer)
                .HasForeignKey<LoanApplication>(application => application.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoanApplication>(entity =>
        {
            entity.HasKey(application => application.Id);
            entity.HasIndex(application => application.CustomerId).IsUnique();
            entity.Property(application => application.RequestedAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Operation).HasMaxLength(10).IsRequired();
            entity.Property(message => message.Payload).IsRequired();
            entity.Property(message => message.LastError).HasMaxLength(1000);
        });
    }
}
