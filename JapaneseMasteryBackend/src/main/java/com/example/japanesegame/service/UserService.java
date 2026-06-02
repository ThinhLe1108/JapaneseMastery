package com.example.japanesegame.service;

import com.example.japanesegame.entity.Role;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
@RequiredArgsConstructor
public class UserService {
    private final UserRepository userRepository;

    public List<User> getAllUsers() {
        return userRepository.findAll();
    }

    public User getUserById(Long id) {
        return userRepository.findById(id)
                .orElseThrow(() -> new RuntimeException("User not found"));
    }

    public void deleteUser(Long id) {
        User user = getUserById(id);
        if (user.getRole() == Role.ADMIN) {
            throw new RuntimeException("Cannot delete ADMIN account");
        }
        userRepository.delete(user);
    }

    public User changeUserRole(Long id, Role newRole) {
        User user = getUserById(id);
        if (user.getRole() == Role.ADMIN) {
            throw new RuntimeException("Cannot change role of ADMIN account");
        }
        user.setRole(newRole);
        return userRepository.save(user);
    }

    public User changeUserLevel(Long id, Integer newLevel) {
        User user = getUserById(id);
        user.setLevel(newLevel);
        return userRepository.save(user);
    }
}
