using System;

// Invoked by JourneyLogicTests; exercises the domain transaction rather than mirroring the UI.
public static class LifeEventLogicTests
{
    public static int Run()
    {
        int assertions = 0;
        Action<bool, string> check = (condition, message) =>
        {
            assertions++;
            if (!condition) throw new Exception("Life events: " + message);
        };
        var definition = new LifeEventDefinition
        {
            eventId = "Y02", stage = LifeStage.YoungAdult, title = "父母催婚", prompt = "你会如何选择？",
            options = new[]
            {
                new LifeEventOption { optionId = "Y02_A", answer = "结婚", traitName = "有人同行", plannedEffect = "守护 +1", narrativeFlag = "MarryNow" },
                new LifeEventOption { optionId = "Y02_B", answer = "稳事业后再结婚", traitName = "先立足", plannedEffect = "节律 +8%", narrativeFlag = "CareerFirst" },
                new LifeEventOption { optionId = "Y02_C", answer = "不结婚", traitName = "自定人生", plannedEffect = "预警 +0.15 秒", narrativeFlag = "StayUnmarried" }
            }
        };
        string error;
        check(definition.IsValid(out error), "three complete options must be legal");
        foreach (int selected in new[] { 0, 1, 2 })
        {
            var data = new LifeEventSaveData { pending = new LifeEventPending { snapshot = definition, stageIndex = 3 } };
            check(data.IsValid, "unselected pending event must be valid");
            check(!data.Commit(-1) && !data.Commit(3) && data.choices.Count == 0, "invalid choices cannot mutate data");
            check(data.Commit(selected), "every answer must be selectable");
            check(data.choices.Count == 1 && data.pending.committed && data.pending.selectedIndex == selected, "commit must be atomic");
            check(data.choices[0].optionId == definition.options[selected].optionId, "selected answer must be preserved");
            check(data.choices[0].narrativeFlag == definition.options[selected].narrativeFlag, "narrative markers must be preserved");
            for (int i = 0; i < 10; i++) check(!data.Commit(selected), "repeated callback cannot duplicate rewards");
            check(data.IsValid && data.HasChoice("Y02"), "committed result must remain serializable");
            data.pending = null;
            check(data.IsValid && data.HasChoice("Y02"), "closing result cannot clear history");
            data.pending = new LifeEventPending { snapshot = definition, stageIndex = 3 };
            check(!data.IsValid && !data.Commit(selected), "duplicate trigger cannot bypass one-per-life rule");
        }
        var invalid = new LifeEventSaveData { pending = new LifeEventPending { snapshot = definition, stageIndex = 2 } };
        check(!invalid.IsValid, "pending stage mismatch must be rejected");
        invalid.pending.stageIndex = 3;
        invalid.pending.x = float.NaN;
        check(!invalid.IsValid, "invalid restore position must be rejected");
        invalid.pending.x = 0;
        invalid.pending.committed = true;
        check(!invalid.IsValid, "result without matching history must be rejected");
        definition.options[2].optionId = "Y02_A";
        check(!definition.IsValid(out error), "duplicate option IDs must be rejected");
        definition.options = new LifeEventOption[2];
        check(!definition.IsValid(out error), "two-card event must be rejected");
        check(new LifeEventSaveData().IsValid, "empty extension for old saves must be legal");
        return assertions;
    }
}
