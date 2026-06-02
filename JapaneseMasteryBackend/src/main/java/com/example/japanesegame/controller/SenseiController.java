package com.example.japanesegame.controller;

import com.example.japanesegame.dto.CustomTestRequest;
import com.example.japanesegame.dto.CustomTestResponse;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.UserRepository;
import com.example.japanesegame.service.CustomTestService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/sensei")
@RequiredArgsConstructor
public class SenseiController {

    private final CustomTestService customTestService;
    private final UserRepository userRepository;

    @PostMapping("/{senseiId}/custom-tests")
    public ResponseEntity<CustomTestResponse> createCustomTest(
            @PathVariable Long senseiId,
            @RequestBody CustomTestRequest request,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
            
        User sensei = userRepository.findById(senseiId)
                .orElseThrow(() -> new RuntimeException("User not found"));
        
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            if (!token.equals(sensei.getAccessToken())) {
                return ResponseEntity.status(401).build();
            }
        }

        CustomTestResponse response = customTestService.createCustomTest(senseiId, request);
        return ResponseEntity.ok(response);
    }

    @GetMapping("/{senseiId}/custom-tests")
    public ResponseEntity<List<CustomTestResponse>> getCustomTests(
            @PathVariable Long senseiId,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
            
        User sensei = userRepository.findById(senseiId)
                .orElseThrow(() -> new RuntimeException("User not found"));
        
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            if (!token.equals(sensei.getAccessToken())) {
                return ResponseEntity.status(401).build();
            }
        }

        List<CustomTestResponse> responses = customTestService.getCustomTestsBySensei(senseiId);
        return ResponseEntity.ok(responses);
    }

    @GetMapping("/{senseiId}/tests/{testId}/attempts")
    public ResponseEntity<?> getTestAttempts(
            @PathVariable Long senseiId,
            @PathVariable Long testId,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
        try {
            return ResponseEntity.ok(customTestService.getTestAttempts(senseiId, testId));
        } catch (Exception e) {
            return ResponseEntity.badRequest().body(e.getMessage());
        }
    }

    @PutMapping("/{senseiId}/custom-tests/{testId}")
    public ResponseEntity<CustomTestResponse> updateCustomTest(
            @PathVariable Long senseiId,
            @PathVariable Long testId,
            @RequestBody CustomTestRequest request,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
            
        User sensei = userRepository.findById(senseiId)
                .orElseThrow(() -> new RuntimeException("User not found"));
        
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            if (!token.equals(sensei.getAccessToken())) {
                return ResponseEntity.status(401).build();
            }
        }

        CustomTestResponse response = customTestService.updateCustomTest(senseiId, testId, request);
        return ResponseEntity.ok(response);
    }

    @DeleteMapping("/{senseiId}/custom-tests/{testId}")
    public ResponseEntity<Void> deleteCustomTest(
            @PathVariable Long senseiId,
            @PathVariable Long testId,
            @RequestHeader(value = "Authorization", required = false) String authHeader) {
            
        User sensei = userRepository.findById(senseiId)
                .orElseThrow(() -> new RuntimeException("User not found"));
        
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String token = authHeader.substring(7);
            if (!token.equals(sensei.getAccessToken())) {
                return ResponseEntity.status(401).build();
            }
        }

        customTestService.deleteCustomTest(senseiId, testId);
        return ResponseEntity.ok().build();
    }
}
