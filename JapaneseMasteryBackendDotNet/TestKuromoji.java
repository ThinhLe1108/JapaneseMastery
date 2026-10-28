import com.atilika.kuromoji.ipadic.Token;
import com.atilika.kuromoji.ipadic.Tokenizer;

public class TestKuromoji {
    public static void main(String[] args) {
        Tokenizer tokenizer = new Tokenizer();
        String text = "近年の分類体系では、コケ植物が側系統であると考えられていたことを反映し、コケ植物に含まれる蘚類、苔類、ツノゴケ類のそれぞれを門の階級に置く分類が用いられてきた。";
        for (Token token : tokenizer.tokenize(text)) {
            System.out.println(token.getSurface() + " -> Reading: " + token.getReading() + ", BaseForm: " + token.getBaseForm());
        }
    }
}
