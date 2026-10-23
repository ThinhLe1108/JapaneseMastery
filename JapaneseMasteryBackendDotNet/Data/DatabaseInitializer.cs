using JapaneseMasteryBackendDotNet.Models;
using Microsoft.EntityFrameworkCore;

namespace JapaneseMasteryBackendDotNet.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            // Ensure schema compatibility
            var sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'pvp_ban_until')
    ALTER TABLE users ADD pvp_ban_until DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'active_theme_id')
    ALTER TABLE users ADD active_theme_id BIGINT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'active_background_id')
    ALTER TABLE users ADD active_background_id BIGINT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('shop_items') AND name = 'cloudinary_public_id')
    ALTER TABLE shop_items ADD cloudinary_public_id VARCHAR(255) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('shop_items') AND name = 'secure_url')
    ALTER TABLE shop_items ADD secure_url VARCHAR(500) NULL;

IF OBJECT_ID('user_purchased_items', 'U') IS NULL
BEGIN
    CREATE TABLE user_purchased_items (
        user_id BIGINT NOT NULL,
        item_id BIGINT NOT NULL,
        PRIMARY KEY (user_id, item_id),
        FOREIGN KEY (user_id) REFERENCES users(id),
        FOREIGN KEY (item_id) REFERENCES shop_items(id)
    );
END
";
            await context.Database.ExecuteSqlRawAsync(sql);

            // Seed admin account if it does not exist
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (adminUser == null)
            {
                var admin = new User
                {
                    Username = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("123"),
                    Role = Role.ADMIN,
                    Level = 99,
                    GCoin = 9999,
                    DisplayName = "System Admin"
                };
                context.Users.Add(admin);
                await context.SaveChangesAsync();
                logger.LogInformation("Default ADMIN account created: admin / 123");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
        }
    }
}

