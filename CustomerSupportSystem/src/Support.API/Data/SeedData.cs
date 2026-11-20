using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Support.Shared.Enums;
using Support.Shared.Models;

namespace Support.API.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["BootstrapAdmin:Password"];
        var fullName = configuration["BootstrapAdmin:FullName"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        if (await context.Users.AnyAsync(x => x.Email == email)) return;

        var admin = new User
        {
            FullName = string.IsNullOrWhiteSpace(fullName) ? "System Administrator" : fullName.Trim(),
            Email = email,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, password);
        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
