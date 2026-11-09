using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JourneyUI : MonoBehaviour
{
    LifeJourneySession journey;
    Font font;
    RectTransform canvas, hud, modal, content;
    Text stageLabel, progressLabel, messageLabel, hazardLabel, demonLabel, countdownLabel;
    Image progressFill, fearShade, bossFill;
    GameObject bossTrack;
    float messageUntil, hazardUntil;
    bool countingDown;
    readonly Color ink = new Color(0.09f, 0.1f, 0.12f, 0.92f);
    readonly Color paper = new Color(0.96f, 0.94f, 0.88f, 1);
    void Awake()
    {
        journey = GetComponent<LifeJourneySession>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC", "Arial Unicode MS", "Arial" }, 24);
        var root = new GameObject("Journey Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<RectTransform>();
        var layer = root.GetComponent<Canvas>();
        layer.renderMode = RenderMode.ScreenSpaceOverlay;
        layer.sortingOrder = 2000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        hud = FullRect("Journey HUD", canvas);
        modal = FullRect("Journey Dialog", canvas);
        modal.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.04f, 0.05f, 0.88f);
        content = Rect("Content", modal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 650));
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 20, 20);
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        hud.gameObject.SetActive(false);
        modal.gameObject.SetActive(false);
        AudioListener.volume = PlayerPrefs.GetFloat("runner.journey.volume", 0.8f);
    }

    void Update()
    {
        if (journey == null || stageLabel == null || !hud.gameObject.activeSelf)
            return;
        if (journey.Profile != null)
            stageLabel.text = journey.Profile.title + "    " + (journey.Save.shortJourney ? "短篇旅程" : "人生旅程");
        progressLabel.text = Mathf.FloorToInt(journey.Progress * 100) + "%   ·   回忆 " + journey.Save.MemoryCount + "/18";
        progressFill.fillAmount = journey.Progress;
        messageLabel.gameObject.SetActive(Time.unscaledTime < messageUntil);
        hazardLabel.gameObject.SetActive(Time.unscaledTime < hazardUntil && journey.IsPlaying);
        demonLabel.text = journey.EncounterActive && journey.Boss != null ? "心魔 · " + journey.Boss.Definition.title + "   " + (journey.Profile.HasCombatEncounter ? journey.Boss.Health + "/" + journey.Boss.MaxHealth + "   " : "") + (journey.Boss.SecondPhase ? "第二阶段 · " : "") + journey.Boss.Status : "";
        bossTrack.SetActive(journey.EncounterActive && journey.Boss != null && journey.Profile.HasCombatEncounter);
        if (journey.Boss != null && journey.Boss.MaxHealth > 0)
            bossFill.fillAmount = (float)journey.Boss.Health / journey.Boss.MaxHealth;
        bool fear = journey.EncounterActive && (journey.Profile.demon == MindDemon.Fear || journey.Profile.demon == MindDemon.Confusion);
        fearShade.color = new Color(0.02f, 0.025f, 0.03f, fear ? 0.18f : 0);
    }

    public void ShowMenu()
    {
        ClearHeldInput();
        var save = JourneySave.Load();
        Panel("走过这一生", "从第一次爬行，到最后一次小跳。\n有些能力会到来，也会离开。你可以慢下来，继续走。", 120);
        if (save != null)
            Button(save.finished ? "回到这一生的结尾" : "继续人生 · " + journey.Settings.stages[save.stageIndex].title, journey.ContinueJourney);
        Button("开启人生 · 标准旅程（约 60 分钟）", () => ConfirmNewJourney(false));
        Button("开启人生 · 短篇旅程（约 12 分钟）", () => ConfirmNewJourney(true));
        if (save != null)
            Button("回看已记住的瞬间", () => ShowMemoryPage(0, ShowMenu));
        Button("退出游戏", Quit);
    }

    public void ConfirmNewJourney(bool shortJourney)
    {
        if (JourneySave.Load() == null)
        {
            journey.NewJourney(shortJourney);
            return;
        }

        Panel("重活一世", "这会从婴儿期重新开始，并覆盖当前旅程的进度与回忆。\n\n准备好，再出发。", 150);
        Button("从头开始", () => journey.NewJourney(shortJourney));
        Button("保留这一生", RestorePanel);
    }

    public void ShowIntroduction(LifeStageProfile profile)
    {
        string change = "";
        if (profile.stage == LifeStage.YoungAdult)
            change = "\n\n冲刺离开了。你仍然能跳，也可以停下来等待。";
        if (profile.stage == LifeStage.MiddleAge)
            change = "\n\n二段跳离开了。接下来的路，一次落稳，再迈一步。";
        if (profile.stage == LifeStage.OldAge)
            change = "\n\n脚步慢了，跳跃也变小了。这段路，允许你慢慢走。";
        Panel(profile.title, profile.introduction + change + "\n\n" + profile.Controls, 230);
        Button("继续这一程", journey.BeginStage);
        Button("保存并返回", journey.ReturnToMenu);
    }

    public void ShowHUD()
    {
        countingDown = false;
        modal.gameObject.SetActive(false);
        ClearChildren(hud);
        hud.gameObject.SetActive(true);
        fearShade = FullRect("Mind atmosphere", hud).gameObject.AddComponent<Image>();
        fearShade.color = Color.clear;
        fearShade.raycastTarget = false;
        var top = Rect("Header", hud, new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(1240, 94));
        top.gameObject.AddComponent<Image>().color = ink;
        stageLabel = FixedText(top, "Stage", new Vector2(0, 1), new Vector2(18, -14), new Vector2(700, 35), 25);
        progressLabel = FixedText(top, "Progress", new Vector2(1, 1), new Vector2(-150, -20), new Vector2(370, 30), 20);
        progressLabel.alignment = TextAnchor.MiddleRight;
        var bar = Rect("Progress track", top, new Vector2(0.5f, 0), new Vector2(0, 13), new Vector2(1200, 5));
        bar.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);
        progressFill = FullRect("Progress fill", bar).gameObject.AddComponent<Image>();
        progressFill.color = paper;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        var pause = FixedButton(top, "暂停", new Vector2(1, 1), new Vector2(-12, -12), new Vector2(110, 42), journey.TogglePause);
        pause.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };
        demonLabel = FixedText(hud, "Demon", new Vector2(0.5f, 1), new Vector2(0, -126), new Vector2(850, 36), 22);
        demonLabel.alignment = TextAnchor.MiddleCenter;
        var healthTrack = Rect("Boss health", hud, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(440, 8));
        healthTrack.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        bossTrack = healthTrack.gameObject;
        bossFill = FullRect("Boss health fill", healthTrack).gameObject.AddComponent<Image>();
        bossFill.color = new Color(0.78f, 0.49f, 0.39f);
        bossFill.type = Image.Type.Filled;
        bossFill.fillMethod = Image.FillMethod.Horizontal;
        messageLabel = FixedText(hud, "Memory text", new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(1160, 52), 23);
        messageLabel.alignment = TextAnchor.MiddleCenter;
        hazardLabel = FixedText(hud, "Hazard warning", new Vector2(0.5f, 0), new Vector2(0, 82), new Vector2(1160, 28), 23);
        hazardLabel.alignment = TextAnchor.MiddleCenter;
        var controls = Rect("Touch controls", hud, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(740, 56));
        var row = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 14;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;
        AddHold(controls, "停下 · Z", value => PlayerController.Instance?.SetTouchStop(value));
        if (journey.Profile.CanJump)
            AddButton(controls, "跳跃 · 空格", () => PlayerController.Instance?.RequestJump(), 50);
        if (journey.Profile.canSprint)
            AddHold(controls, "冲刺 · Shift", value => PlayerController.Instance?.SetTouchSprint(value));
        if (journey.Profile.canSlide)
            AddHold(controls, "俯身 · C", value => PlayerController.Instance?.SetTouchSlide(value));
        if (journey.Profile.HasCombatEncounter)
            AddHold(controls, "攻击 · J", value => PlayerController.Instance?.SetTouchAttack(value));
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void ShowDeath(string text)
    {
        Panel("这一程，暂时停下了", text + "\n\n你走过的安全路段已经记下。", 160);
        Button("继续这一程 · 从检查点出发", journey.RetryCheckpoint);
        Button("重活一世 · 从婴儿期开始", () => ConfirmNewJourney(journey.Save.shortJourney));
        Button("保存并返回", journey.ReturnToMenu);
    }

    public void ShowPause()
    {
        countingDown = false;
        Panel("歇一会儿", journey.Profile.title + "    回忆 " + journey.Save.MemoryCount + "/18\n" + journey.Profile.Controls, 100);
        Button("继续走", journey.Resume);
        Button("从这一程的检查点重试", journey.RetryCheckpoint);
        Button("回看记住的瞬间", () => ShowMemoryPage(journey.StageIndex, ShowPause));
        Button("回看人生抉择", () => ShowEventHistory(0, ShowPause));
        AddVolume();
        Button("保存并返回", journey.ReturnToMenu);
    }

    public void ShowResumeCountdown()
    {
        if (countingDown)
            return;
        countingDown = true;
        Panel("慢慢来", "准备继续这一程。", 90);
        countdownLabel = Label(content, "3", 60, 100);
    }

    public void SetCountdown(int value)
    {
        if (countdownLabel != null)
            countdownLabel.text = value.ToString();
    }

    public void ShowTransition(LifeStageProfile previous, LifeStageProfile next)
    {
        Panel(previous.title + " → " + next.title, previous.departureText + "\n\n" + next.introduction, 220);
        Button("走向" + next.title, journey.NextStage);
        Button("保存并返回", journey.ReturnToMenu);
    }

    public void ShowEnding()
    {
        Panel("这一生，已经走过", journey.Settings.endingText + "\n\n记住的瞬间：" + journey.Save.MemoryCount + "/18", 240);
        Button("回看这一生", () => ShowMemoryPage(0, ShowEnding));
        Button("这一生的选择", () => ShowEventHistory(0, ShowEnding));
        Button("重活一世", () => ConfirmNewJourney(journey.Save.shortJourney));
        Button("回到起点", journey.ReturnToMenu);
    }

    public void ShowEventHistory(int index, Action back)
    {
        var records = (journey.Save ?? JourneySave.Load())?.lifeEvents?.choices;
        if (records == null || records.Count == 0)
        {
            Panel("人生抉择", "还没有记下的选择。走进人生事件点，留下你的回答。", 160);
            Button("返回", back);
            return;
        }
        index = Mathf.Clamp(index, 0, records.Count - 1);
        var item = records[index];
        Panel("人生抉择 " + (index + 1) + "/" + records.Count + " · " + item.title,
            item.prompt + "\n\n你的回答：" + item.answer + "\n经历：" + item.traitName
            + "\n" + item.plannedEffect + "\n\n经历已记录，能力效果暂未启用。", 290);
        int current = index;
        if (index > 0) Button("上一个选择", () => ShowEventHistory(current - 1, back));
        if (index + 1 < records.Count) Button("下一个选择", () => ShowEventHistory(current + 1, back));
        Button("返回", back);
    }

    void ShowMemoryPage(int stage, Action back)
    {
        var save = journey.Save ?? JourneySave.Load();
        var profile = journey.Settings.stages[stage];
        var text = new StringBuilder();
        for (int i = 0; i < 3; i++)
            text.AppendLine(save != null && save.HasMemory(stage, i) ? profile.memories[i] : "—— 还有一个瞬间，留在那段路上。 ——").AppendLine();
        Panel(profile.title + " · 回忆", text.ToString(), 230);
        if (stage > 0)
            Button("上一程", () => ShowMemoryPage(stage - 1, back));
        if (stage < 5)
            Button("下一程", () => ShowMemoryPage(stage + 1, back));
        Button("返回", back);
    }

    public void ShowLoading()
    {
        Panel("正在走向下一程", "请稍候……", 130);
    }

    public bool IsShowingDialog => (modal != null && modal.gameObject.activeSelf)
        || journey.State == JourneyState.EventChoice || journey.State == JourneyState.EventResult;

    public void HideModal()
    {
        countingDown = false;
        modal.gameObject.SetActive(false);
    }

    public void HideForEvent()
    {
        HideModal();
        hud.gameObject.SetActive(false);
    }

    public void Notify(string text, float seconds)
    {
        if (messageLabel != null)
            messageLabel.text = text;
        messageUntil = Time.unscaledTime + seconds;
    }

    public void SetHazardHint(string text, float seconds)
    {
        if (hazardLabel != null)
            hazardLabel.text = text;
        hazardUntil = Time.unscaledTime + seconds;
    }

    public void ClearHeldInput()
    {
        PlayerController.Instance?.ClearInput();
    }

    void RestorePanel()
    {
        switch (journey.State)
        {
            case JourneyState.Dead:
                ShowDeath(journey.Profile.deathText);
                break;
            case JourneyState.Paused:
                ShowPause();
                break;
            case JourneyState.Ending:
                ShowEnding();
                break;
            default:
                ShowMenu();
                break;
        }
    }

    void Panel(string title, string body, float bodyHeight)
    {
        ClearHeldInput();
        EnsureEventSystem();
        hud.gameObject.SetActive(false);
        modal.gameObject.SetActive(true);
        ClearChildren(content);
        Label(content, title, 34, 56);
        Label(content, body, 23, bodyHeight);
    }

    void Button(string text, Action action)
    {
        AddButton(content, text, action, 46);
    }

    UnityEngine.UI.Button AddButton(Transform parent, string text, Action action, float height)
    {
        var root = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<LayoutElement>().preferredHeight = height;
        root.GetComponent<Image>().color = new Color(0.24f, 0.25f, 0.25f, 0.96f);
        var button = root.GetComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(() => action());
        var label = FullRect("Label", root.GetComponent<RectTransform>()).gameObject.AddComponent<Text>();
        ConfigureText(label, text, 22);
        label.alignment = TextAnchor.MiddleCenter;
        if (parent == content && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(root);
        return button;
    }

    void AddHold(Transform parent, string text, Action<bool> action)
    {
        var button = AddButton(parent, text, () =>
        {
        }, 50);
        button.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };
        button.gameObject.AddComponent<JourneyHoldButton>().onHeld = action;
    }

    void AddVolume()
    {
        Label(content, "声音", 20, 26);
        var root = new GameObject("Volume", typeof(RectTransform), typeof(Image), typeof(Slider), typeof(LayoutElement));
        root.transform.SetParent(content, false);
        root.GetComponent<LayoutElement>().preferredHeight = 30;
        root.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1);
        var handle = Rect("Handle", root.GetComponent<RectTransform>(), new Vector2(0, 0.5f), Vector2.zero, new Vector2(22, 30));
        handle.gameObject.AddComponent<Image>().color = paper;
        var slider = root.GetComponent<Slider>();
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = PlayerPrefs.GetFloat("runner.journey.volume", 0.8f);
        slider.onValueChanged.AddListener(value =>
        {
            PlayerPrefs.SetFloat("runner.journey.volume", value);
            AudioListener.volume = value;
            if (Camera.main != null)
                Camera.main.GetComponent<JourneyPresentation>()?.SetMasterVolume(value);
        });
    }

    Text Label(Transform parent, string value, int size, float height)
    {
        var root = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<LayoutElement>().preferredHeight = height;
        var label = root.GetComponent<Text>();
        ConfigureText(label, value, size);
        label.alignment = TextAnchor.MiddleCenter;
        return label;
    }

    Text FixedText(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, int fontSize)
    {
        var label = Rect(name, parent, anchor, offset, size).gameObject.AddComponent<Text>();
        ConfigureText(label, "", fontSize);
        return label;
    }

    UnityEngine.UI.Button FixedButton(RectTransform parent, string text, Vector2 anchor, Vector2 offset, Vector2 size, Action action)
    {
        var button = AddButton(parent, text, action, size.y);
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        return button;
    }

    void ConfigureText(Text label, string value, int size)
    {
        label.font = font;
        label.text = value;
        label.fontSize = size;
        label.color = paper;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = false;
    }

    static RectTransform FullRect(string name, RectTransform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    static RectTransform Rect(string name, RectTransform parent, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
        return rect;
    }

    static void ClearChildren(Transform parent)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
            new GameObject("Journey EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    void Quit()
    {
        journey.Persist();
        Application.Quit();
    }
}
