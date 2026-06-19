package com.example.japanesegame.controller;

import com.example.japanesegame.entity.ShopItem;
import com.example.japanesegame.entity.ShopItemType;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.service.ShopService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.multipart.MultipartFile;

import java.util.List;

@RestController
@RequestMapping("/api/shop")
public class ShopController {

    @Autowired
    private ShopService shopService;

    // [Designer Action] Upload a new item to the shop
    @PostMapping("/items")
    public ResponseEntity<ShopItem> uploadItem(
            @RequestParam("name") String name,
            @RequestParam("type") ShopItemType type,
            @RequestParam("price") Integer price,
            @RequestParam("designerId") Long designerId,
            @RequestParam("file") MultipartFile file) {
        try {
            ShopItem item = shopService.uploadItemToShop(name, type, price, designerId, file);
            return ResponseEntity.ok(item);
        } catch (Exception e) {
            return ResponseEntity.internalServerError().build();
        }
    }

    // [Player Action] Get all available items in the shop
    @GetMapping("/items")
    public ResponseEntity<List<ShopItem>> getItems() {
        return ResponseEntity.ok(shopService.getAllItems());
    }

    // [Designer Action] Get all items uploaded by a specific designer
    @GetMapping("/items/designer/{designerId}")
    public ResponseEntity<List<ShopItem>> getDesignerItems(@PathVariable Long designerId) {
        return ResponseEntity.ok(shopService.getDesignerItems(designerId));
    }

    // [Player Action] Get optimized URL for a purchased item
    @GetMapping("/items/{id}/use")
    public ResponseEntity<String> useItem(@PathVariable Long id) {
        try {
            String optimizedUrl = shopService.getOptimizedItemUrlForPlayer(id);
            return ResponseEntity.ok(optimizedUrl);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    // [Player Action] Buy an item and deduct G-Coins
    @PostMapping("/items/{itemId}/buy")
    public ResponseEntity<?> buyItem(@RequestParam Long userId, @PathVariable Long itemId) {
        try {
            User updatedUser = shopService.buyItem(userId, itemId);
            return ResponseEntity.ok(updatedUser);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    // [Player Action] Apply a purchased item
    @PostMapping("/items/{itemId}/apply")
    public ResponseEntity<?> applyItem(@RequestParam Long userId, @PathVariable Long itemId) {
        try {
            User updatedUser = shopService.applyItem(userId, itemId);
            return ResponseEntity.ok(updatedUser);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
}
