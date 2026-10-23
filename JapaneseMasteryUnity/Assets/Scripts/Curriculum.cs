using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class CurriculumItem
{
    public string wordJp { get; set; }
    public string kana { get; set; }
    public string romaji { get; set; }
    public string meaning { get; set; }
}

public static class Curriculum
{
    private static readonly Dictionary<int, List<CurriculumItem>> LEVELS = new Dictionary<int, List<CurriculumItem>>()
    {
        // --- Giai đoạn 1: Hiragana Cơ bản ---
        { 1, new List<CurriculumItem> {
            new CurriculumItem{kana="あ", romaji="a"}, new CurriculumItem{kana="い", romaji="i"}, new CurriculumItem{kana="う", romaji="u"}, new CurriculumItem{kana="え", romaji="e"}, new CurriculumItem{kana="お", romaji="o"},
            new CurriculumItem{kana="か", romaji="ka"}, new CurriculumItem{kana="き", romaji="ki"}, new CurriculumItem{kana="く", romaji="ku"}, new CurriculumItem{kana="け", romaji="ke"}, new CurriculumItem{kana="こ", romaji="ko"}
        }},
        { 2, new List<CurriculumItem> {
            new CurriculumItem{kana="さ", romaji="sa"}, new CurriculumItem{kana="し", romaji="shi"}, new CurriculumItem{kana="す", romaji="su"}, new CurriculumItem{kana="せ", romaji="se"}, new CurriculumItem{kana="そ", romaji="so"},
            new CurriculumItem{kana="た", romaji="ta"}, new CurriculumItem{kana="ち", romaji="chi"}, new CurriculumItem{kana="つ", romaji="tsu"}, new CurriculumItem{kana="て", romaji="te"}, new CurriculumItem{kana="と", romaji="to"}
        }},
        { 3, new List<CurriculumItem> {
            new CurriculumItem{kana="な", romaji="na"}, new CurriculumItem{kana="に", romaji="ni"}, new CurriculumItem{kana="ぬ", romaji="nu"}, new CurriculumItem{kana="ね", romaji="ne"}, new CurriculumItem{kana="の", romaji="no"},
            new CurriculumItem{kana="は", romaji="ha"}, new CurriculumItem{kana="ひ", romaji="hi"}, new CurriculumItem{kana="ふ", romaji="fu"}, new CurriculumItem{kana="へ", romaji="he"}, new CurriculumItem{kana="ほ", romaji="ho"}
        }},
        { 4, new List<CurriculumItem> {
            new CurriculumItem{kana="ま", romaji="ma"}, new CurriculumItem{kana="み", romaji="mi"}, new CurriculumItem{kana="む", romaji="mu"}, new CurriculumItem{kana="め", romaji="me"}, new CurriculumItem{kana="も", romaji="mo"},
            new CurriculumItem{kana="や", romaji="ya"}, new CurriculumItem{kana="ゆ", romaji="yu"}, new CurriculumItem{kana="よ", romaji="yo"},
            new CurriculumItem{kana="わ", romaji="wa"}, new CurriculumItem{kana="を", romaji="o"}
        }},
        { 5, new List<CurriculumItem> {
            new CurriculumItem{kana="ら", romaji="ra"}, new CurriculumItem{kana="り", romaji="ri"}, new CurriculumItem{kana="る", romaji="ru"}, new CurriculumItem{kana="れ", romaji="re"}, new CurriculumItem{kana="ろ", romaji="ro"},
            new CurriculumItem{kana="ん", romaji="n"},
            new CurriculumItem{kana="っk", romaji="kk"}, new CurriculumItem{kana="っs", romaji="ss"}, new CurriculumItem{kana="っt", romaji="tt"}, new CurriculumItem{kana="っp", romaji="pp"}
        }},
        // --- Giai đoạn 2: Biến âm - Dakuon & Handakuon ---
        { 6, new List<CurriculumItem> {
            new CurriculumItem{kana="が", romaji="ga"}, new CurriculumItem{kana="ぎ", romaji="gi"}, new CurriculumItem{kana="ぐ", romaji="gu"}, new CurriculumItem{kana="げ", romaji="ge"}, new CurriculumItem{kana="ご", romaji="go"},
            new CurriculumItem{kana="ざ", romaji="za"}, new CurriculumItem{kana="じ", romaji="ji"}, new CurriculumItem{kana="ず", romaji="zu"}, new CurriculumItem{kana="ぜ", romaji="ze"}, new CurriculumItem{kana="ぞ", romaji="zo"}
        }},
        { 7, new List<CurriculumItem> {
            new CurriculumItem{kana="だ", romaji="da"}, new CurriculumItem{kana="ぢ", romaji="ji"}, new CurriculumItem{kana="づ", romaji="zu"}, new CurriculumItem{kana="で", romaji="de"}, new CurriculumItem{kana="ど", romaji="do"},
            new CurriculumItem{kana="ば", romaji="ba"}, new CurriculumItem{kana="び", romaji="bi"}, new CurriculumItem{kana="ぶ", romaji="bu"}, new CurriculumItem{kana="べ", romaji="be"}, new CurriculumItem{kana="ぼ", romaji="bo"}
        }},
        { 8, new List<CurriculumItem> {
            new CurriculumItem{kana="ぱ", romaji="pa"}, new CurriculumItem{kana="ぴ", romaji="pi"}, new CurriculumItem{kana="ぷ", romaji="pu"}, new CurriculumItem{kana="ぺ", romaji="pe"}, new CurriculumItem{kana="ぽ", romaji="po"},
            new CurriculumItem{kana="ああ", romaji="aa"}, new CurriculumItem{kana="いい", romaji="ii"}, new CurriculumItem{kana="うう", romaji="uu"}, new CurriculumItem{kana="ええ", romaji="ee"}, new CurriculumItem{kana="おお", romaji="oo"}
        }},
        // --- Giai đoạn 3: Âm ghép & Trường âm mở rộng ---
        { 9, new List<CurriculumItem> {
            new CurriculumItem{kana="えい", romaji="ei"}, new CurriculumItem{kana="おう", romaji="ou"},
            new CurriculumItem{kana="きゃ", romaji="kya"}, new CurriculumItem{kana="きゅ", romaji="kyu"}, new CurriculumItem{kana="きょ", romaji="kyo"},
            new CurriculumItem{kana="ぎゃ", romaji="gya"}, new CurriculumItem{kana="ぎゅ", romaji="gyu"}, new CurriculumItem{kana="ぎょ", romaji="gyo"}
        }},
        { 10, new List<CurriculumItem> {
            new CurriculumItem{kana="しゃ", romaji="sha"}, new CurriculumItem{kana="しゅ", romaji="shu"}, new CurriculumItem{kana="しょ", romaji="sho"},
            new CurriculumItem{kana="じゃ", romaji="ja"}, new CurriculumItem{kana="じゅ", romaji="ju"}, new CurriculumItem{kana="じょ", romaji="jo"},
            new CurriculumItem{kana="ちゃ", romaji="cha"}, new CurriculumItem{kana="ちゅ", romaji="chu"}, new CurriculumItem{kana="ちょ", romaji="cho"}
        }},
        { 11, new List<CurriculumItem> {
            new CurriculumItem{kana="にゃ", romaji="nya"}, new CurriculumItem{kana="にゅ", romaji="nyu"}, new CurriculumItem{kana="にょ", romaji="nyo"},
            new CurriculumItem{kana="ひゃ", romaji="hya"}, new CurriculumItem{kana="ひゅ", romaji="hyu"}, new CurriculumItem{kana="ひょ", romaji="hyo"},
            new CurriculumItem{kana="びゃ", romaji="bya"}, new CurriculumItem{kana="びゅ", romaji="byu"}, new CurriculumItem{kana="びょ", romaji="byo"}
        }},
        { 12, new List<CurriculumItem> {
            new CurriculumItem{kana="ぴゃ", romaji="pya"}, new CurriculumItem{kana="ぴゅ", romaji="pyu"}, new CurriculumItem{kana="ぴょ", romaji="pyo"},
            new CurriculumItem{kana="みゃ", romaji="mya"}, new CurriculumItem{kana="みゅ", romaji="myu"}, new CurriculumItem{kana="みょ", romaji="myo"},
            new CurriculumItem{kana="りゃ", romaji="rya"}, new CurriculumItem{kana="りゅ", romaji="ryu"}, new CurriculumItem{kana="りょ", romaji="ryo"}
        }},
        // --- Giai đoạn 4: Katakana Cơ bản ---
        { 13, new List<CurriculumItem> {
            new CurriculumItem{kana="ア", romaji="a"}, new CurriculumItem{kana="イ", romaji="i"}, new CurriculumItem{kana="ウ", romaji="u"}, new CurriculumItem{kana="エ", romaji="e"}, new CurriculumItem{kana="オ", romaji="o"},
            new CurriculumItem{kana="カ", romaji="ka"}, new CurriculumItem{kana="キ", romaji="ki"}, new CurriculumItem{kana="ク", romaji="ku"}, new CurriculumItem{kana="ケ", romaji="ke"}, new CurriculumItem{kana="コ", romaji="ko"}
        }},
        { 14, new List<CurriculumItem> {
            new CurriculumItem{kana="サ", romaji="sa"}, new CurriculumItem{kana="シ", romaji="shi"}, new CurriculumItem{kana="ス", romaji="su"}, new CurriculumItem{kana="セ", romaji="se"}, new CurriculumItem{kana="ソ", romaji="so"},
            new CurriculumItem{kana="タ", romaji="ta"}, new CurriculumItem{kana="チ", romaji="chi"}, new CurriculumItem{kana="ツ", romaji="tsu"}, new CurriculumItem{kana="テ", romaji="te"}, new CurriculumItem{kana="ト", romaji="to"}
        }},
        { 15, new List<CurriculumItem> {
            new CurriculumItem{kana="ナ", romaji="na"}, new CurriculumItem{kana="ニ", romaji="ni"}, new CurriculumItem{kana="ヌ", romaji="nu"}, new CurriculumItem{kana="ネ", romaji="ne"}, new CurriculumItem{kana="ノ", romaji="no"},
            new CurriculumItem{kana="ハ", romaji="ha"}, new CurriculumItem{kana="ヒ", romaji="hi"}, new CurriculumItem{kana="フ", romaji="fu"}, new CurriculumItem{kana="ヘ", romaji="he"}, new CurriculumItem{kana="ホ", romaji="ho"}
        }},
        { 16, new List<CurriculumItem> {
            new CurriculumItem{kana="マ", romaji="ma"}, new CurriculumItem{kana="ミ", romaji="mi"}, new CurriculumItem{kana="ム", romaji="mu"}, new CurriculumItem{kana="メ", romaji="me"}, new CurriculumItem{kana="モ", romaji="mo"},
            new CurriculumItem{kana="ヤ", romaji="ya"}, new CurriculumItem{kana="ユ", romaji="yu"}, new CurriculumItem{kana="ヨ", romaji="yo"},
            new CurriculumItem{kana="ワ", romaji="wa"}, new CurriculumItem{kana="ヲ", romaji="wo"}
        }},
        { 17, new List<CurriculumItem> {
            new CurriculumItem{kana="ラ", romaji="ra"}, new CurriculumItem{kana="リ", romaji="ri"}, new CurriculumItem{kana="ル", romaji="ru"}, new CurriculumItem{kana="レ", romaji="re"}, new CurriculumItem{kana="ロ", romaji="ro"},
            new CurriculumItem{kana="ン", romaji="n"},
            new CurriculumItem{kana="っk", romaji="kk"}, new CurriculumItem{kana="っs", romaji="ss"}, new CurriculumItem{kana="っt", romaji="tt"}, new CurriculumItem{kana="っp", romaji="pp"}
        }},
        // --- Giai đoạn 5: Biến âm Katakana ---
        { 18, new List<CurriculumItem> {
            new CurriculumItem{kana="ガ", romaji="ga"}, new CurriculumItem{kana="ギ", romaji="gi"}, new CurriculumItem{kana="グ", romaji="gu"}, new CurriculumItem{kana="ゲ", romaji="ge"}, new CurriculumItem{kana="ゴ", romaji="go"},
            new CurriculumItem{kana="ザ", romaji="za"}, new CurriculumItem{kana="ジ", romaji="ji"}, new CurriculumItem{kana="ズ", romaji="zu"}, new CurriculumItem{kana="ゼ", romaji="ze"}, new CurriculumItem{kana="ゾ", romaji="zo"}
        }},
        { 19, new List<CurriculumItem> {
            new CurriculumItem{kana="ダ", romaji="da"}, new CurriculumItem{kana="ヂ", romaji="ji"}, new CurriculumItem{kana="ヅ", romaji="zu"}, new CurriculumItem{kana="デ", romaji="de"}, new CurriculumItem{kana="ド", romaji="do"},
            new CurriculumItem{kana="バ", romaji="ba"}, new CurriculumItem{kana="ビ", romaji="bi"}, new CurriculumItem{kana="ブ", romaji="bu"}, new CurriculumItem{kana="ベ", romaji="be"}, new CurriculumItem{kana="ボ", romaji="bo"}
        }},
        { 20, new List<CurriculumItem> {
            new CurriculumItem{kana="パ", romaji="pa"}, new CurriculumItem{kana="ピ", romaji="pi"}, new CurriculumItem{kana="プ", romaji="pu"}, new CurriculumItem{kana="ペ", romaji="pe"}, new CurriculumItem{kana="ポ", romaji="po"},
            new CurriculumItem{kana="アー", romaji="aa"}, new CurriculumItem{kana="イー", romaji="ii"}, new CurriculumItem{kana="ウー", romaji="uu"}, new CurriculumItem{kana="エー", romaji="ee"}, new CurriculumItem{kana="オー", romaji="oo"}
        }},
        // --- Giai đoạn 6: Âm ghép Katakana ---
        { 21, new List<CurriculumItem> {
            new CurriculumItem{kana="キャ", romaji="kya"}, new CurriculumItem{kana="キュ", romaji="kyu"}, new CurriculumItem{kana="キョ", romaji="kyo"},
            new CurriculumItem{kana="ギャ", romaji="gya"}, new CurriculumItem{kana="ギュ", romaji="gyu"}, new CurriculumItem{kana="ギョ", romaji="gyo"},
            new CurriculumItem{kana="シャ", romaji="sha"}, new CurriculumItem{kana="シュ", romaji="shu"}, new CurriculumItem{kana="ショ", romaji="sho"}
        }},
        { 22, new List<CurriculumItem> {
            new CurriculumItem{kana="ジャ", romaji="ja"}, new CurriculumItem{kana="ジュ", romaji="ju"}, new CurriculumItem{kana="ジョ", romaji="jo"},
            new CurriculumItem{kana="チャ", romaji="cha"}, new CurriculumItem{kana="チュ", romaji="chu"}, new CurriculumItem{kana="チョ", romaji="cho"},
            new CurriculumItem{kana="ニャ", romaji="nya"}, new CurriculumItem{kana="ニュ", romaji="nyu"}, new CurriculumItem{kana="ニョ", romaji="nyo"}
        }},
        { 23, new List<CurriculumItem> {
            new CurriculumItem{kana="ヒャ", romaji="hya"}, new CurriculumItem{kana="ヒュ", romaji="hyu"}, new CurriculumItem{kana="ヒョ", romaji="hyo"},
            new CurriculumItem{kana="ビャ", romaji="bya"}, new CurriculumItem{kana="ビュ", romaji="byu"}, new CurriculumItem{kana="びょ", romaji="byo"},
            new CurriculumItem{kana="ピゃ", romaji="pya"}, new CurriculumItem{kana="ピゅ", romaji="pyu"}, new CurriculumItem{kana="ピょ", romaji="pyo"}
        }},
        { 24, new List<CurriculumItem> {
            new CurriculumItem{kana="ミャ", romaji="mya"}, new CurriculumItem{kana="ミュ", romaji="myu"}, new CurriculumItem{kana="ミョ", romaji="myo"},
            new CurriculumItem{kana="リゃ", romaji="rya"}, new CurriculumItem{kana="リュ", romaji="ryu"}, new CurriculumItem{kana="リョ", romaji="ryo"}
        }}
    };

    private static Dictionary<string, List<CurriculumItem>> extended_levels = new Dictionary<string, List<CurriculumItem>>();
    private static bool is_json_loaded = false;

    public static List<CurriculumItem> GetLevel(int level)
    {
        if (LEVELS.ContainsKey(level))
        {
            return new List<CurriculumItem>(LEVELS[level]);
        }

        if (!is_json_loaded)
        {
            LoadExtendedJson();
        }

        string key = level.ToString();
        if (extended_levels.ContainsKey(key))
        {
            return new List<CurriculumItem>(extended_levels[key]);
        }

        return new List<CurriculumItem>();
    }

    private static void LoadExtendedJson()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "data", "jmdict_levels.json");
        if (!File.Exists(filePath)) filePath = Path.Combine(Application.streamingAssetsPath, "data", "jmdict_levels.json");

        if (File.Exists(filePath))
        {
            try
            {
                string content = File.ReadAllText(filePath);
                extended_levels = JsonConvert.DeserializeObject<Dictionary<string, List<CurriculumItem>>>(content);
                is_json_loaded = true;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Curriculum: JSON Parse Error: " + e.Message);
            }
        }
    }
}
