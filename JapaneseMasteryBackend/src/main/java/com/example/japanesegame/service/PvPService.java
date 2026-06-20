package com.example.japanesegame.service;

import com.example.japanesegame.dto.PvPMatch;
import com.example.japanesegame.entity.User;
import com.example.japanesegame.entity.Vocabulary;
import com.example.japanesegame.repository.UserRepository;
import jakarta.annotation.PostConstruct;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

import java.io.BufferedReader;
import java.io.FileReader;
import java.time.LocalDateTime;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.stream.Collectors;

@Service
public class PvPService {

    @Autowired
    private UserRepository userRepository;
    
    @Autowired
    private JMdictService jmdictService;

    private List<String> paragraphs = new ArrayList<>();
    private Random random = new Random();

    // Queues
    private ConcurrentLinkedQueue<Long> queueUnder25 = new ConcurrentLinkedQueue<>();
    private ConcurrentLinkedQueue<Long> queueOver24 = new ConcurrentLinkedQueue<>();
    
    private ConcurrentHashMap<String, PvPMatch> activeMatches = new ConcurrentHashMap<>();
    private ConcurrentHashMap<Long, String> userMatchMap = new ConcurrentHashMap<>();

    // Lobby system
    private ConcurrentHashMap<String, Long> lobbies = new ConcurrentHashMap<>(); // lobbyCode -> hostUserId

    @PostConstruct
    public void init() {
        String filePath = "pvp_paragraphs.txt";
        java.io.File file = new java.io.File(filePath);
        if (!file.exists()) {
            System.err.println("PvP Paragraphs file not found at: " + file.getAbsolutePath());
        }

        try (BufferedReader br = new BufferedReader(new FileReader(file))) {
            String line;
            while ((line = br.readLine()) != null) {
                String trimmed = sanitizeWikiText(line.trim());
                if (!trimmed.isEmpty() && trimmed.length() <= 150) {
                    paragraphs.add(trimmed);
                }
            }
        } catch (Exception e) {
            paragraphs.add("これはテストの文章です。");
        }
        
        if (paragraphs.isEmpty()) {
            paragraphs.add("これはテストの文章です。");
        }
    }

    private String sanitizeWikiText(String text) {
        if (text == null) return "";
        // Remove common wiki artifacts
        text = text.replaceAll("\\{\\{.*?\\}\\}", "")
                   .replaceAll("\\[\\[(.*?\\|)?(.*?)\\]\\]", "$2")
                   .replaceAll("<ref.*?>.*?</ref>", "")
                   .replaceAll("<.*?>", "")
                   .replace("\r", "")
                   .replace("\n", " ");
        
        // Whitelist: ASCII, Japanese, common punctuation
        StringBuilder sb = new StringBuilder();
        for (char c : text.toCharArray()) {
            if ((c >= 0x00 && c <= 0x7F) || // ASCII
                (c >= 0x3000 && c <= 0x303F) || // JP Punctuation
                (c >= 0x3040 && c <= 0x309F) || // Hiragana
                (c >= 0x30A0 && c <= 0x30FF) || // Katakana
                (c >= 0x4E00 && c <= 0x9FAF) || // Kanji
                (c >= 0xFF00 && c <= 0xFFEF)) { // Full-width
                sb.append(c);
            }
        }
        return sb.toString().trim();
    }

    public synchronized String joinQueue(Long userId) {
        if (userMatchMap.containsKey(userId)) {
            // Check if match is already FINISHED and needs clearing
            String matchId = userMatchMap.get(userId);
            PvPMatch match = activeMatches.get(matchId);
            if (match == null || "FINISHED".equals(match.getState())) {
                acknowledgeFinish(userId); // Clear state
            } else {
                return "ALREADY_IN_MATCH";
            }
        }
        
        User user = userRepository.findById(userId).orElse(null);
        if (user == null) return "ERROR";
        
        if (user.getPvpBanUntil() != null && user.getPvpBanUntil().isAfter(LocalDateTime.now())) {
            return "BANNED";
        }
        
        if (user.getLevel() < 25) {
            if (!queueUnder25.contains(userId)) queueUnder25.add(userId);
        } else {
            if (!queueOver24.contains(userId)) queueOver24.add(userId);
        }
        return "QUEUED";
    }

    public synchronized void leaveQueue(Long userId) {
        queueUnder25.remove(userId);
        queueOver24.remove(userId);
    }

    @Scheduled(fixedRate = 1000)
    public void matchPlayers() {
        processQueue(queueUnder25, true);
        processQueue(queueOver24, false);
    }
    
