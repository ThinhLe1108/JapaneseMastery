package com.example.japanesegame.repository;

import com.example.japanesegame.entity.CustomTest;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface CustomTestRepository extends JpaRepository<CustomTest, Long> {
    List<CustomTest> findBySenseiId(Long senseiId);
    Optional<CustomTest> findByTestCode(String testCode);
    List<CustomTest> findByIsApprovedFalse();
    List<CustomTest> findByIsApprovedTrue();
}
