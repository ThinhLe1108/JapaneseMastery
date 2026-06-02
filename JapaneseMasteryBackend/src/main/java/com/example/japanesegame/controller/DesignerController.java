package com.example.japanesegame.controller;

import com.example.japanesegame.dto.ShopItemRequest;
import com.example.japanesegame.entity.ShopItem;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.ShopItemRepository;
import com.example.japanesegame.repository.UserRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/designer/{designerId}/items")
public class DesignerController {

    @Autowired
    private ShopItemRepository shopItemRepository;

    @Autowired
    private UserRepository userRepository;

    @GetMapping
    public ResponseEntity<List<ShopItem>> getDesignerItems(@PathVariable Long designerId) {
        List<ShopItem> items = shopItemRepository.findByDesignerIdOrderByIdDesc(designerId);
        return ResponseEntity.ok(items);
    }

    @PostMapping
    public ResponseEntity<?> createShopItem(@PathVariable Long designerId, @RequestBody ShopItemRequest request) {
        User designer = userRepository.findById(designerId).orElse(null);
        if (designer == null || !designer.getRole().name().equals("DESIGNER")) {
            return ResponseEntity.badRequest().body("User is not a valid designer.");
        }

        ShopItem item = ShopItem.builder()
                .designer(designer)
                .name(request.getName())
                .price(request.getPrice())
                .type(request.getType())
                .isApproved(false) // Phải qua kiểm duyệt
                .build();

        shopItemRepository.save(item);
        return ResponseEntity.ok("Item created successfully. Pending moderation.");
    }
}
