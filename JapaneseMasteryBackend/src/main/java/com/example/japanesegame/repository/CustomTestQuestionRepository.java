package com.example.japanesegame.repository;

import com.example.japanesegame.entity.CustomTestQuestion;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface CustomTestQuestionRepository extends JpaRepository<CustomTestQuestion, Long> {
    List<CustomTestQuestion> findByCustomTestId(Long testId);
    void deleteByCustomTestId(Long testId);
}
