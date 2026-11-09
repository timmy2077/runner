using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Separate modal canvas: gameplay remains owned by the campaign state machine.
public class JourneyEventPanel : MonoBehaviour
{
    LifeEventDirector director;
    LifeJourneySession journey;
    RectTransform canvas, page, cards;
    Font font;
    Image[] cardImages;
    Button confirm;
    Text selection;
    bool portrait;
    int openedFrame;
    bool enterReleased;

    public void Initialize(LifeEventDirector owner, LifeJourneySession session)
    {
        director = owner;
        journey = session;
        font = director.Settings != null && director.Settings.fontOverride != null ? director.Settings.fontOverride
            : Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC", "Arial" }, 24);
        var root = new GameObject("Life Event Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<RectTransform>();
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 2100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var shade = root.AddComponent<Image>();
        shade.color = new Color(0.035f, 0.04f, 0.05f, director.Settings != null ? director.Settings.backdropOpacity : 0.94f);
        page = Stretch("Event page", canvas, 24);
        var layout = page.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        Hide();
    }

    public void Show()
    {
        if (director.Pending == null) return;
        if (EventSystem.current == null)
            new GameObject("Journey EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EventSystem.current.SetSelectedGameObject(null);
        journey.UI.ClearHeldInput();
        canvas.gameObject.SetActive(true);
        foreach (Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        openedFrame = Time.frameCount;
        enterReleased = !Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter);
        portrait = Screen.height > Screen.width;
        var pending = director.Pending;
        var item = pending.snapshot;
        string stage = journey.Settings.stages[(int)item.stage].title;
        Label(page, stage + " · " + item.title + (director.IsPreview ? "  [预览 · 不写存档]" : ""), 30, 48);
        Label(page, item.prompt, 24, portrait ? 100 : 76);
        if (!pending.committed) BuildCards();
        else BuildResult();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void BuildCards()
    {
        cards = new GameObject("Three choices", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
        cards.SetParent(page, false);
        var element = cards.GetComponent<LayoutElement>();
        element.flexibleHeight = 1;
        element.minHeight = portrait ? 390 : 230;
        HorizontalOrVerticalLayoutGroup row = portrait
            ? (HorizontalOrVerticalLayoutGroup)cards.gameObject.AddComponent<VerticalLayoutGroup>()
            : cards.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 14;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = true;
        cardImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var button = MakeButton(cards, "", () => director.Select(index), 0);
            button.name = "Choice " + (i + 1);
            button.GetComponent<LayoutElement>().flexibleHeight = 1;
            button.GetComponent<LayoutElement>().flexibleWidth = 1;
            cardImages[i] = button.GetComponent<Image>();
            var option = director.Pending.snapshot.options[i];
            ScrollText(button.transform, (i + 1) + "  " + option.answer + "\n\n" + option.traitName
                + "\n\n" + option.plannedEffect + "\n\n预期能力 · 暂未启用",
                director.Settings != null ? director.Settings.cardFontSize : 24);
        }
        selection = Label(page, "请选择一张卡，再确认。", 20, 30);
        var footer = ButtonRow(page);
        confirm = MakeButton(footer, "确认这个选择 · Enter", director.Confirm, 48);
        MakeButton(footer, director.IsPreview ? "关闭预览" : "保存并返回", director.SaveAndReturn, 48);
        RefreshSelection();
    }

    void BuildResult()
    {
        var pending = director.Pending;
        var option = pending.snapshot.options[pending.selectedIndex];
        var body = new GameObject("Choice result", typeof(RectTransform), typeof(LayoutElement));
        body.transform.SetParent(page, false);
        body.GetComponent<LayoutElement>().flexibleHeight = 1;
        ScrollText(body.transform, "你选择了：" + option.answer + "\n\n"
            + (string.IsNullOrWhiteSpace(option.resultText) ? "你记下了这一次选择。" : option.resultText)
            + "\n\n经历：" + option.traitName + "\n" + option.plannedEffect
            + "\n\n" + (director.IsPreview ? "本次预览不会保存。" : "经历已记录。能力效果暂未启用。"), 25);
        var footer = ButtonRow(page);
        MakeButton(footer, director.IsPreview ? "结束预览 · Enter" : "继续前行 · Enter", director.Continue, 52);
        MakeButton(footer, director.IsPreview ? "关闭预览" : "保存并返回", director.SaveAndReturn, 52);
    }

    public void RefreshSelection()
    {
        if (director.Pending == null || director.Pending.committed || cardImages == null) return;
        for (int i = 0; i < 3; i++)
            cardImages[i].color = director.SelectedIndex == i
                ? new Color(0.33f, 0.44f, 0.48f, 1) : new Color(0.17f, 0.19f, 0.20f, 1);
        confirm.interactable = director.SelectedIndex >= 0;
        selection.text = director.SelectedIndex < 0 ? "请选择一张卡，再确认。"
            : "已选：" + director.Pending.snapshot.options[director.SelectedIndex].answer;
    }

    void Update()
    {
        if (canvas == null || !canvas.gameObject.activeSelf || director.Pending == null) return;
        if (portrait != (Screen.height > Screen.width)) { Show(); return; }
        if (Time.frameCount <= openedFrame || (director.Settings != null && !director.Settings.keyboardShortcuts)) return;
        if (!Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter)) enterReleased = true;
        if (journey.State == JourneyState.EventChoice)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) director.Select(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) director.Select(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) director.Select(2);
        }
        if (enterReleased && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            enterReleased = false;
            if (journey.State == JourneyState.EventChoice) director.Confirm();
            else if (journey.State == JourneyState.EventResult) director.Continue();
        }
    }

