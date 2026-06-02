package com.example.japanesegame.repository;

import com.example.japanesegame.entity.Vocabulary;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface VocabularyRepository extends JpaRepository<Vocabulary, Long> {
    List<Vocabulary> findByLevelRequired(Integer levelRequired);
    boolean existsByWordJp(String wordJp);
}
