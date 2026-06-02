package com.example.japanesegame.entity;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "vocabularies")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Vocabulary {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "word_jp", nullable = false, columnDefinition = "NVARCHAR(255)")
    private String wordJp;

    @Column(nullable = false, columnDefinition = "NVARCHAR(255)")
    private String romaji;

    @Column(nullable = false, columnDefinition = "NVARCHAR(255)")
    private String meaning;

    @Column(name = "level_required", nullable = false)
    private Integer levelRequired;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private WordType type;
}
