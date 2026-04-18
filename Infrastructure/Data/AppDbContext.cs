using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