    private void processQueue(ConcurrentLinkedQueue<Long> queue, boolean isUnder25) {
        if (isUnder25) {
            while (!queue.isEmpty()) {
                Long p1 = queue.poll();
                if (p1 == null) break;

                String matchId = UUID.randomUUID().toString();
                PvPMatch match = new PvPMatch();
                match.setMatchId(matchId);
                match.setPlayer1Id(p1);
                match.setPlayer2Id(-1L); // -1 is BOT
                
                String jp = getRandomWordsJp(20);
                if (jp == null || jp.trim().isEmpty()) jp = "テスト 単語";
                match.setParagraph(jp);
                
                String rp = getRandomWordsRomaji(jp);
                if (rp == null || rp.trim().isEmpty()) rp = "error";
                match.setRomajiParagraph(rp);
                
                match.setState("COUNTDOWN"); // 3 seconds countdown
                match.setP1TimeMs(System.currentTimeMillis() + 3000); 
                
                activeMatches.put(matchId, match);
                userMatchMap.put(p1, matchId);
            }
        } else {
            while (queue.size() >= 2) {
                Long p1 = queue.poll();
                Long p2 = queue.poll();
                if (p1 == null || p2 == null) break;

                String matchId = UUID.randomUUID().toString();
                PvPMatch match = new PvPMatch();
                match.setMatchId(matchId);
                match.setPlayer1Id(p1);
                match.setPlayer2Id(p2);
                
                String p = getRandomParagraph();
                if (p == null || p.trim().isEmpty()) p = "これはテストの文章です。";
                p = p.replace("\r", "").replace("\n", " ").trim();
                match.setParagraph(p);
                
                String rp = RomajiConverter.toRomaji(p);
                if (rp == null || rp.trim().isEmpty()) rp = "error";
                match.setRomajiParagraph(rp);
                
                match.setState("COUNTDOWN"); // 3 seconds countdown
                match.setP1TimeMs(System.currentTimeMillis() + 3000); 
                
                activeMatches.put(matchId, match);
                userMatchMap.put(p1, matchId);
                userMatchMap.put(p2, matchId);
            }
        }
    }

    private String getRandomParagraph() {
        if (paragraphs.isEmpty()) return "エラー";
        return paragraphs.get(random.nextInt(paragraphs.size()));
    }
    
    private String getRandomWordsJp(int count) {
        List<Vocabulary> all = jmdictService.getAllVocabularies();
        List<Vocabulary> valid = all.stream().filter(v -> v.getLevelRequired() <= 24).collect(Collectors.toList());
        if (valid.isEmpty()) return "テスト 単語";
        Collections.shuffle(valid);
        return valid.stream().limit(count)
            .map(v -> v.getWordJp() != null && !v.getWordJp().isEmpty() ? v.getWordJp() : v.getKana())
            .collect(Collectors.joining(" "));
    }

    private String getRandomWordsRomaji(String jp) {
        return RomajiConverter.toRomaji(jp);
    }

    public Map<String, Object> getStatus(Long userId) {
        Map<String, Object> response = new HashMap<>();
        String matchId = userMatchMap.get(userId);
        
        if (matchId == null) {
            if (queueUnder25.contains(userId) || queueOver24.contains(userId)) {
                response.put("status", "QUEUED");
            } else {
                response.put("status", "NONE");
            }
            return response;
        }

        PvPMatch match = activeMatches.get(matchId);
        if (match == null) {
            userMatchMap.remove(userId);
            response.put("status", "NONE");
            return response;
        }

        // Check countdown
        if ("COUNTDOWN".equals(match.getState())) {
            long remaining = match.getP1TimeMs() - System.currentTimeMillis();
            if (remaining <= 0) {
                match.setState("PLAYING");
                match.setP1TimeMs(0L);
                match.setP2TimeMs(0L);
                match.setStartTimeMs(System.currentTimeMillis());
            } else {
                response.put("countdown", remaining);
            }
        } else if ("PLAYING".equals(match.getState()) && match.getPlayer2Id() == -1L) {
            // Simulate bot progress
            long elapsed = System.currentTimeMillis() - match.getStartTimeMs();
            // Let's say bot types 3 characters per second
            int botProgress = (int)(elapsed / 333); 
            if (botProgress > match.getRomajiParagraph().length()) {
                botProgress = match.getRomajiParagraph().length();
            }
            match.setP2TimeMs((long) botProgress);
            
            // Bot finish logic
            if (botProgress == match.getRomajiParagraph().length()) {
                finishMatchByMatchId(-1L, matchId);
            }
        }

        response.put("status", "MATCHED");
        response.put("matchId", match.getMatchId());
        response.put("paragraph", match.getParagraph());
        response.put("romajiParagraph", match.getRomajiParagraph());
        response.put("state", match.getState());
        
        boolean isP1 = userId.equals(match.getPlayer1Id());
        Long opponentId = isP1 ? match.getPlayer2Id() : match.getPlayer1Id();
        response.put("opponentId", opponentId);
        
        if (opponentId == -1L) {
            response.put("opponentName", "BOT");
        } else {
            User opponent = userRepository.findById(opponentId).orElse(null);
            response.put("opponentName", opponent != null ? opponent.getUsername() : "Opponent");
        }
        
        // Progress uses timeMs fields for progress temporarily during PLAYING
        response.put("myProgress", isP1 ? match.getP1TimeMs() : match.getP2TimeMs());
        response.put("oppProgress", isP1 ? match.getP2TimeMs() : match.getP1TimeMs());
        
        if ("FINISHED".equals(match.getState())) {
            response.put("winnerId", match.getWinnerId());
            response.put("reward", match.getGCoinReward());
        }

        return response;
    }
    
