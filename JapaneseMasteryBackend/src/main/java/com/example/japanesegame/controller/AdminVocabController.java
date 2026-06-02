package com.example.japanesegame.controller;

import com.example.japanesegame.entity.Vocabulary;
import com.example.japanesegame.service.VocabularyService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/admin/vocabularies")
@RequiredArgsConstructor
public class AdminVocabController {

    private final VocabularyService vocabularyService;

    @GetMapping
    public ResponseEntity<List<Vocabulary>> getAllVocabularies() {
        return ResponseEntity.ok(vocabularyService.getAllVocabularies());
    }

    @PutMapping("/{id}/level")
    public ResponseEntity<Vocabulary> updateVocabularyLevel(@PathVariable Long id, @RequestParam Integer level) {
        return ResponseEntity.ok(vocabularyService.updateVocabularyLevel(id, level));
    }
}
