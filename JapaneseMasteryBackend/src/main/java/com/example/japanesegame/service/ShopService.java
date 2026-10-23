package com.example.japanesegame.service;

import com.example.japanesegame.entity.ShopItem;
import com.example.japanesegame.entity.ShopItemType;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.ShopItemRepository;
import com.example.japanesegame.repository.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.Optional;

@Service
public class ShopService {

    @Autowired
    private CloudinaryService cloudinaryService;

    @Autowired
    private ShopItemRepository shopItemRepository;

    @Autowired
    private UserRepository userRepository;

    public ShopItem uploadItemToShop(String name, ShopItemType type, Integer price, Long designerId, MultipartFile file) throws IOException {
        // Upload image to Cloudinary
        Map uploadResult = cloudinaryService.uploadFile(file);
        
        String secureUrl = (String) uploadResult.get("secure_url");
        String publicId = (String) uploadResult.get("public_id");

        User designer = userRepository.findById(designerId)
                .orElseThrow(() -> new RuntimeException("Designer not found"));

        // Save item in the database
        ShopItem item = new ShopItem();
        item.setName(name);
        item.setType(type);
        item.setPrice(price);
        item.setDesigner(designer);
        item.setCloudinaryPublicId(publicId);
        item.setSecureUrl(secureUrl);
        item.setApproved(true); // Default to approved since moderator role is disabled

        return shopItemRepository.save(item);
    }

    public List<ShopItem> getAllItems() {
        return shopItemRepository.findAll();
    }

    public List<ShopItem> getDesignerItems(Long designerId) {
        return shopItemRepository.findByDesignerIdOrderByIdDesc(designerId);
    }

    public String getOptimizedItemUrlForPlayer(Long itemId) {
        Optional<ShopItem> itemOpt = shopItemRepository.findById(itemId);
        if (itemOpt.isPresent()) {
            return cloudinaryService.generateOptimizedUrl(itemOpt.get().getCloudinaryPublicId());
        }
        throw new RuntimeException("Shop item not found!");
    }

    public User buyItem(Long userId, Long itemId) {
        User user = userRepository.findById(userId)
                .orElseThrow(() -> new RuntimeException("User not found"));
        ShopItem item = shopItemRepository.findById(itemId)
                .orElseThrow(() -> new RuntimeException("Shop item not found"));

        if (user.getPurchasedItems().contains(item)) {
            throw new RuntimeException("You already own this item!");
        }

        if (user.getGCoin() < item.getPrice()) {
            throw new RuntimeException("Not enough G-Coins!");
        }

        // Deduct G-Coins and add to purchased
        user.setGCoin(user.getGCoin() - item.getPrice());
        user.getPurchasedItems().add(item);

        return userRepository.save(user);
    }

    public User applyItem(Long userId, Long itemId) {
        User user = userRepository.findById(userId)
                .orElseThrow(() -> new RuntimeException("User not found"));
        ShopItem item = shopItemRepository.findById(itemId)
                .orElseThrow(() -> new RuntimeException("Shop item not found"));

        if (!user.getPurchasedItems().contains(item)) {
            throw new RuntimeException("You must buy this item first!");
        }

        if (item.getType() == ShopItemType.THEME) {
            user.setActiveTheme(item);
        }

        return userRepository.save(user);
    }
}