    public synchronized String updateProgress(Long userId, int progress) {
        String matchId = userMatchMap.get(userId);
        if (matchId == null) return "NO_MATCH";
        PvPMatch match = activeMatches.get(matchId);
        if (match == null || !"PLAYING".equals(match.getState())) return "NOT_PLAYING";
        
        if (userId.equals(match.getPlayer1Id())) match.setP1TimeMs((long)progress);
        else if (userId.equals(match.getPlayer2Id())) match.setP2TimeMs((long)progress);
        
        return "OK";
    }

    public synchronized String finishMatchByMatchId(Long winnerId, String matchId) {
        PvPMatch match = activeMatches.get(matchId);
        if (match == null || !"PLAYING".equals(match.getState())) return "NOT_PLAYING";

        // First one to finish wins
        match.setState("FINISHED");
        match.setWinnerId(winnerId);
        
        // Reward winner
        if (winnerId != -1L) {
            User winner = userRepository.findById(winnerId).orElse(null);
            if (winner != null) {
                winner.setGCoin(winner.getGCoin() + 100);
                userRepository.save(winner);
            }
        }
        
        // Punish loser
        Long loserId = winnerId.equals(match.getPlayer1Id()) ? match.getPlayer2Id() : match.getPlayer1Id();
        if (loserId != -1L) {
            User loser = userRepository.findById(loserId).orElse(null);
            if (loser != null) {
                if (loser.getGCoin() >= 100) {
                    loser.setGCoin(loser.getGCoin() - 100);
                } else {
                    loser.setGCoin(0);
                    loser.setPvpBanUntil(LocalDateTime.now().plusHours(1));
                }
                userRepository.save(loser);
            }
        }

        return "FINISHED";
    }

    public synchronized String finishMatch(Long userId, Long timeTaken) {
        String matchId = userMatchMap.get(userId);
        if (matchId == null) return "NO_MATCH";
        return finishMatchByMatchId(userId, matchId);
    }

    public synchronized String quitMatch(Long userId) {
        String matchId = userMatchMap.get(userId);
        if (matchId == null) return "NO_MATCH";

        PvPMatch match = activeMatches.get(matchId);
        if (match == null) return "NO_MATCH";
        if ("FINISHED".equals(match.getState())) return "ALREADY_FINISHED";

        // Punish quitter
        User quitter = userRepository.findById(userId).orElse(null);
        if (quitter != null) {
            if (quitter.getGCoin() >= 100) {
                quitter.setGCoin(quitter.getGCoin() - 100);
            } else {
                quitter.setGCoin(0);
            }
            quitter.setPvpBanUntil(LocalDateTime.now().plusHours(1)); // Quitting ALWAYS bans for 1 hr
            userRepository.save(quitter);
        }

        // Award opponent
        Long opponentId = userId.equals(match.getPlayer1Id()) ? match.getPlayer2Id() : match.getPlayer1Id();
        if (opponentId != -1L) {
            User opponent = userRepository.findById(opponentId).orElse(null);
            if (opponent != null) {
                opponent.setGCoin(opponent.getGCoin() + 100);
                userRepository.save(opponent);
            }
        }

        match.setWinnerId(opponentId);
        match.setState("FINISHED");

        return "QUIT_SUCCESS";
    }

