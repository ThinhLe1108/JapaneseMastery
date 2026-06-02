package com.example.japanesegame.service;

import com.example.japanesegame.entity.Vocabulary;
import com.example.japanesegame.repository.VocabularyRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import com.example.japanesegame.entity.WordType;


import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class VocabularyService {
    private final VocabularyRepository vocabularyRepository;
    private final JMdictService jmdictService;

    public List<Vocabulary> getAllVocabularies() {
        return vocabularyRepository.findAll();
    }

    public Vocabulary getVocabularyById(Long id) {
        return vocabularyRepository.findById(id).orElseThrow(() -> new RuntimeException("Vocabulary not found"));
    }

    public Vocabulary updateVocabularyLevel(Long id, Integer level) {
        Vocabulary vocab = getVocabularyById(id);
        vocab.setLevelRequired(level);
        return vocabularyRepository.save(vocab);
    }



    public List<Vocabulary> getVocabulariesForLevel(int level, java.util.Set<String> learnedWords) {
        // Luôn ưu tiên lấy từ Database trước. Nếu Database đã có, trả về ngay.
        List<Vocabulary> dbVocabs = vocabularyRepository.findByLevelRequired(level);
        if (dbVocabs != null && !dbVocabs.isEmpty()) {
            return dbVocabs;
        }

        // Nếu Database chưa có (Level mới), tự động sinh từ kanjidic2.xml và lưu vào DB
        return fetchVocabulariesFromKanjidicAndSave(level);
    }

    private List<Vocabulary> fetchVocabulariesFromKanjidicAndSave(int level) {
        List<Vocabulary> fetched = jmdictService.getVocabulariesForLevel(level);
        List<Vocabulary> saved = new ArrayList<>();
        
        for (Vocabulary vocab : fetched) {
            if (!vocabularyRepository.existsByWordJp(vocab.getWordJp())) {
                saved.add(vocabularyRepository.save(vocab));
            }
        }
        
        return saved;
    }
}
