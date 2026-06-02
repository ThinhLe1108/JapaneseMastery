package com.example.japanesegame.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class CustomTestResponse {
    private Long id;
    private String title;
    private String testCode;
    private Integer minLevel;
    private Integer rewardGcoin;
    private Integer maxAttempts;
    private Boolean isApproved;
    private Long senseiId;
    private String senseiUsername;
    private List<CustomTestQuestionDTO> questions;
}
