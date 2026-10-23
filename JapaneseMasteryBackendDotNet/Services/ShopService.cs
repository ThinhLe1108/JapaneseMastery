using JapaneseMasteryBackendDotNet.Data;
using JapaneseMasteryBackendDotNet.Models;
using Microsoft.EntityFrameworkCore;

namespace JapaneseMasteryBackendDotNet.Services;

public class ShopService
{
    private readonly AppDbContext _context;
    private readonly CloudinaryService _cloudinaryService;

    public ShopService(AppDbContext context, CloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<ShopItem> UploadItemToShopAsync(string name, ShopItemType type, int price, long designerId, IFormFile file)
    {
        var (secureUrl, publicId) = await _cloudinaryService.UploadFileAsync(file);

        var designer = await _context.Users.FindAsync(designerId);
        if (designer == null)
        {
            throw new InvalidOperationException("Designer not found");
        }

        var item = new ShopItem
        {
            Name = name,
            Type = type,
            Price = price,
            DesignerId = designerId,
            CloudinaryPublicId = publicId,
            SecureUrl = secureUrl,
            IsApproved = true
        };

        _context.ShopItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<List<ShopItem>> GetAllItemsAsync()
    {
        return await _context.ShopItems
            .Include(i => i.Designer)
            .ToListAsync();
    }

    public async Task<List<ShopItem>> GetDesignerItemsAsync(long designerId)
    {
        return await _context.ShopItems
            .Where(i => i.DesignerId == designerId)
            .OrderByDescending(i => i.Id)
            .ToListAsync();
    }

    public async Task<string> GetOptimizedItemUrlForPlayerAsync(long itemId)
    {
        var item = await _context.ShopItems.FindAsync(itemId);
        if (item == null)
        {
            throw new InvalidOperationException("Shop item not found!");
        }

        return _cloudinaryService.GenerateOptimizedUrl(item.CloudinaryPublicId);
    }

    public async Task<User> BuyItemAsync(long userId, long itemId)
    {
        var user = await _context.Users
            .Include(u => u.PurchasedItems)
            .Include(u => u.UserLearnedWords)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var item = await _context.ShopItems.FindAsync(itemId);
        if (item == null)
        {
            throw new InvalidOperationException("Shop item not found");
        }

        if (user.PurchasedItems.Any(i => i.Id == itemId))
        {
            throw new InvalidOperationException("You already own this item!");
        }

        if (user.GCoin < item.Price)
        {
            throw new InvalidOperationException("Not enough G-Coins!");
        }

        user.GCoin -= item.Price;
        user.PurchasedItems.Add(item);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User> ApplyItemAsync(long userId, long itemId)
    {
        var user = await _context.Users
            .Include(u => u.PurchasedItems)
            .Include(u => u.ActiveTheme)
            .Include(u => u.ActiveBackground)
            .Include(u => u.UserLearnedWords)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var item = await _context.ShopItems.FindAsync(itemId);
        if (item == null)
        {
            throw new InvalidOperationException("Shop item not found");
        }

        if (!user.PurchasedItems.Any(i => i.Id == itemId))
        {
            throw new InvalidOperationException("You must buy this item first!");
        }

        if (item.Type == ShopItemType.THEME)
        {
            user.ActiveThemeId = item.Id;
            user.ActiveTheme = item;
        }

        await _context.SaveChangesAsync();
        return user;
    }
}

