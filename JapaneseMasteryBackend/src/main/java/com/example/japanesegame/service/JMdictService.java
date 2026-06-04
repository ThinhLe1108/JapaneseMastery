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

@Service
@RequiredArgsConstructor
public class JMdictService {
    private final VocabularyRepository vocabularyRepository;
    private final List<Vocabulary> xmlVocabList = new ArrayList<>();

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
            File dictFile = new File(baseDir, "JMdict_e");
            if (!dictFile.exists()) {
                System.out.println("JMdict_e not found! Downloading from ftp.edrdg.org (62MB)... Please wait...");
                try {
                    File parent = dictFile.getParentFile();
                    if (parent != null && !parent.exists()) {
                        parent.mkdirs();
                    }
                    URL url = new URL("http://ftp.edrdg.org/pub/Nihongo/JMdict_e");
                    Files.copy(url.openStream(), dictFile.toPath(), StandardCopyOption.REPLACE_EXISTING);
                    System.out.println("Download complete!");
                } catch (Exception e) {
                    System.out.println("Failed to download JMdict_e: " + e.getMessage());
                    return;
                }
            }

            System.out.println("Loading JMdict_e using StAX...");
            
            // Disable XML entity expansion limit (default is 64000) because JMdict has hundreds of thousands of entities
            System.setProperty("jdk.xml.entityExpansionLimit", "0");
            
            XMLInputFactory factory = XMLInputFactory.newInstance();
            try {
                factory.setProperty(javax.xml.XMLConstants.FEATURE_SECURE_PROCESSING, false);
            } catch (Exception ignored) {}
            
            // Enable DTD for internal entities but disable external to prevent network access
            factory.setProperty(XMLInputFactory.SUPPORT_DTD, true);
            factory.setProperty(XMLInputFactory.IS_SUPPORTING_EXTERNAL_ENTITIES, false);
            
            InputStream fileStream = new FileInputStream(dictFile);
            XMLStreamReader reader = factory.createXMLStreamReader(fileStream);

            List<JMDictEntry> tempEntries = new ArrayList<>();
            
            JMDictEntry currentEntry = null;
            String currentElement = "";
            String tempText = "";

            while (reader.hasNext()) {
                int event = reader.next();
                
                switch (event) {
                    case XMLEvent.START_ELEMENT:
                        currentElement = reader.getLocalName();
                        if ("entry".equals(currentElement)) {
                            currentEntry = new JMDictEntry();
                        }
                        tempText = "";
                        break;
                        
                    case XMLEvent.CHARACTERS:
                        if (currentEntry != null) {
                            tempText += reader.getText();
                        }
                        break;
                        
                    case XMLEvent.END_ELEMENT:
                        String endElement = reader.getLocalName();
                        if (currentEntry != null) {
                            if ("keb".equals(endElement)) {
                                if (currentEntry.keb == null) currentEntry.keb = tempText.trim();
                            } else if ("reb".equals(endElement)) {
                                if (currentEntry.reb == null) currentEntry.reb = tempText.trim();
                            } else if ("gloss".equals(endElement)) {
                                if (currentEntry.gloss == null) currentEntry.gloss = tempText.trim();
                            } else if ("ke_pri".equals(endElement) || "re_pri".equals(endElement)) {
                                currentEntry.hasPri = true;
                            } else if ("entry".equals(endElement)) {
                                if (currentEntry.reb != null && currentEntry.gloss != null) {
                                    tempEntries.add(currentEntry);
                                }
                                currentEntry = null;
                            }
                        }
                        break;
                }
            }
            
            reader.close();
            fileStream.close();

            // Sort entries: those with priority tags (news1, ichi1, etc.) first
            // Then we just take them in order.
            tempEntries.sort((a, b) -> Boolean.compare(b.hasPri, a.hasPri));

            // Load existing DB words to exclude overlaps
            Set<String> existingDbWords = vocabularyRepository.findAll().stream()
                    .map(Vocabulary::getWordJp)
                    .collect(Collectors.toSet());

            xmlVocabList.clear();
            for (JMDictEntry entry : tempEntries) {
                String wordJp = entry.keb != null ? entry.keb : entry.reb;
                if (!existingDbWords.contains(wordJp)) {
                    Vocabulary vocab = Vocabulary.builder()
                            .wordJp(wordJp)
                            .romaji(entry.reb)
                            .meaning(entry.gloss)
                            .type(WordType.KANJI)
                            .build();
                    xmlVocabList.add(vocab);
                }
            }

            System.out.println("Loaded " + xmlVocabList.size() + " unique vocabularies from JMdict_e");

        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public List<Vocabulary> getVocabulariesForLevel(int level) {
        int offset = (level - 25) * 10;
        if (offset < 0 || offset >= xmlVocabList.size()) {
            return new ArrayList<>();
        }
        int end = Math.min(offset + 10, xmlVocabList.size());
        
        List<Vocabulary> result = new ArrayList<>();
        for (int i = offset; i < end; i++) {
            Vocabulary base = xmlVocabList.get(i);
            Vocabulary copy = Vocabulary.builder()
                    .wordJp(base.getWordJp())
                    .romaji(base.getRomaji())
                    .meaning(base.getMeaning())
                    .type(base.getType())
                    .levelRequired(level)
                    .build();
            result.add(copy);
        }
        return result;
    }

    private static class JMDictEntry {
        String keb; // kanji
        String reb; // reading
        String gloss; // meaning
        boolean hasPri = false; // Priority tag (common word)
    }
}
