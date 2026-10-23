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
    private final com.example.japanesegame.service.JMdictService jmdictService;

    @GetMapping
    public ResponseEntity<List<Vocabulary>> getAllVocabularies(
            @RequestParam(required = false) Integer level,
            @RequestParam(required = false) String q) {
            
        if (q != null && !q.trim().isEmpty()) {
            String query = q.trim().toLowerCase();
            List<Vocabulary> results = jmdictService.searchVocabularies(query);
            return ResponseEntity.ok(results);
        }

        if (level != null) {
            return ResponseEntity.ok(jmdictService.getVocabulariesForLevel(level));
        }
        
        return ResponseEntity.ok(jmdictService.getAllVocabularies());
    }


    @PutMapping("/{id}/level")
    public ResponseEntity<Vocabulary> updateVocabularyLevel(@PathVariable Long id, @RequestParam Integer level) {
        return ResponseEntity.ok(vocabularyService.updateVocabularyLevel(id, level));
    }
}
