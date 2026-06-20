package com.example.japanesegame.controller;

import com.example.japanesegame.service.PvPService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/pvp")
public class PvPController {

    @Autowired
    private PvPService pvpService;

    @PostMapping("/queue/join/{userId}")
    public ResponseEntity<String> joinQueue(@PathVariable Long userId) {
        String result = pvpService.joinQueue(userId);
        if ("INELIGIBLE".equals(result)) {
            return ResponseEntity.badRequest().body("Player is not eligible.");
        }
        if ("BANNED".equals(result)) {
            return ResponseEntity.badRequest().body("BANNED");
        }
        return ResponseEntity.ok(result);
    }

    @PostMapping("/queue/leave/{userId}")
    public ResponseEntity<String> leaveQueue(@PathVariable Long userId) {
        pvpService.leaveQueue(userId);
        return ResponseEntity.ok("LEFT_QUEUE");
    }

    @PostMapping("/match/quit/{userId}")
    public ResponseEntity<String> quitMatch(@PathVariable Long userId) {
        String result = pvpService.quitMatch(userId);
        return ResponseEntity.ok(result);
    }

    @GetMapping("/status/{userId}")
    public ResponseEntity<Map<String, Object>> getStatus(@PathVariable Long userId) {
        return ResponseEntity.ok(pvpService.getStatus(userId));
    }

    @PostMapping("/match/progress/{userId}")
    public ResponseEntity<String> updateProgress(@PathVariable Long userId, @RequestParam int progress) {
        return ResponseEntity.ok(pvpService.updateProgress(userId, progress));
    }

    @PostMapping("/match/finish/{userId}")
    public ResponseEntity<String> finishMatch(@PathVariable Long userId, @RequestParam Long timeMs) {
        return ResponseEntity.ok(pvpService.finishMatch(userId, timeMs));
    }

    @PostMapping("/match/ack/{userId}")
    public ResponseEntity<String> acknowledgeFinish(@PathVariable Long userId) {
        pvpService.acknowledgeFinish(userId);
        return ResponseEntity.ok("ACKNOWLEDGED");
    }

    // ============ LOBBY ENDPOINTS ============

    @PostMapping("/lobby/create/{userId}")
    public ResponseEntity<Map<String, Object>> createLobby(@PathVariable Long userId) {
        Map<String, Object> result = pvpService.createLobby(userId);
        if (result.containsKey("error")) {
            return ResponseEntity.badRequest().body(result);
        }
        return ResponseEntity.ok(result);
    }

    @PostMapping("/lobby/join/{code}/{userId}")
    public ResponseEntity<Map<String, Object>> joinLobby(@PathVariable String code, @PathVariable Long userId) {
        Map<String, Object> result = pvpService.joinLobby(code.toUpperCase(), userId);
        if (result.containsKey("error")) {
            return ResponseEntity.badRequest().body(result);
        }
        return ResponseEntity.ok(result);
    }

    @PostMapping("/lobby/cancel/{code}/{userId}")
    public ResponseEntity<String> cancelLobby(@PathVariable String code, @PathVariable Long userId) {
        pvpService.cancelLobby(code.toUpperCase(), userId);
        return ResponseEntity.ok("LOBBY_CANCELLED");
    }
}
