using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LifeEventOption
{
    [Tooltip("稳定选项 ID；发布后不要改名。")]
    public string optionId;
    public string answer;
    public string traitName;
    [TextArea(2, 6), Tooltip("策划中的能力说明。本版本只展示，不执行效果。")]
    public string plannedEffect;
    [TextArea(2, 5)] public string resultText;
    public string narrativeFlag;
}

[Serializable]
public class LifeEventDefinition
{
    public string eventId;
    public LifeStage stage;
    public string title;
    [TextArea(2, 6)] public string prompt;
    public LifeEventOption[] options = new LifeEventOption[3];

    public bool IsValid(out string error)
    {
        if (string.IsNullOrWhiteSpace(eventId) || eventId != eventId.Trim() || (int)stage < 0 || (int)stage > 5
            || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(prompt))
        { error = "事件 ID、阶段、标题或正文无效。"; return false; }
        if (options == null || options.Length != 3)
        { error = eventId + " 必须恰好包含三个选项。"; return false; }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in options)
            if (option == null || string.IsNullOrWhiteSpace(option.optionId) || option.optionId != option.optionId.Trim()
                || !ids.Add(option.optionId) || string.IsNullOrWhiteSpace(option.answer)
                || string.IsNullOrWhiteSpace(option.traitName) || string.IsNullOrWhiteSpace(option.plannedEffect))
            { error = eventId + " 的选项 ID 重复或卡牌内容缺失。"; return false; }
        error = null;
        return true;
    }
}

[CreateAssetMenu(menuName = "Life Journey/人生事件配置", fileName = "LifeEventSettings")]
public class LifeEventSettings : ScriptableObject
{
    [Header("全部事件：ID 区分大小写，发布后保持稳定")]
    public LifeEventDefinition[] events = new LifeEventDefinition[0];
    [Header("界面与调试参数")]
    [Range(0, 5), Tooltip("确认后继续前行时的恢复倒计时。")]
    public int resumeCountdownSeconds = 3;
    public bool keyboardShortcuts = true;
    [Tooltip("只控制日志，不改变触发或存档规则。")]
    public bool verboseLogging;
    [Range(18, 34)] public int cardFontSize = 24;
    [Range(0, 1)] public float backdropOpacity = 0.94f;
    [Tooltip("空则使用现有界面的系统中文字体。")]
    public Font fontOverride;

    public static LifeEventSettings Load() => Resources.Load<LifeEventSettings>("LifeEventSettings");

    public LifeEventDefinition Find(string id)
    {
        LifeEventDefinition found = null;
        if (events == null) return null;
        foreach (var item in events)
            if (item != null && string.Equals(item.eventId, id, StringComparison.Ordinal))
            {
                // Ambiguous IDs must never silently select the first item.
                if (found != null) return null;
                found = item;
            }
        return found;
    }

    public List<string> ValidateContent()
    {
        var errors = new List<string>();
        var eventIds = new HashSet<string>(StringComparer.Ordinal);
        var optionIds = new HashSet<string>(StringComparer.Ordinal);
        if (events == null || events.Length == 0) errors.Add("事件库为空。");
        foreach (var item in events ?? new LifeEventDefinition[0])
        {
            string error;
            if (item == null) { errors.Add("事件库存在空元素。"); continue; }
            if (!item.IsValid(out error)) errors.Add(error);
            if (!eventIds.Add(item.eventId ?? "")) errors.Add("事件 ID 重复：" + item.eventId);
            foreach (var option in item.options ?? new LifeEventOption[0])
                if (option != null && !optionIds.Add(option.optionId ?? ""))
                    errors.Add("全局选项 ID 重复：" + option.optionId);
        }
        return errors;
    }
}
