package com.example.japanesegame.controller;

import com.example.japanesegame.dto.CustomTestResponse;
import com.example.japanesegame.entity.ShopItem;
import com.example.japanesegame.service.ModerationService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/moderator")
@RequiredArgsConstructor
public class ModeratorController {

    private final ModerationService moderationService;

    @GetMapping("/{moderatorId}/pending-tests")
    public ResponseEntity<?> getPendingTests(
            @PathVariable Long moderatorId,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
        try {
            List<CustomTestResponse> tests = moderationService.getPendingTests(moderatorId);
            return ResponseEntity.ok(tests);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    @GetMapping("/{moderatorId}/approved-tests")
    public ResponseEntity<?> getApprovedTests(
            @PathVariable Long moderatorId,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
        try {
            List<CustomTestResponse> tests = moderationService.getApprovedTests(moderatorId);
            return ResponseEntity.ok(tests);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    @PutMapping("/{moderatorId}/tests/{testId}/approve")
    public ResponseEntity<?> approveTest(
            @PathVariable Long moderatorId,
            @PathVariable Long testId,
            @RequestBody Map<String, Boolean> body,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
        try {
            boolean approve = body.getOrDefault("approve", false);
            return ResponseEntity.ok(moderationService.approveTest(moderatorId, testId, approve));
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    @GetMapping("/{moderatorId}/pending-items")
    public ResponseEntity<?> getPendingItems(
            @PathVariable Long moderatorId,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
        try {
            List<ShopItem> items = moderationService.getPendingItems(moderatorId);
            return ResponseEntity.ok(items);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    @PutMapping("/{moderatorId}/items/{itemId}/approve")
    public ResponseEntity<?> approveItem(
            @PathVariable Long moderatorId,
            @PathVariable Long itemId,
            @RequestBody Map<String, Boolean> body,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
        try {
            boolean approve = body.getOrDefault("approve", false);
            return ResponseEntity.ok(moderationService.approveItem(moderatorId, itemId, approve));
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
}
