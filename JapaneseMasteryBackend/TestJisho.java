import org.springframework.web.client.RestTemplate;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

public class TestJisho {
    public static void main(String[] args) {
        try {
            int level = 25;
            String keyword = "jlpt-n5";
            int offsetLevel = level - 25;
            int page = (offsetLevel / 2) + 1;
            String url = "https://jisho.org/api/v1/search/words?keyword=" + keyword + "&page=" + page;
            System.out.println("URL: " + url);
            RestTemplate restTemplate = new RestTemplate();
            String response = restTemplate.getForObject(url, String.class);
            ObjectMapper mapper = new ObjectMapper();
            JsonNode rootNode = mapper.readTree(response);
            JsonNode dataNode = rootNode.path("data");
            System.out.println("Data size: " + dataNode.size());
            System.out.println("First item: " + dataNode.get(0).path("japanese").get(0).path("word").asText());
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
