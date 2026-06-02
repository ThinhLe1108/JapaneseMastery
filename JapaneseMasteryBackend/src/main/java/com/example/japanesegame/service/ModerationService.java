package com.example.japanesegame.service;

import com.example.japanesegame.dto.CustomTestResponse;
import com.example.japanesegame.entity.CustomTest;
import com.example.japanesegame.entity.Role;
import com.example.japanesegame.entity.ShopItem;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.repository.CustomTestRepository;
import com.example.japanesegame.repository.ShopItemRepository;
import com.example.japanesegame.repository.UserRepository;
import com.example.japanesegame.repository.CustomTestQuestionRepository;
import com.example.japanesegame.repository.CustomTestAttemptRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor
public class ModerationService {

    private final UserRepository userRepository;
    private final CustomTestRepository customTestRepository;
    private final ShopItemRepository shopItemRepository;
    private final CustomTestService customTestService;
    private final CustomTestQuestionRepository customTestQuestionRepository;
    private final CustomTestAttemptRepository customTestAttemptRepository;

    private User validateModerator(Long moderatorId) {
        User mod = userRepository.findById(moderatorId)
                .orElseThrow(() -> new RuntimeException("User not found"));
        if (!mod.getRole().equals(Role.MODERATOR) && !mod.getRole().equals(Role.ADMIN)) {
            throw new RuntimeException("Only MODERATOR or ADMIN can perform this action");
        }
        return mod;
    }

    public List<CustomTestResponse> getPendingTests(Long moderatorId) {
        validateModerator(moderatorId);
        return customTestRepository.findByIsApprovedFalse().stream()
                .map(customTestService::mapToResponse)
                .collect(Collectors.toList());
    }

    public List<CustomTestResponse> getApprovedTests(Long moderatorId) {
        validateModerator(moderatorId);
        return customTestRepository.findByIsApprovedTrue().stream()
                .map(customTestService::mapToResponse)
                .collect(Collectors.toList());
    }

    @Transactional
    public CustomTestResponse approveTest(Long moderatorId, Long testId, boolean approve) {
        User mod = validateModerator(moderatorId);
        CustomTest test = customTestRepository.findById(testId)
                .orElseThrow(() -> new RuntimeException("Test not found"));

        if (test.getSensei().getId().equals(mod.getId())) {
            throw new RuntimeException("Kiểm duyệt viên không được tự duyệt nội dung do mình tạo ra!");
        }

        if (approve) {
            test.setIsApproved(true);
            CustomTest savedTest = customTestRepository.save(test);
            return customTestService.mapToResponse(savedTest);
        } else {
            // Delete constraints first
            customTestQuestionRepository.deleteByCustomTestId(testId);
            // We need to delete attempts too if any exist (though usually pending tests don't have attempts)
            // But just in case:
            customTestAttemptRepository.deleteByTestId(testId);
            
            customTestRepository.delete(test);
            return null;
        }
    }

    public List<ShopItem> getPendingItems(Long moderatorId) {
        validateModerator(moderatorId);
        return shopItemRepository.findByIsApprovedFalse();
    }

    @Transactional
    public ShopItem approveItem(Long moderatorId, Long itemId, boolean approve) {
        User mod = validateModerator(moderatorId);
        ShopItem item = shopItemRepository.findById(itemId)
                .orElseThrow(() -> new RuntimeException("Item not found"));

        if (item.getDesigner().getId().equals(mod.getId())) {
            throw new RuntimeException("Kiểm duyệt viên không được tự duyệt nội dung do mình tạo ra!");
        }

        if (approve) {
            item.setIsApproved(true);
            return shopItemRepository.save(item);
        } else {
            shopItemRepository.delete(item);
            return null;
        }
    }
}
