package com.example.japanesegame.dto;

import lombok.Data;

@Data
public class PvPMatch {
    private String matchId;
    private Long player1Id;
    private Long player2Id;
    private String paragraph;
    private String romajiParagraph;
    private String state; // "WAITING_P1", "WAITING_P2", "FINISHED"
    
    private Long p1TimeMs = 0L;
    private Long p2TimeMs = 0L;
    
    private Long winnerId = -1L;
    private int gCoinReward = 100;
    
    private Long startTimeMs = 0L;
}
