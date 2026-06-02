package com.example.japanesegame.dto;

import com.example.japanesegame.entity.Role;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
public class AuthResponse {
    private Long id;
    private String token;
    private String username;
    private Role role;
    private Integer level;
    
    @com.fasterxml.jackson.annotation.JsonProperty("gCoin")
    private Integer gCoin;
}
