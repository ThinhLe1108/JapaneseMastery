package com.example.japanesegame.controller;

import com.example.japanesegame.entity.Role;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.service.UserService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/admin/users")
@RequiredArgsConstructor
public class AdminController {

    private final UserService userService;

    @GetMapping
    public ResponseEntity<List<User>> getAllUsers() {
        return ResponseEntity.ok(userService.getAllUsers());
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<Void> deleteUser(@PathVariable Long id) {
        userService.deleteUser(id);
        return ResponseEntity.ok().build();
    }

    @PutMapping("/{id}/role")
    public ResponseEntity<User> changeRole(@PathVariable Long id, @RequestParam Role role) {
        return ResponseEntity.ok(userService.changeUserRole(id, role));
    }

    @PutMapping("/{id}/level")
    public ResponseEntity<User> changeLevel(@PathVariable Long id, @RequestParam Integer level) {
        return ResponseEntity.ok(userService.changeUserLevel(id, level));
    }
}
