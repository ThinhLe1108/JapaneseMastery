package com.example.japanesegame.service;

import com.cloudinary.Cloudinary;
import com.cloudinary.utils.ObjectUtils;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import jakarta.annotation.PostConstruct;
import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.HashMap;
import java.util.Map;

@Service
public class CloudinaryService {

    private Cloudinary cloudinary;

    @PostConstruct
    public void init() {
        Map<String, String> env = new HashMap<>();
        try (BufferedReader br = new BufferedReader(new FileReader(".env"))) {
            String line;
            while ((line = br.readLine()) != null) {
                if (line.contains("=")) {
                    String[] parts = line.split("=", 2);
                    env.put(parts[0].trim(), parts[1].trim());
                }
            }
        } catch (Exception e) {
            System.err.println("Could not load .env file: " + e.getMessage());
        }

        cloudinary = new Cloudinary(ObjectUtils.asMap(
                "cloud_name", env.get("CLOUDINARY_CLOUD_NAME"),
                "api_key", env.get("CLOUDINARY_API_KEY"),
                "api_secret", env.get("CLOUDINARY_API_SECRET"),
                "secure", true
        ));
    }

    public Map uploadFile(MultipartFile file) throws IOException {
        return cloudinary.uploader().upload(file.getBytes(), ObjectUtils.asMap(
                "resource_type", "auto"
        ));
    }
    
    public String generateOptimizedUrl(String publicId) {
        return cloudinary.url()
                .transformation(new com.cloudinary.Transformation().quality("auto").fetchFormat("auto"))
                .generate(publicId);
    }
}
