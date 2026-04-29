using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FinanceTracker.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Income>(e =>
        {
            e.ToTable("incomes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
            e.Property(x => x.Source).HasColumnName("source").IsRequired();
            e.Property(x => x.Description).HasColumnName("description").IsRequired();
            e.Property(x => x.Date).HasColumnName("date").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

            // Immutable columns: never update after insert.
            e.Property(x => x.UserId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();
            e.Property(x => x.CreatedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.ToTable("expenses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
            e.Property(x => x.Category).HasColumnName("category").IsRequired();
            e.Property(x => x.Description).HasColumnName("description").IsRequired();
            e.Property(x => x.Date).HasColumnName("date").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

            e.Property(x => x.UserId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();
            e.Property(x => x.CreatedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        });

        modelBuilder.Entity<SavingsGoal>(e =>
        {
            e.ToTable("savings_goals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.TargetAmount).HasColumnName("target_amount").HasColumnType("numeric(18,2)").IsRequired();
            e.Property(x => x.CurrentAmount).HasColumnName("current_amount").HasColumnType("numeric(18,2)").IsRequired();
            e.Property(x => x.Deadline).HasColumnName("deadline");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Ignore(x => x.RemainingAmount);
            e.Ignore(x => x.Progress);

            e.Property(x => x.UserId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();
            e.Property(x => x.CreatedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        });

        modelBuilder.Entity<Loan>(e =>
        {
            e.ToTable("loans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.CounterpartyName).HasColumnName("counterparty_name").IsRequired();
            e.Property(x => x.CounterpartyPhoneNumber).HasColumnName("counterparty_phone_number");
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
            e.Property(x => x.AmountRepaid).HasColumnName("amount_repaid").HasColumnType("numeric(18,2)").IsRequired();
            e.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired();
            e.Property(x => x.Description).HasColumnName("description").IsRequired();
            e.Property(x => x.Date).HasColumnName("date").IsRequired();
            e.Property(x => x.DueDate).HasColumnName("due_date");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            e.Ignore(x => x.RemainingBalance);
            e.Ignore(x => x.IsSettled);

            e.Property(x => x.UserId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();
            e.Property(x => x.CreatedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        });
    }
}