    public void Hide()
    {
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    Transform ButtonRow(Transform parent)
    {
        var root = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<LayoutElement>().preferredHeight = 52;
        root.GetComponent<LayoutElement>().flexibleHeight = 0;
        var row = root.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 12;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = true;
        return root.transform;
    }

    Button MakeButton(Transform parent, string caption, Action action, float height)
    {
        var root = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<LayoutElement>().preferredHeight = height;
        root.GetComponent<Image>().color = new Color(0.2f, 0.23f, 0.24f, 1);
        var button = root.GetComponent<Button>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => action());
        if (!string.IsNullOrEmpty(caption))
        {
            var text = Stretch("Caption", root.transform, 6).gameObject.AddComponent<Text>();
            Configure(text, caption, 21);
            text.alignment = TextAnchor.MiddleCenter;
        }
        return button;
    }

    Text Label(Transform parent, string caption, int size, float height)
    {
        var root = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<LayoutElement>().preferredHeight = height;
        var text = root.GetComponent<Text>();
        Configure(text, caption, size);
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }

    void ScrollText(Transform parent, string caption, int size)
    {
        var scrollRect = Stretch("Scrollable text", parent, 14);
        var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        var viewport = Stretch("Viewport", scrollRect, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        // Transparent raycast surface permits scrolling without preventing the parent card click.
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        var body = new GameObject("Body", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        body.SetParent(viewport, false);
        body.anchorMin = new Vector2(0, 1);
        body.anchorMax = Vector2.one;
        body.pivot = new Vector2(0.5f, 1);
        body.sizeDelta = Vector2.zero;
        Configure(body.GetComponent<Text>(), caption, size);
        body.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = body;
        scroll.verticalNormalizedPosition = 1;
    }

    void Configure(Text text, string caption, int size)
    {
        text.font = font;
        text.text = caption;
        text.fontSize = size;
        text.color = new Color(0.96f, 0.94f, 0.88f, 1);
        text.raycastTarget = false;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    static RectTransform Stretch(string name, Transform parent, float margin)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, margin);
        rect.offsetMax = new Vector2(-margin, -margin);
        return rect;
    }
}
