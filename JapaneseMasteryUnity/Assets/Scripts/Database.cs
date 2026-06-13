using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class Database : MonoBehaviour
{
    private static Database _instance;
    public static Database Instance { 
        get {
            if (_instance == null) {
                _instance = UnityEngine.Object.FindObjectOfType<Database>();
                if (_instance == null) {
                    GameObject go = new GameObject("DatabaseManager");
                    _instance = go.AddComponent<Database>();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    private string SavePath;

    public Dictionary<string, object> data = new Dictionary<string, object>
    {
        { "account", new Dictionary<string, object>
            {
                { "display_name", "New Player" },
                { "current_level", 1 },
                { "g_coin_balance", 0 },
                { "role", "PLAYER" }
            }
        },
        { "memory_records", new Dictionary<string, Dictionary<string, object>>() }
    };

    private void Awake()
    {
        if (_instance == null || _instance == this)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SavePath = Path.Combine(Application.persistentDataPath, "user_data.json");
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveData()
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
    }

    public void LoadData()
    {
        if (!File.Exists(SavePath))
        {
            SaveData();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load user data: " + e.Message);
        }
    }

    public Dictionary<string, object> GetAccount()
    {
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(data["account"]));
    }

    public void UpdateAccount(string key, object value)
    {
        var account = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(data["account"]));
        account[key] = value;
        data["account"] = account;
        SaveData();
    }

    public Dictionary<string, object> GetMemoryRecord(string kanji)
    {
        var records = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(JsonConvert.SerializeObject(data["memory_records"]));
        if (records.ContainsKey(kanji))
            return records[kanji];
        return new Dictionary<string, object>();
    }

    public void UpdateMemoryRecord(string kanji, Dictionary<string, object> recordData)
    {
        var records = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(JsonConvert.SerializeObject(data["memory_records"]));
        records[kanji] = recordData;
        data["memory_records"] = records;
        SaveData();
    }
}
