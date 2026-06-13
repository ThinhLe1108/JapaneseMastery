package com.example.japanesegame.service;

import com.example.japanesegame.entity.Vocabulary;
import com.example.japanesegame.entity.WordType;
import com.example.japanesegame.repository.VocabularyRepository;
import jakarta.annotation.PostConstruct;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

import javax.xml.stream.XMLInputFactory;
import javax.xml.stream.XMLStreamReader;
import javax.xml.stream.events.XMLEvent;
import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.StandardCopyOption;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.nio.file.Paths;
import java.util.Map;

@Service
@RequiredArgsConstructor
public class JMdictService {
    private final VocabularyRepository vocabularyRepository;
    private final Map<Integer, List<Vocabulary>> levelVocabMap = new java.util.HashMap<>();

    @PostConstruct
    public void init() {
        reloadJMdict();
    }

    public synchronized void reloadJMdict() {
        try {
            String currentDir = System.getProperty("user.dir");
            File baseDir = new File(currentDir);
            if ("JapaneseMasteryBackend".equals(baseDir.getName())) {
                baseDir = baseDir.getParentFile();
            }
            
            levelVocabMap.clear();
            ObjectMapper mapper = new ObjectMapper();

            // 1. Load default_vocab.json (Levels 1-24)
            File defaultJsonFile = new File(baseDir, "JapaneseMastery/data/default_vocab.json");
            if (defaultJsonFile.exists()) {
                System.out.println("Loading default_vocab.json...");
                Map<String, List<Vocabulary>> defaultMap = mapper.readValue(defaultJsonFile, new TypeReference<Map<String, List<Vocabulary>>>() {});
                for (Map.Entry<String, List<Vocabulary>> entry : defaultMap.entrySet()) {
                    int level = Integer.parseInt(entry.getKey());
                    levelVocabMap.put(level, entry.getValue());
                }
            } else {
                System.out.println("default_vocab.json not found at " + defaultJsonFile.getAbsolutePath());
            }

            // 2. Load jmdict_levels.json (Levels 25+)
            File jsonFile = new File(baseDir, "JapaneseMastery/data/jmdict_levels.json");
            if (jsonFile.exists()) {
                System.out.println("Loading jmdict_levels.json...");
                Map<String, List<Vocabulary>> levelsMap = mapper.readValue(jsonFile, new TypeReference<Map<String, List<Vocabulary>>>() {});
                for (Map.Entry<String, List<Vocabulary>> entry : levelsMap.entrySet()) {
                    int level = Integer.parseInt(entry.getKey());
                    levelVocabMap.put(level, entry.getValue());
                }
            } else {
                System.out.println("jmdict_levels.json not found at " + jsonFile.getAbsolutePath());
            }

            System.out.println("Loaded vocabularies for " + levelVocabMap.size() + " levels.");

        } catch (Exception e) {
            System.err.println("Failed to load vocabulary JSON files: " + e.getMessage());
            e.printStackTrace();
        }
    }

    public List<Vocabulary> getVocabulariesForLevel(int level) {
        List<Vocabulary> levelVocabs = levelVocabMap.get(level);
        if (levelVocabs == null) return new ArrayList<>();

        List<Vocabulary> result = new ArrayList<>();
        for (Vocabulary base : levelVocabs) {
            Vocabulary copy = Vocabulary.builder()
                    .wordJp(base.getWordJp())
                    .kana(base.getKana())
                    .romaji(base.getRomaji())
                    .meaning(base.getMeaning())
                    .type(base.getType())
                    .levelRequired(level)
                    .build();
            result.add(copy);
        }
        return result;
    }

    public List<Vocabulary> getAllVocabularies() {
        List<Vocabulary> all = new ArrayList<>();
        List<Integer> levels = levelVocabMap.keySet().stream().sorted().collect(Collectors.toList());
        for (int level : levels) {
            all.addAll(getVocabulariesForLevel(level));
        }
        return all;
    }

    public List<Vocabulary> searchVocabularies(String query) {
        List<Vocabulary> results = new ArrayList<>();
        if (query == null || query.trim().isEmpty()) return results;
        
        String lowerQuery = query.toLowerCase();
        
        for (Map.Entry<Integer, List<Vocabulary>> entry : levelVocabMap.entrySet()) {
            int level = entry.getKey();
            for (Vocabulary v : entry.getValue()) {
                if ((v.getWordJp() != null && v.getWordJp().toLowerCase().contains(lowerQuery)) ||
                    (v.getKana() != null && v.getKana().toLowerCase().contains(lowerQuery)) ||
                    (v.getRomaji() != null && v.getRomaji().toLowerCase().contains(lowerQuery)) ||
                    (v.getMeaning() != null && v.getMeaning().toLowerCase().contains(lowerQuery))) {
                    
                    Vocabulary copy = Vocabulary.builder()
                        .wordJp(v.getWordJp())
                        .kana(v.getKana())
                        .romaji(v.getRomaji())
                        .meaning(v.getMeaning())
                        .type(v.getType())
                        .levelRequired(level)
                        .build();
                        
                    results.add(copy);
                    if (results.size() >= 100) break;
                }
            }
            if (results.size() >= 100) break;
        }
        return results;
    }
}
