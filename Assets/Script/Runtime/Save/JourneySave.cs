using System;
using UnityEngine;

[Serializable]
public class JourneySaveData
{
    public int version = 1;
    public int stageIndex;
    public int checkpointSegment;
    public int completedStages;
    public int memoryMask;
    public int demonMask;
    public int routeSeed;
    public int deaths;
    public float elapsedSeconds;
    public bool shortJourney;
    public bool finished;
    // Optional extension: legacy v1 saves without this field remain valid.
    public LifeEventSaveData lifeEvents = new LifeEventSaveData();
    public bool IsValid => version == 1 && stageIndex >= 0 && stageIndex < 6 && checkpointSegment >= 0 && checkpointSegment < 10000 && deaths >= 0 && !float.IsNaN(elapsedSeconds) && !float.IsInfinity(elapsedSeconds) && elapsedSeconds >= 0 && completedStages >= 0 && completedStages <= 63 && demonMask >= 0 && demonMask <= 63 && memoryMask >= 0 && memoryMask < (1 << 18) && (!finished || (stageIndex == 5 && completedStages == 63))
        && (lifeEvents == null || (lifeEvents.IsValid && (lifeEvents.pending == null || (!finished && lifeEvents.pending.stageIndex == stageIndex))));

    public bool HasMemory(int stage, int slot) => (memoryMask & (1 << (stage * 3 + slot))) != 0;
    public bool Remember(int stage, int slot)
    {
        if (stage < 0 || stage > 5 || slot < 0 || slot > 2 || HasMemory(stage, slot))
            return false;
        memoryMask |= 1 << (stage * 3 + slot);
        return true;
    }

    public int MemoryCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < 18; i++)
                if ((memoryMask & (1 << i)) != 0)
                    count++;
            return count;
        }
    }
}

public static class JourneySave
{
    // Automated smoke tests use separate keys and never overwrite the player's journey.
    static string Key => Array.IndexOf(Environment.GetCommandLineArgs(), "-journeySmoke") >= 0 ? "runner.lifejourney.smoke" : "runner.lifejourney.v1";
    static string BackupKey => Key + ".backup";

    public static JourneySaveData Decode(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            var data = JsonUtility.FromJson<JourneySaveData>(json);
            if (data != null && data.lifeEvents == null) data.lifeEvents = new LifeEventSaveData();
            return data != null && data.IsValid ? data : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static JourneySaveData Load()
    {
        return Decode(PlayerPrefs.GetString(Key, "")) ?? Decode(PlayerPrefs.GetString(BackupKey, ""));
    }

    public static void Write(JourneySaveData data)
    {
        if (data == null || !data.IsValid)
            return;
        string json = JsonUtility.ToJson(data);
        // Keep two complete records; a corrupt primary record can be recovered on next launch.
        PlayerPrefs.SetString(BackupKey, json);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }
}
