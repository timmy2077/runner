using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(500)]
public class LifeEventDirector : MonoBehaviour
{
    public LifeEventSettings Settings { get; private set; }
    public LifeEventPending Pending => preview ?? journey.Save?.lifeEvents?.pending;
    public bool IsPreview => preview != null;
    public int SelectedIndex => Pending != null ? Pending.selectedIndex : -1;
    public string LastStatus { get; private set; } = "等待进入事件触发器";

    LifeJourneySession journey;
    JourneyEventPanel panel;
    LifeEventPending preview;
    JourneyState previewReturnState;
    readonly List<LifeEventTrigger> requests = new List<LifeEventTrigger>();
    readonly HashSet<int> warned = new HashSet<int>();
    bool focused = true;

    void Awake()
    {
        journey = GetComponent<LifeJourneySession>();
        Settings = LifeEventSettings.Load();
        panel = gameObject.AddComponent<JourneyEventPanel>();
        panel.Initialize(this, journey);
    }

    public void Request(LifeEventTrigger trigger)
    {
        if (!requests.Contains(trigger)) requests.Add(trigger);
    }

    void LateUpdate()
    {
        requests.RemoveAll(item => item == null || !item.isActiveAndEnabled || !item.HasPlayerInside
            || item.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        if (!journey.IsPlaying || journey.Save == null || Pending != null) return;
        foreach (var trigger in requests)
        {
            if (journey.Save.lifeEvents.HasChoice(trigger.eventId)) continue;
            var item = Settings != null ? Settings.Find(trigger.eventId) : null;
            string error = null;
            if (item == null || !item.IsValid(out error) || (int)item.stage != journey.StageIndex || !trigger.Area.enabled)
            {
                if (warned.Add(trigger.GetInstanceID()))
                    Debug.LogWarning("人生事件点 " + trigger.name + " 无法触发：" + (error ?? "事件 ID 不存在/重复、阶段不匹配或碰撞器未启用。"), trigger);
                continue;
            }
            if (trigger.requireGrounded && (PlayerController.Instance == null || !PlayerController.Instance.IsGrounded)) continue;
            var pending = Snapshot(item);
            journey.Save.lifeEvents.pending = pending;
            journey.Persist();
            OpenPending();
            LastStatus = "已触发 " + item.eventId + " · " + trigger.name;
            if (Settings.verboseLogging) Debug.Log(LastStatus, trigger);
            break;
        }
    }

    LifeEventPending Snapshot(LifeEventDefinition item)
    {
        var player = PlayerController.Instance;
        var position = player != null ? player.transform.position : Vector3.zero;
        var body = player != null ? player.GetComponent<Rigidbody2D>() : null;
        var velocity = body != null ? body.velocity : Vector2.zero;
        return new LifeEventPending
        {
            snapshot = JsonUtility.FromJson<LifeEventDefinition>(JsonUtility.ToJson(item)),
            stageIndex = (int)item.stage, selectedIndex = -1,
            x = position.x, y = position.y, z = position.z, velocityX = velocity.x, velocityY = velocity.y
        };
    }

    public bool RestorePending()
    {
        if (journey.Save?.lifeEvents?.pending == null) return false;
        if (journey.Save.lifeEvents.pending.stageIndex != journey.StageIndex) return false;
        OpenPending();
        return true;
    }

    void OpenPending()
    {
        journey.SetEventState(Pending.committed ? JourneyState.EventResult : JourneyState.EventChoice);
        panel.Show();
    }

    public void Select(int index)
    {
        if (!focused || journey.State != JourneyState.EventChoice || Pending == null || Pending.committed || index < 0 || index > 2) return;
        Pending.selectedIndex = index;
        if (!IsPreview) journey.Persist();
        panel.RefreshSelection();
    }

    public void Confirm()
    {
        if (!focused || journey.State != JourneyState.EventChoice || Pending == null || SelectedIndex < 0) return;
        if (IsPreview) Pending.committed = true;
        else
        {
            if (!journey.Save.lifeEvents.Commit(SelectedIndex)) return;
            journey.Persist();
        }
        journey.SetEventState(JourneyState.EventResult);
        panel.Show();
        LastStatus = "已选择 " + Pending.snapshot.options[SelectedIndex].optionId + (IsPreview ? "（预览，未存档）" : "（仅记录，未应用能力）");
        if (Settings != null && Settings.verboseLogging) Debug.Log(LastStatus);
    }

    public void Continue()
    {
        if (!focused || journey.State != JourneyState.EventResult || Pending == null) return;
        if (IsPreview) { ClosePreview(); return; }
        journey.Save.lifeEvents.pending = null;
        journey.Persist();
        panel.Hide();
        journey.ResumeAfterEvent(Settings != null ? Settings.resumeCountdownSeconds : 3);
    }

    public void SaveAndReturn()
    {
        if (!focused) return;
        if (IsPreview) { ClosePreview(); return; }
        panel.Hide();
        journey.ReturnToMenu();
    }

    // Used only by editor tooling; does not create choices, consume nodes or persist a pending event.
    public bool Preview(string eventId, out string error)
    {
        error = null;
        if (!journey.IsStageScene || (journey.State != JourneyState.Playing && journey.State != JourneyState.Paused))
        { error = "请先进入关卡并关闭开场界面；仅能在跑酷或暂停状态预览。"; return false; }
        var item = Settings != null ? Settings.Find(eventId) : null;
        if (item == null) { error = "找不到唯一事件 ID：" + eventId; return false; }
        if (!item.IsValid(out error)) return false;
        if (journey.Save?.lifeEvents?.pending != null) { error = "请先完成真实的待选事件。"; return false; }
        previewReturnState = journey.State;
        preview = Snapshot(item);
        OpenPending();
        return true;
    }

    public void ClosePreview()
    {
        if (!IsPreview) return;
        preview = null;
        panel.Hide();
        if (previewReturnState == JourneyState.Paused) journey.PauseAfterEventPreview();
        else journey.ResumeAfterEvent(Settings != null ? Settings.resumeCountdownSeconds : 3);
    }

    public void ClearRuntime()
    {
        preview = null;
        requests.Clear();
        warned.Clear();
        panel.Hide();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Batch-mode validation has no foreground window; interactive input still requires focus.
        focused = hasFocus || Application.isBatchMode;
        if (!hasFocus && Pending != null && !IsPreview) journey.Persist();
    }
}
