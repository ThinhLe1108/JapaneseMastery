package com.example.japanesegame.entity;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "custom_tests")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class CustomTest {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private String title;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "sensei_id", nullable = false)
    private User sensei;

    @Column(name = "test_code", unique = true, nullable = false)
    private String testCode;

    @Column(name = "min_level", nullable = false)
    @Builder.Default
    private Integer minLevel = 1;

    @Column(name = "reward_gcoin", nullable = false)
    private Integer rewardGcoin;

    @Column(name = "max_attempts", nullable = false)
    private Integer maxAttempts; // Bắt buộc dưới 10 lần

    @Column(name = "is_approved", nullable = false)
    @Builder.Default
    private Boolean isApproved = true;
}
