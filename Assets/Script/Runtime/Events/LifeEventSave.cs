using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LifeEventChoiceRecord
{
    public string eventId;
    public int stageIndex;
    public string title;
    public string prompt;
    public string optionId;
    public string answer;
    public string traitName;
    public string plannedEffect;
    public string resultText;
    public string narrativeFlag;
}

[Serializable]
public class LifeEventPending
{
    public LifeEventDefinition snapshot;
    public int stageIndex;
    public int selectedIndex = -1;
    public bool committed;
    public float x, y, z, velocityX, velocityY;
}

[Serializable]
public class LifeEventSaveData
{
    public int schemaVersion = 1;
    public List<LifeEventChoiceRecord> choices = new List<LifeEventChoiceRecord>();
    // JsonUtility serializes null inline classes as empty objects. An explicit discriminator
    // prevents an empty placeholder from becoming a phantom event on the next load.
    public bool hasPending;
    [SerializeField] LifeEventPending pendingSnapshot;
    public LifeEventPending pending
    {
        get => hasPending ? pendingSnapshot : null;
        set { pendingSnapshot = value; hasPending = value != null; }
    }

    public bool HasChoice(string eventId)
    {
        if (choices == null) return false;
        return choices.Exists(item => item != null && string.Equals(item.eventId, eventId, StringComparison.Ordinal));
    }

    public bool IsValid
    {
        get
        {
            if (schemaVersion != 1 || choices == null || choices.Count > 4096) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in choices)
                if (item == null || string.IsNullOrWhiteSpace(item.eventId) || string.IsNullOrWhiteSpace(item.optionId)
                    || item.stageIndex < 0 || item.stageIndex > 5 || !ids.Add(item.eventId)) return false;
            if (pending == null) return !hasPending;
            string error;
            if (pending.snapshot == null || !pending.snapshot.IsValid(out error) || pending.stageIndex < 0 || pending.stageIndex > 5
                || pending.stageIndex != (int)pending.snapshot.stage || pending.selectedIndex < -1 || pending.selectedIndex > 2
                || !Finite(pending.x) || !Finite(pending.y) || !Finite(pending.z)
                || !Finite(pending.velocityX) || !Finite(pending.velocityY)) return false;
            if (pending.committed)
            {
                var record = choices.Find(item => item.eventId == pending.snapshot.eventId);
                return pending.selectedIndex >= 0 && record != null
                    && record.optionId == pending.snapshot.options[pending.selectedIndex].optionId;
            }
            return !HasChoice(pending.snapshot.eventId);
        }
    }

    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    // The domain guard is necessary even when UI buttons are already disabled.
    public bool Commit(int index)
    {
        if (!IsValid || pending == null || pending.committed || index < 0 || index > 2
            || HasChoice(pending.snapshot.eventId)) return false;
        var item = pending.snapshot;
        var option = item.options[index];
        choices.Add(new LifeEventChoiceRecord
        {
            eventId = item.eventId, stageIndex = pending.stageIndex, title = item.title, prompt = item.prompt,
            optionId = option.optionId, answer = option.answer, traitName = option.traitName,
            plannedEffect = option.plannedEffect, resultText = option.resultText, narrativeFlag = option.narrativeFlag
        });
        pending.selectedIndex = index;
        pending.committed = true;
        return true;
    }
}
