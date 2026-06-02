package com.example.japanesegame.entity;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "custom_test_questions")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class CustomTestQuestion {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "custom_test_id", nullable = false)
    private CustomTest customTest;

    @Column(name = "question_text", nullable = false)
    private String questionText;

    @Column(name = "answer_a")
    private String answerA;

    @Column(name = "answer_b")
    private String answerB;

    @Column(name = "answer_c")
    private String answerC;

    @Column(name = "answer_d")
    private String answerD;

    @Column(name = "correct_answer", nullable = false)
    private String correctAnswer;
}
