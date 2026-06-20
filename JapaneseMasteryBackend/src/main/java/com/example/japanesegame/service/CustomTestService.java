package com.example.japanesegame.service;

import com.example.japanesegame.dto.CustomTestRequest;
import com.example.japanesegame.dto.CustomTestResponse;
import com.example.japanesegame.entity.CustomTest;
import com.example.japanesegame.entity.Role;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.CustomTestRepository;
import com.example.japanesegame.repository.UserRepository;
import com.example.japanesegame.dto.CustomTestQuestionDTO;
import com.example.japanesegame.entity.CustomTestQuestion;
import com.example.japanesegame.entity.CustomTestAttempt;
import com.example.japanesegame.dto.CustomTestAttemptDTO;
import com.example.japanesegame.repository.CustomTestQuestionRepository;
import com.example.japanesegame.repository.CustomTestAttemptRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.LocalDateTime;
import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class CustomTestService {

    private final CustomTestRepository customTestRepository;
    private final CustomTestQuestionRepository customTestQuestionRepository;
    private final CustomTestAttemptRepository customTestAttemptRepository;
    private final UserRepository userRepository;

    @Transactional
    public CustomTestResponse createCustomTest(Long senseiId, CustomTestRequest request) {
        User sensei = userRepository.findById(senseiId)
                .orElseThrow(() -> new RuntimeException("User not found"));

        if (!sensei.getRole().equals(Role.SENSEI)) {
            throw new RuntimeException("Only SENSEI can create custom tests");
        }

        if (request.getMaxAttempts() == null || request.getMaxAttempts() >= 10 || request.getMaxAttempts() <= 0) {
            throw new RuntimeException("Max attempts must be between 1 and 9");
        }
        
        if (request.getRewardGcoin() == null || request.getRewardGcoin() < 0) {
            throw new RuntimeException("Reward G-Coin must be positive");
        }

        CustomTest customTest = CustomTest.builder()
                .title(request.getTitle())
                .testCode(request.getTestCode() != null ? request.getTestCode() : "TEST-" + System.currentTimeMillis())
                .minLevel(request.getMinLevel() != null ? request.getMinLevel() : 1)
                .sensei(sensei)
                .rewardGcoin(request.getRewardGcoin())
                .maxAttempts(request.getMaxAttempts())
                .isApproved(true)
                .build();

        final CustomTest savedCustomTest = customTestRepository.save(customTest);

        if (request.getQuestions() != null && !request.getQuestions().isEmpty()) {
            List<CustomTestQuestion> questions = request.getQuestions().stream().map(qDto -> CustomTestQuestion.builder()
                    .customTest(savedCustomTest)
                    .questionText(qDto.getQuestionText())
                    .answerA(qDto.getAnswerA())
                    .answerB(qDto.getAnswerB())
                    .answerC(qDto.getAnswerC())
                    .answerD(qDto.getAnswerD())
                    .correctAnswer(qDto.getCorrectAnswer())
                    .build()).collect(Collectors.toList());
            customTestQuestionRepository.saveAll(questions);
        }

        return mapToResponse(savedCustomTest);
    }

    public List<CustomTestResponse> getCustomTestsBySensei(Long senseiId) {
        User sensei = userRepository.findById(senseiId)
                .orElseThrow(() -> new RuntimeException("User not found"));

        if (!sensei.getRole().equals(Role.SENSEI)) {
            throw new RuntimeException("Only SENSEI can view their custom tests");
        }

        return customTestRepository.findBySenseiId(senseiId).stream()
                .map(this::mapToResponse)
                .collect(Collectors.toList());
    }

    public CustomTestResponse getCustomTestByCode(String testCode) {
        CustomTest test = customTestRepository.findByTestCode(testCode)
                .orElseThrow(() -> new RuntimeException("Test not found"));
        return mapToResponse(test);
    }

    public CustomTestResponse getCustomTestForPlayer(Long playerId, String testCode) {
        CustomTest test = customTestRepository.findByTestCode(testCode)
                .orElseThrow(() -> new RuntimeException("Test not found"));
                
        if (!Boolean.TRUE.equals(test.getIsApproved())) {
            throw new RuntimeException("Bài test này đang chờ kiểm duyệt và chưa được phép làm.");
        }
        
        long attempts = customTestAttemptRepository.countByPlayerIdAndTestId(playerId, test.getId());
        if (attempts >= test.getMaxAttempts()) {
            throw new RuntimeException("Bạn đã vượt quá số lần làm bài test này (" + test.getMaxAttempts() + " lần).");
        }
        
        return mapToResponse(test);
    }

    @Transactional
    public User submitCustomTest(Long playerId, String testCode, int score, boolean passed) {
        User player = userRepository.findById(playerId)
                .orElseThrow(() -> new RuntimeException("User not found"));
        
        CustomTest test = customTestRepository.findByTestCode(testCode)
                .orElseThrow(() -> new RuntimeException("Test not found"));
                
        long attempts = customTestAttemptRepository.countByPlayerIdAndTestId(playerId, test.getId());
        if (attempts >= test.getMaxAttempts()) {
            throw new RuntimeException("Bạn đã vượt quá số lần làm bài test này.");
        }
        
        CustomTestAttempt attempt = CustomTestAttempt.builder()
                .player(player)
                .test(test)
                .score(score)
                .passed(passed)
                .attemptTime(LocalDateTime.now())
                .build();
        customTestAttemptRepository.save(attempt);
        
        if (passed && test.getRewardGcoin() > 0) {
            player.setGCoin(player.getGCoin() + test.getRewardGcoin());
            userRepository.save(player);
        }
        
        return player;
    }

    @Transactional
    public CustomTestResponse updateCustomTest(Long senseiId, Long testId, CustomTestRequest request) {
        User sensei = userRepository.findById(senseiId)
                .orElseThrow(() -> new RuntimeException("User not found"));

        CustomTest test = customTestRepository.findById(testId)
                .orElseThrow(() -> new RuntimeException("Test not found"));

        if (!test.getSensei().getId().equals(sensei.getId())) {
            throw new RuntimeException("You do not own this test");
        }

        if (request.getMaxAttempts() != null) {
            if (request.getMaxAttempts() >= 10 || request.getMaxAttempts() <= 0) {
                throw new RuntimeException("Max attempts must be between 1 and 9");
            }
            test.setMaxAttempts(request.getMaxAttempts());
        }

        if (request.getRewardGcoin() != null) {
            if (request.getRewardGcoin() < 0) {
                throw new RuntimeException("Reward G-Coin must be positive");
            }
            test.setRewardGcoin(request.getRewardGcoin());
        }

        if (request.getTitle() != null && !request.getTitle().isEmpty()) {
            test.setTitle(request.getTitle());
        }

        if (request.getTestCode() != null && !request.getTestCode().isEmpty()) {
            test.setTestCode(request.getTestCode());
        }

        if (request.getMinLevel() != null) {
            test.setMinLevel(request.getMinLevel());
        }

        final CustomTest savedTest = customTestRepository.save(test);

        if (request.getQuestions() != null) {
            customTestQuestionRepository.deleteByCustomTestId(savedTest.getId());
            List<CustomTestQuestion> questions = request.getQuestions().stream().map(qDto -> CustomTestQuestion.builder()
                    .customTest(savedTest)
                    .questionText(qDto.getQuestionText())
                    .answerA(qDto.getAnswerA())
                    .answerB(qDto.getAnswerB())
                    .answerC(qDto.getAnswerC())
                    .answerD(qDto.getAnswerD())
                    .correctAnswer(qDto.getCorrectAnswer())
                    .build()).collect(Collectors.toList());
            customTestQuestionRepository.saveAll(questions);
        }

        return mapToResponse(savedTest);
    }

    @Transactional
    public void deleteCustomTest(Long senseiId, Long testId) {
        CustomTest test = customTestRepository.findById(testId)
                .orElseThrow(() -> new RuntimeException("Test not found"));

        if (!test.getSensei().getId().equals(senseiId)) {
            throw new RuntimeException("You do not own this test");
        }

        customTestQuestionRepository.deleteByCustomTestId(testId);
        customTestRepository.delete(test);
    }

    public List<CustomTestAttemptDTO> getTestAttempts(Long senseiId, Long testId) {
        CustomTest test = customTestRepository.findById(testId)
                .orElseThrow(() -> new RuntimeException("Test not found"));

        if (!test.getSensei().getId().equals(senseiId)) {
            throw new RuntimeException("You do not own this test");
        }

        return customTestAttemptRepository.findByTestIdOrderByAttemptTimeDesc(testId).stream()
                .map(attempt -> CustomTestAttemptDTO.builder()
                        .playerName(attempt.getPlayer().getUsername())
                        .score(attempt.getScore())
                        .passed(attempt.isPassed())
                        .attemptTime(attempt.getAttemptTime())
                        .build())
                .collect(Collectors.toList());
    }

    public CustomTestResponse mapToResponse(CustomTest test) {
        List<CustomTestQuestionDTO> questionDTOs = customTestQuestionRepository.findByCustomTestId(test.getId()).stream()
                .map(q -> CustomTestQuestionDTO.builder()
                        .id(q.getId())
                        .questionText(q.getQuestionText())
                        .answerA(q.getAnswerA())
                        .answerB(q.getAnswerB())
                        .answerC(q.getAnswerC())
                        .answerD(q.getAnswerD())
                        .correctAnswer(q.getCorrectAnswer())
                        .build())
                .collect(Collectors.toList());

        return CustomTestResponse.builder()
                .id(test.getId())
                .title(test.getTitle())
                .testCode(test.getTestCode())
                .minLevel(test.getMinLevel())
                .rewardGcoin(test.getRewardGcoin())
                .maxAttempts(test.getMaxAttempts())
                .isApproved(test.getIsApproved())
                .senseiId(test.getSensei().getId())
                .senseiUsername(test.getSensei().getUsername())
                .questions(questionDTOs)
                .build();
    }
}
