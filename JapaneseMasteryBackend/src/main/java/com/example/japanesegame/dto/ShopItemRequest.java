package com.example.japanesegame.dto;

import com.example.japanesegame.entity.ShopItemType;
import lombok.Data;

@Data
public class ShopItemRequest {
    private String name;
    private Integer price;
    private ShopItemType type;
}
