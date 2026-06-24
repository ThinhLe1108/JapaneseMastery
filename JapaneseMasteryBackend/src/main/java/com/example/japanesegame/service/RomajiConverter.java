package com.example.japanesegame.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

public class RomajiConverter {

    private static final ObjectMapper mapper = new ObjectMapper();
    private static final Map<String, String> CACHE = new ConcurrentHashMap<>();

    public static String toRomaji(String text) {
        // Return cached result immediately if available
        String cached = CACHE.get(text);
        if (cached != null) {
            return cached;
        }

        try {
            URL url = new URL("http://localhost:8081/convert");
            HttpURLConnection conn = (HttpURLConnection) url.openConnection();
            conn.setRequestMethod("POST");
            conn.setRequestProperty("Content-Type", "application/json");
            conn.setRequestProperty("Accept", "application/json");
            conn.setDoOutput(true);

            String jsonInputString = mapper.createObjectNode()
                                           .put("text", text)
                                           .toString();

            try (OutputStream os = conn.getOutputStream()) {
                byte[] input = jsonInputString.getBytes(StandardCharsets.UTF_8);
                os.write(input, 0, input.length);
            }

            int responseCode = conn.getResponseCode();
            if (responseCode == HttpURLConnection.HTTP_OK) {
                try (BufferedReader br = new BufferedReader(new InputStreamReader(conn.getInputStream(), StandardCharsets.UTF_8))) {
                    StringBuilder response = new StringBuilder();
                    String responseLine;
                    while ((responseLine = br.readLine()) != null) {
                        response.append(responseLine.trim());
                    }
                    
                    JsonNode rootNode = mapper.readTree(response.toString());
                    String romaji = rootNode.get("romaji").asText();
                    
                    // Convert Hepburn macrons to Wapuro typing format so the UI displays typeable characters
                    romaji = romaji.replace("ā", "aa")
                                   .replace("ī", "ii")
                                   .replace("ū", "uu")
                                   .replace("ē", "ee")
                                   .replace("ō", "ou");
                    
                    // Cache the successful result before returning
                    CACHE.put(text, romaji);
                    return romaji;
                }
            } else {
                System.err.println("Kuroshiro Error. Did you start the Node.js server?");
                return "Error from Kuroshiro Server: HTTP " + responseCode;
            }
        } catch (Exception e) {
            System.err.println("CRITICAL: Failed to connect to Kuroshiro Microservice.");
            e.printStackTrace();
            return "Connection Error: Is Kuroshiro Running?";
        }
    }
}

