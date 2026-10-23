using System.Collections.Generic;
using UnityEngine;

public class SRSManager : MonoBehaviour
{
    private static SRSManager _instance;
    public static SRSManager Instance { 
        get {
            if (_instance == null) {
                _instance = UnityEngine.Object.FindObjectOfType<SRSManager>();
                if (_instance == null) {
                    GameObject go = new GameObject("SRSManager");
                    _instance = go.AddComponent<SRSManager>();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }
    private void Awake()
    {
        if (_instance == null || _instance == this)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Dictionary<string, object> CreateDefaultRecord()
    {
        return new Dictionary<string, object>
        {
            { "meaning_strength", 0.2f },
            { "reading_strength", 0.2f },
            { "hesitation_score", 0.0f },
            { "average_reaction_time", 0.0f },
            { "total_attempts", 0 }
        };
    }

    public Dictionary<string, object> GetOrCreateRecord(string kanji)
    {
        var rec = Database.Instance.GetMemoryRecord(kanji);
        if (rec == null || rec.Count == 0)
        {
            return CreateDefaultRecord();
        }
        return rec;
    }

    public float GetOverallStrength(string kanji)
    {
        var rec = GetOrCreateRecord(kanji);
        float m = System.Convert.ToSingle(rec["meaning_strength"]);
        float r = System.Convert.ToSingle(rec["reading_strength"]);
        return (m + r) / 2.0f;
    }

    public void UpdateRecord(string kanji, string qType, bool isCorrect, float reactionTime, int hesitationCount)
    {
        var rec = GetOrCreateRecord(kanji);
        
        int attempts = System.Convert.ToInt32(rec["total_attempts"]) + 1;
        rec["total_attempts"] = attempts;

        float avgReact = System.Convert.ToSingle(rec["average_reaction_time"]);
        float hesiScore = System.Convert.ToSingle(rec["hesitation_score"]);

        if (attempts == 1)
        {
            rec["average_reaction_time"] = reactionTime;
            rec["hesitation_score"] = (float)hesitationCount;
        }
        else
        {
            rec["average_reaction_time"] = (avgReact * (attempts - 1) + reactionTime) / (float)attempts;
            rec["hesitation_score"] = (hesiScore * (attempts - 1) + (float)hesitationCount) / (float)attempts;
        }

        float timeFactor = Mathf.Max(0.2f, 1.0f - (reactionTime / 10.0f));
        float hesitationFactor = Mathf.Max(0.3f, 1.0f - (hesitationCount * 0.25f));
        float baseChange = 0.25f;

        float mStrength = System.Convert.ToSingle(rec["meaning_strength"]);
        float rStrength = System.Convert.ToSingle(rec["reading_strength"]);

        if (isCorrect)
        {
            float change = baseChange * timeFactor * hesitationFactor;
            if (qType == "meaning") rec["meaning_strength"] = Mathf.Min(1.0f, mStrength + change);
            else if (qType == "reading") rec["reading_strength"] = Mathf.Min(1.0f, rStrength + change);
        }
        else
        {
            float change = baseChange * 1.5f;
            if (qType == "meaning") rec["meaning_strength"] = Mathf.Max(0.0f, mStrength - change);
            else if (qType == "reading") rec["reading_strength"] = Mathf.Max(0.0f, rStrength - change);
        }

        Database.Instance.UpdateMemoryRecord(kanji, rec);
    }

    public bool IsMastered(string kanji)
    {
        return GetOverallStrength(kanji) >= 0.85f;
    }
}
