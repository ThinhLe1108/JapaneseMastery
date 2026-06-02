package com.example.japanesegame.repository;

import com.example.japanesegame.entity.ShopItem;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface ShopItemRepository extends JpaRepository<ShopItem, Long> {
    List<ShopItem> findByDesignerIdOrderByIdDesc(Long designerId);
    List<ShopItem> findByIsApprovedFalse();
}
