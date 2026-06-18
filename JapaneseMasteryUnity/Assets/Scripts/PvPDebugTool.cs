using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PvPDebugTool : MonoBehaviour
{
    private PvPManager pvp;
    private string testJp = "これはテストの文章です。";
    private string testRomaji = "korewa tesuto no bunshou desu.";
    
    private float mockOppProgress = 0f;
    private bool isSimulating = false;

    private void Start()
    {
        pvp = PvPManager.Instance;
        if (pvp == null) {
            Debug.LogError("PvPManager Instance not found!");
        }
    }

    private void Update()
    {
        if (isSimulating && pvp != null)
        {
            mockOppProgress += Time.deltaTime * 0.05f; // 5% per second
            if (mockOppProgress > 1.0f) mockOppProgress = 1.0f;
            
            // Inject into PvPManager via reflection if needed, but PvPManager handles it in PollStatus
            // For true debug simulation, we just need to keep triggering HandleMatchState
        }
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(10, 10, 350, 600), GUI.skin.box);
        GUILayout.Label("<b>PvP Debug Tool</b>", GUILayout.Width(330));

        if (GUILayout.Button("1. Trigger COUNTDOWN (Normal)"))
        {
            MockMatch("Normal Match", testJp, testRomaji, "COUNTDOWN");
        }

        if (GUILayout.Button("2. Trigger PLAYING (Long text)"))
        {
            string longJp = "宗教学は、「宗教」という研究対象に対し、様々な研究方法を用いて研究が進められている。個々の研究は宗教学の研究であると同時に社会学・心理学・文化人類学等それぞれの研究であるとも言える。スラヴ人には独自の文化と神話があった。世界は、自然の法則を支配する天の神々と、人々の習慣や行動を支配する地下の神々という、2つの反対の力によって支配されていると信じられていた。";
            MockMatch("Long Match", longJp, "shoukyougaku wa...", "PLAYING");
        }

        if (GUILayout.Button("3. Trigger PLAYING (Symbols)"))
        {
            MockMatch("Symbol Match", "Test <noparse> & ! ?", "test noparse & ! ?", "PLAYING");
        }

        if (GUILayout.Button("Toggle Opponent Simulation"))
        {
            isSimulating = !isSimulating;
            if (!isSimulating) mockOppProgress = 0;
        }

        if (isSimulating) {
            GUILayout.Label($"Simulating Opponent: {(mockOppProgress*100):F0}%");
            if (GUILayout.Button("Update Simulated Match State")) {
                 UpdateSimulatedState();
            }
        }

        if (GUILayout.Button("Trigger FINISHED (Win)"))
        {
            MockMatch("debug", testJp, testRomaji, "FINISHED", Global.UserId);
        }

        if (GUILayout.Button("Trigger FINISHED (Loss)"))
        {
            MockMatch("debug", testJp, testRomaji, "FINISHED", -99);
        }

        if (GUILayout.Button("Hide All Panels"))
        {
             var method = pvp.GetType().GetMethod("HideAllPanels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
             method.Invoke(pvp, null);
        }

        GUILayout.EndArea();
    }

    private void UpdateSimulatedState()
    {
        var data = new Dictionary<string, object> {
            { "status", "MATCHED" },
            { "matchId", "debug-id" },
            { "paragraph", testJp },
            { "romajiParagraph", testRomaji },
            { "state", "PLAYING" },
            { "myProgress", 5 }, // Just some dummy chars
            { "oppProgress", (int)(mockOppProgress * testRomaji.Length) },
            { "countdown", 0 }
        };
        InvokeHandleMatchState(data);
    }

    private void MockMatch(string id, string jp, string romaji, string state, long winnerId = -1)
    {
        var data = new Dictionary<string, object> {
            { "status", "MATCHED" },
            { "matchId", id },
            { "paragraph", jp },
            { "romajiParagraph", romaji },
            { "state", state },
            { "countdown", 5000 },
            { "myProgress", 0 },
            { "oppProgress", 0 },
            { "winnerId", winnerId },
            { "reward", 100 }
        };
        InvokeHandleMatchState(data);
    }

    private void InvokeHandleMatchState(Dictionary<string, object> data)
    {
        var method = pvp.GetType().GetMethod("HandleMatchState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null) {
            method.Invoke(pvp, new object[] { data });
        } else {
            Debug.LogError("Could not find HandleMatchState method!");
        }
    }
}
