package com.example.japanesegame.controller;

import com.example.japanesegame.dto.CustomTestResponse;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.UserRepository;
import com.example.japanesegame.service.CustomTestService;
import com.example.japanesegame.service.VocabularyService;
import com.example.japanesegame.entity.Vocabulary;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.util.Map;

@RestController
@RequestMapping("/api/player")
@RequiredArgsConstructor
public class PlayerController {

    private final UserRepository userRepository;
    private final CustomTestService customTestService;
    private final VocabularyService vocabularyService;

    @GetMapping("/{id}/heartbeat")
    public ResponseEntity<Void> heartbeat(@PathVariable Long id, @RequestHeader(value = "Authorization", required = false) String authHeader) {
        User user = userRepository.findById(id).orElseThrow(() -> new RuntimeException("User not found"));
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            if (!token.equals(user.getAccessToken())) {
                return ResponseEntity.status(401).build();
            }
        }
        return ResponseEntity.ok().build();
    }

    @PutMapping("/{id}/progress")
    public ResponseEntity<User> updateProgress(@PathVariable Long id, @RequestParam Integer level, @RequestHeader(value = "Authorization", required = false) String authHeader, @RequestBody(required = false) List<String> newLearnedWords) {
        User user = userRepository.findById(id).orElseThrow(() -> new RuntimeException("User not found"));
        
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            if (!token.equals(user.getAccessToken())) {
                return ResponseEntity.status(401).build();
            }
        }

        // Add learned words if provided
        if (newLearnedWords != null && !newLearnedWords.isEmpty()) {
            user.getLearnedWords().addAll(newLearnedWords);
        }

        // Chỉ lưu nếu level mới cao hơn level hiện tại để tránh lỗi thụt lùi cấp độ
        if (level > user.getLevel()) {
            user.setLevel(level);
        }
        userRepository.save(user);
        
        return ResponseEntity.ok(user);
    }

    @GetMapping("/vocabularies")
    public ResponseEntity<List<Vocabulary>> getVocabulariesForLevel(@RequestParam Integer level, @RequestParam(required = false) Long userId) {
        java.util.Set<String> learnedWords = null;
        if (userId != null) {
            User user = userRepository.findById(userId).orElse(null);
            if (user != null) {
                learnedWords = user.getLearnedWords();
            }
        }
        return ResponseEntity.ok(vocabularyService.getVocabulariesForLevel(level, learnedWords));
    }

    @PutMapping("/{id}/coins")
    public ResponseEntity<User> updateCoins(@PathVariable Long id, @RequestParam Integer coins, @RequestHeader(value = "Authorization", required = false) String authHeader) {
        User user = userRepository.findById(id).orElseThrow(() -> new RuntimeException("User not found"));
        
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            if (!token.equals(user.getAccessToken())) {
                return ResponseEntity.status(401).build();
            }
        }

        user.setGCoin(coins);
        userRepository.save(user);
        return ResponseEntity.ok(user);
    }

    @GetMapping("/{id}/custom-tests/{testCode}")
    public ResponseEntity<?> getCustomTest(@PathVariable Long id, @PathVariable String testCode, @RequestHeader(value = "Authorization", required = false) String authHeader) {
        try {
            return ResponseEntity.ok(customTestService.getCustomTestForPlayer(id, testCode));
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    @PostMapping("/{id}/custom-tests/{testCode}/submit")
    public ResponseEntity<?> submitCustomTest(@PathVariable Long id, @PathVariable String testCode, @RequestBody Map<String, Object> body) {
        try {
            int score = (int) body.get("score");
            boolean passed = (boolean) body.get("passed");
            User updatedUser = customTestService.submitCustomTest(id, testCode, score, passed);
            return ResponseEntity.ok(updatedUser);
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }
}
