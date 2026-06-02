package com.example.japanesegame.entity;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "shop_items")
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class ShopItem {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "designer_id", nullable = false)
    private User designer;

    @Column(nullable = false)
    private String name;

    @Column(nullable = false)
    private Integer price;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private ShopItemType type;

    @Column(name = "is_approved", nullable = false)
    @Builder.Default
    private Boolean isApproved = false;
}
