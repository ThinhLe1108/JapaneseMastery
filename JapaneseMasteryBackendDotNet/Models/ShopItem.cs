using System.Text.Json.Serialization;

namespace JapaneseMasteryBackendDotNet.Models;

public class ShopItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ShopItemType Type { get; set; } = ShopItemType.THEME;
    public int Price { get; set; }
    public string? CloudinaryPublicId { get; set; }
    public string? SecureUrl { get; set; }

    public long? DesignerId { get; set; }

    [JsonIgnore]
    public User? Designer { get; set; }

    public bool IsApproved { get; set; } = true;
}

