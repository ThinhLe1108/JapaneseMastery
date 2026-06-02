package com.example.japanesegame.repository;

import com.example.japanesegame.entity.CustomTestAttempt;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface CustomTestAttemptRepository extends JpaRepository<CustomTestAttempt, Long> {
    long countByPlayerIdAndTestId(Long playerId, Long testId);
    void deleteByTestId(Long testId);
    java.util.List<CustomTestAttempt> findByTestIdOrderByAttemptTimeDesc(Long testId);
}
