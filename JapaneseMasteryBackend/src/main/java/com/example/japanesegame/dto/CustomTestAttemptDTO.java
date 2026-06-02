package com.example.japanesegame.dto;

import lombok.Builder;
import lombok.Data;

import java.time.LocalDateTime;

@Data
@Builder
public class CustomTestAttemptDTO {
    private String playerName;
    private int score;
    private boolean passed;
    private LocalDateTime attemptTime;
}
