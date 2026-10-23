package com.example.japanesegame.config;

import com.example.japanesegame.repository.VocabularyRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.CommandLineRunner;
import org.springframework.core.annotation.Order;
import org.springframework.stereotype.Component;

@Component
@Order(2)
@RequiredArgsConstructor
public class VocabularySeeder implements CommandLineRunner {

    private final VocabularyRepository vocabularyRepository;

    @Override
    public void run(String... args) throws Exception {
        // Vocabulary seeding moved to JSON loading in JMdictService
        System.out.println("VocabularySeeder: Skipping DB seeding as per JSON-only requirement.");
    }
}
