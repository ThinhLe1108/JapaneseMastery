package com.example.japanesegame.service;

import com.example.japanesegame.config.JwtUtil;
import com.example.japanesegame.dto.AuthRequest;
import com.example.japanesegame.dto.AuthResponse;
import com.example.japanesegame.entity.Role;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
@RequiredArgsConstructor
public class AuthService {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtUtil jwtUtil;

    public AuthResponse register(AuthRequest request) {
        if (userRepository.findByUsername(request.getUsername()).isPresent()) {
            throw new RuntimeException("Username already exists");
        }

        User user = User.builder()
                .username(request.getUsername())
                .password(passwordEncoder.encode(request.getPassword()))
                .role(Role.PLAYER)
                .level(1)
                .gCoin(0)
                .build();

        userRepository.save(user);

        String token = jwtUtil.generateToken(user.getUsername());
        user.setAccessToken(token);
        userRepository.save(user);
        
        return AuthResponse.builder()
                .id(user.getId())
                .token(token)
                .username(user.getUsername())
                .role(user.getRole())
                .level(user.getLevel())
                .gCoin(user.getGCoin())
                .build();
    }

    public AuthResponse login(AuthRequest request) {
        Optional<User> optUser = userRepository.findByUsername(request.getUsername());
        if (optUser.isEmpty()) {
            throw new RuntimeException("Invalid username or password");
        }

        User user = optUser.get();
        if (!passwordEncoder.matches(request.getPassword(), user.getPassword())) {
            throw new RuntimeException("Invalid username or password");
        }

        if (user.getAccessToken() != null && jwtUtil.validateToken(user.getAccessToken())) {
            throw new RuntimeException("Account is already logged in on another device!");
        }

        String token = jwtUtil.generateToken(user.getUsername());
        user.setAccessToken(token);
        userRepository.save(user);
        
        return AuthResponse.builder()
                .id(user.getId())
                .token(token)
                .username(user.getUsername())
                .role(user.getRole())
                .level(user.getLevel())
                .gCoin(user.getGCoin())
                .build();
    }

    public void logout(Long id) {
        userRepository.findById(id).ifPresent(user -> {
            user.setAccessToken(null);
            userRepository.save(user);
        });
    }
}
