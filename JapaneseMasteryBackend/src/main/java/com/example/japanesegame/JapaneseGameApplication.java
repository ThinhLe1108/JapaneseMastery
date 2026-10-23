package com.example.japanesegame;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

@SpringBootApplication
@EnableScheduling
public class JapaneseGameApplication {

	public static void main(String[] args) {
		SpringApplication.run(JapaneseGameApplication.class, args);
	}

}
