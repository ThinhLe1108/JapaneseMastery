package com.example.japanesegame.config;

import com.example.japanesegame.entity.Role;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.CommandLineRunner;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
public class DatabaseSeeder implements CommandLineRunner {

    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;

    @Override
    public void run(String... args) throws Exception {
        if (userRepository.findByUsername("admin").isEmpty()) {
            User admin = User.builder()
                    .username("admin")
                    .password(passwordEncoder.encode("123"))
                    .role(Role.ADMIN)
                    .level(99)
                    .gCoin(9999)
                    .displayName("System Admin")
                    .build();
            userRepository.save(admin);
            System.out.println("Default ADMIN account created: admin / 123");
        }
    }
}
