package com.example.japanesegame.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import lombok.Builder;

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

    @Column(nullable = false)
    private String name;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private ShopItemType type;

    @Column(nullable = false)
    private Integer price;

    @Column
    private String cloudinaryPublicId;

    @Column
    private String secureUrl;

    @ManyToOne(fetch = FetchType.EAGER)
    @JoinColumn(name = "designer_id")
    @com.fasterxml.jackson.annotation.JsonIgnore
    private User designer;

    @Column(name = "is_approved", nullable = false)
    @Builder.Default
    private boolean isApproved = true; // Temporarily disabled moderator role
}
