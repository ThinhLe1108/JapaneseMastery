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
        return jmdictService.getAllVocabularies();
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
        List<Vocabulary> baseList = jmdictService.getVocabulariesForLevel(level);
        List<Vocabulary> expandedList = new ArrayList<>();

        for (Vocabulary v : baseList) {
            String wordJp = (v.getWordJp() != null && !v.getWordJp().isEmpty()) ? v.getWordJp() : v.getKana();
            String kana = (v.getKana() != null && !v.getKana().isEmpty()) ? v.getKana() : v.getWordJp();

            if (hasKanji(wordJp)) {
                // Add Kanji version
                expandedList.add(Vocabulary.builder()
                        .wordJp(wordJp)
                        .kana(kana)
                        .romaji(v.getRomaji())
                        .meaning(v.getMeaning())
                        .type(v.getType())
                        .levelRequired(v.getLevelRequired())
                        .build());
                // Add Kana version
                expandedList.add(Vocabulary.builder()
                        .wordJp(kana)
                        .kana(kana)
                        .romaji(v.getRomaji())
                        .meaning(v.getMeaning())
                        .type(v.getType())
                        .levelRequired(v.getLevelRequired())
                        .build());
            } else {
                // Just add as is (Hiragana/Katakana)
                expandedList.add(Vocabulary.builder()
                        .wordJp(wordJp)
                        .kana(kana)
                        .romaji(v.getRomaji())
                        .meaning(v.getMeaning())
                        .type(v.getType())
                        .levelRequired(v.getLevelRequired())
                        .build());
            }
        }
        return expandedList;
    }

    private boolean hasKanji(String s) {
        if (s == null) return false;
        for (char c : s.toCharArray()) {
            if (c >= '\u4e00' && c <= '\u9faf') return true;
        }
        return false;
    }

}
