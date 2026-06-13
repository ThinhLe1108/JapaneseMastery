package com.example.japanesegame.entity;

import jakarta.persistence.*;
import lombok.*;

import java.time.LocalDateTime;

@Entity
@Table(name = "learning_progress")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class LearningProgress {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;

    @Column(name = "word_jp", nullable = false, columnDefinition = "NVARCHAR(255)")
    private String wordJp;

    @Column(name = "is_memorized", nullable = false)
    @Builder.Default
    private Boolean isMemorized = false;

    @Column(name = "hesitation_count", nullable = false)
    @Builder.Default
    private Integer hesitationCount = 0;

    @Column(name = "last_review_time")
    private LocalDateTime lastReviewTime;

    @Column(name = "is_writing_passed", nullable = false)
    @Builder.Default
    private Boolean isWritingPassed = false;
}