    public synchronized void acknowledgeFinish(Long userId) {
        String matchId = userMatchMap.get(userId);
        if (matchId != null) {
            PvPMatch match = activeMatches.get(matchId);
            if (match != null && "FINISHED".equals(match.getState())) {
                userMatchMap.remove(userId);
                Long opponentId = userId.equals(match.getPlayer1Id()) ? match.getPlayer2Id() : match.getPlayer1Id();
                if (!userMatchMap.containsKey(opponentId)) {
                    activeMatches.remove(matchId);
                }
            }
        }
    }

    // ============ LOBBY SYSTEM ============

    public synchronized Map<String, Object> createLobby(Long userId) {
        Map<String, Object> response = new HashMap<>();
        
        // Check if user is already in a match
        if (userMatchMap.containsKey(userId)) {
            String matchId = userMatchMap.get(userId);
            PvPMatch match = activeMatches.get(matchId);
            if (match != null && !"FINISHED".equals(match.getState())) {
                response.put("error", "ALREADY_IN_MATCH");
                return response;
            }
            acknowledgeFinish(userId);
        }

        // Check ban
        User user = userRepository.findById(userId).orElse(null);
        if (user == null) {
            response.put("error", "USER_NOT_FOUND");
            return response;
        }
        if (user.getPvpBanUntil() != null && user.getPvpBanUntil().isAfter(LocalDateTime.now())) {
            response.put("error", "BANNED");
            return response;
        }

        // Remove any existing lobby by this user
        lobbies.entrySet().removeIf(entry -> entry.getValue().equals(userId));

        // Generate unique code
        String code = generateLobbyCode();
        while (lobbies.containsKey(code)) {
            code = generateLobbyCode();
        }

        lobbies.put(code, userId);
        response.put("lobbyCode", code);
        response.put("status", "LOBBY_CREATED");
        return response;
    }

    public synchronized Map<String, Object> joinLobby(String code, Long userId) {
        Map<String, Object> response = new HashMap<>();

        if (!lobbies.containsKey(code)) {
            response.put("error", "LOBBY_NOT_FOUND");
            return response;
        }

        Long hostId = lobbies.get(code);
        if (hostId.equals(userId)) {
            response.put("error", "CANNOT_JOIN_OWN_LOBBY");
            return response;
        }

        // Check if joiner is already in a match
        if (userMatchMap.containsKey(userId)) {
            String existingMatch = userMatchMap.get(userId);
            PvPMatch em = activeMatches.get(existingMatch);
            if (em != null && !"FINISHED".equals(em.getState())) {
                response.put("error", "ALREADY_IN_MATCH");
                return response;
            }
            acknowledgeFinish(userId);
        }

        // Check ban for joiner
        User joiner = userRepository.findById(userId).orElse(null);
        if (joiner == null) {
            response.put("error", "USER_NOT_FOUND");
            return response;
        }
        if (joiner.getPvpBanUntil() != null && joiner.getPvpBanUntil().isAfter(LocalDateTime.now())) {
            response.put("error", "BANNED");
            return response;
        }

        // Remove lobby entry
        lobbies.remove(code);

        // Also remove host from random queues if they were in one
        queueUnder25.remove(hostId);
        queueOver24.remove(hostId);
        queueUnder25.remove(userId);
        queueOver24.remove(userId);

        // Create match between host and joiner
        String matchId = UUID.randomUUID().toString();
        PvPMatch match = new PvPMatch();
        match.setMatchId(matchId);
        match.setPlayer1Id(hostId);
        match.setPlayer2Id(userId);

        String p = getRandomParagraph();
        if (p == null || p.trim().isEmpty()) p = "これはテストの文章です。";
        p = p.replace("\r", "").replace("\n", " ").trim();
        match.setParagraph(p);

        String rp = RomajiConverter.toRomaji(p);
        if (rp == null || rp.trim().isEmpty()) rp = "error";
        match.setRomajiParagraph(rp);

        match.setState("COUNTDOWN");
        match.setP1TimeMs(System.currentTimeMillis() + 3000);

        activeMatches.put(matchId, match);
        userMatchMap.put(hostId, matchId);
        userMatchMap.put(userId, matchId);

        response.put("status", "MATCH_CREATED");
        response.put("matchId", matchId);
        return response;
    }

    public synchronized void cancelLobby(String code, Long userId) {
        Long hostId = lobbies.get(code);
        if (hostId != null && hostId.equals(userId)) {
            lobbies.remove(code);
        }
    }

    private String generateLobbyCode() {
        String chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No I/O/0/1 to avoid confusion
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 6; i++) {
            sb.append(chars.charAt(random.nextInt(chars.length())));
        }
        return sb.toString();
    }
}
