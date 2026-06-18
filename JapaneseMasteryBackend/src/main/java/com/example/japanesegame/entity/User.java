package com.example.japanesegame.entity;

import jakarta.persistence.*;
import lombok.*;
import java.util.HashSet;
import java.util.Set;

@Entity
@Table(name = "users")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(unique = true, nullable = false)
    private String username; // SĐT/Email

    @Column(nullable = false)
    private String password;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private Role role;

    @Column(nullable = false)
    @Builder.Default
    private Integer level = 1;

    @Column(name = "g_coin", nullable = false)
    @Builder.Default
    private Integer gCoin = 0;

    @Column(name = "display_name")
    private String displayName;

    @Column(name = "access_token", length = 500)
    private String accessToken;

    @Column(name = "pvp_ban_until")
    private java.time.LocalDateTime pvpBanUntil;

    @ElementCollection(fetch = FetchType.EAGER)
    @CollectionTable(name = "user_learned_words", joinColumns = @JoinColumn(name = "user_id"))
    @Column(name = "word_jp", columnDefinition = "NVARCHAR(255)")
    @Builder.Default
    private Set<String> learnedWords = new HashSet<>();
}
