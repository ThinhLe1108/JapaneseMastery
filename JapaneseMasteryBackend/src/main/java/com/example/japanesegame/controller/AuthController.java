package com.example.japanesegame.controller;

import com.example.japanesegame.dto.AuthRequest;
import com.example.japanesegame.dto.AuthResponse;
import com.example.japanesegame.service.AuthService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/auth")
@RequiredArgsConstructor
public class AuthController {

    private final AuthService authService;

    @PostMapping("/register")
    public ResponseEntity<AuthResponse> register(@RequestBody AuthRequest request) {
        return ResponseEntity.ok(authService.register(request));
    }

    @PostMapping("/login")
    public ResponseEntity<AuthResponse> login(@RequestBody AuthRequest request) {
        return ResponseEntity.ok(authService.login(request));
    }

    @PostMapping("/logout/{id}")
    public ResponseEntity<Void> logout(@PathVariable Long id) {
        authService.logout(id);
        return ResponseEntity.ok().build();
    }
}
