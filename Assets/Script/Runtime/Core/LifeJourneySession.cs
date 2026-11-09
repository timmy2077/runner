using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class LifeJourneySession : MonoBehaviour
{
    public static LifeJourneySession Instance { get; private set; }
    public LifeJourneySettings Settings { get; private set; }
    public JourneySaveData Save { get; private set; }
    public JourneyState State { get; private set; } = JourneyState.Menu;
    public LifeStageProfile Profile { get; private set; }
    public int StageIndex { get; private set; } = -1;
    public float Progress { get; private set; }
    public bool IsStageScene => StageIndex >= 0 && Profile != null;
    public bool IsPlaying => IsStageScene && State == JourneyState.Playing;
    public bool EncounterActive => IsPlaying && Encounter != null && Encounter.IsPresent;
    public float DurationScale => Save != null && Save.shortJourney ? Settings.shortJourneyScale : 1;
    public JourneyUI UI { get; private set; }
    public LifeEventDirector Events { get; private set; }
    public LifeCourseGenerator Course { get; private set; }
    public JourneyEncounterDirector Encounter { get; private set; }
    public JourneyBossController Boss => Encounter != null ? Encounter.Actor : null;
    public bool EncounterResolved => !IsStageScene || Profile.demon == MindDemon.None || (Save != null && (Save.demonMask & (1 << StageIndex)) != 0);
    // Compatibility entry point for the original player and validation code.
    public bool BossDefeated => EncounterResolved;

    float saveTimer;
    float deathTimer;
    bool resuming;
    Coroutine resumeCountdown;
    JourneyPresentation presentation;
    AsyncOperation loading;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("Life Journey").AddComponent<LifeJourneySession>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Settings = LifeJourneySettings.Load();
        Save = JourneySave.Load();
        UI = gameObject.AddComponent<JourneyUI>();
        Events = gameObject.AddComponent<LifeEventDirector>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1;
        }
    }

    void Start()
    {
        // Entering Play Mode can restore the initial scene without a Single sceneLoaded event.
        // Initialize that scene before the player and route Start methods run.
        if (Settings != null && State == JourneyState.Menu && !UI.IsShowingDialog)
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || Settings == null)
            return;
        StageIndex = Settings.FindStage(scene.name);
        Events.ClearRuntime();
        Course = null;
        Encounter = null;
        Progress = 0;
        Profile = IsValidStage(StageIndex) ? Settings.stages[StageIndex] : null;
        if (Profile == null)
        {
            State = JourneyState.Menu;
            Time.timeScale = 1;
            UI.ShowMenu();
            return;
        }

        // Opening a stage directly in the editor starts a fresh session for that stage.
        if (Save == null || Save.stageIndex != StageIndex || Save.finished)
            Save = new JourneySaveData
            {
                stageIndex = StageIndex,
                completedStages = (1 << StageIndex) - 1,
                routeSeed = NewRouteSeed()
            };
        Save.checkpointSegment = Mathf.Min(Save.checkpointSegment, JourneyRoutePlan.SegmentCount(Profile, DurationScale) - 2);
        SetState(JourneyState.Introduction);
        presentation = Camera.main != null ? Camera.main.gameObject.AddComponent<JourneyPresentation>() : null;
        HideLegacyInterface(scene);
        JourneyLegacyIsolation.Apply(scene);
        var encounterRoot = new GameObject("Journey Encounters");
        SceneManager.MoveGameObjectToScene(encounterRoot, scene);
        Encounter = encounterRoot.AddComponent<JourneyEncounterDirector>();
        Encounter.Initialize(this);
        UI.ShowIntroduction(Profile);
        Persist();
    }

    static void HideLegacyInterface(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                canvas.enabled = false;
            foreach (var ui in root.GetComponentsInChildren<PuaseUI>(true))
                ui.enabled = false;
            foreach (var healthBar in root.GetComponentsInChildren<HealthBar>(true))
                healthBar.enabled = false;
        }
    }

    bool IsValidStage(int stage) => Settings != null && stage >= 0 && stage < Settings.stages.Length;
    void Update()
    {
        if (Settings == null)
            return;
        if (IsStageScene && Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
        if (IsPlaying)
        {
            Save.elapsedSeconds += Time.deltaTime;
            saveTimer += Time.deltaTime;
            if (Course != null)
            {
                Progress = Course.Progress;
                if (Progress >= 1)
                    CompleteStage();
            }

            if (saveTimer >= 15)
                Persist();
        }

        if (State == JourneyState.Dying)
        {
            deathTimer += Time.unscaledDeltaTime;
            if (presentation != null)
                presentation.SetDeath(Mathf.Clamp01(deathTimer / Settings.deathFadeSeconds));
            if (deathTimer >= Settings.deathFadeSeconds)
            {
                SetState(JourneyState.Dead);
                UI.ShowDeath(Profile.deathText);
            }
        }
    }

    public static string DemonTitle(MindDemon demon)
    {
        switch (demon)
        {
            case MindDemon.Fear:
                return "恐惧";
            case MindDemon.Anxiety:
                return "焦虑";
            case MindDemon.Confusion:
                return "迷茫";
            case MindDemon.Pressure:
                return "压力";
            case MindDemon.Death:
                return "死亡";
            default:
                return "";
        }
    }

    public void RegisterCourse(LifeCourseGenerator course)
    {
        Course = course;
        Progress = course.Progress;
    }

    void SetState(JourneyState state)
    {
        State = state;
        if (state != JourneyState.Paused)
            resuming = false;
        Time.timeScale = state == JourneyState.Playing || state == JourneyState.Menu || state == JourneyState.Loading ? 1 : 0;
        if (state != JourneyState.Playing && PlayerController.Instance != null)
            PlayerController.Instance.ClearInput();
    }

    public void BeginStage()
    {
        if (State != JourneyState.Introduction)
            return;
        SetState(JourneyState.Playing);
        if (Events.RestorePending()) return;
        UI.ShowHUD();
        UI.Notify(Profile.Controls, 7);
    }

    public void NewJourney(bool shortJourney)
    {
        if (loading != null)
            return;
        Save = new JourneySaveData
        {
            shortJourney = shortJourney,
            routeSeed = NewRouteSeed()
        };
        JourneySave.Write(Save);
        LoadStage(0);
    }

    public void ContinueJourney()
    {
        if (loading != null)
            return;
        Save = JourneySave.Load();
        if (Save == null)
        {
            NewJourney(false);
            return;
        }

        if (Save.finished)
        {
            SetState(JourneyState.Ending);
            UI.ShowEnding();
            return;
        }

        LoadStage(Save.stageIndex);
    }

    public void RetryCheckpoint()
    {
        if (State != JourneyState.Dead && State != JourneyState.Paused)
            return;
        Persist();
        LoadStage(StageIndex);
    }

    public void Die()
    {
        if (!IsPlaying)
            return;
        Save.deaths++;
        Encounter?.CancelAttacks();
        deathTimer = 0;
        SetState(JourneyState.Dying);
        if (PlayerController.Instance != null)
            PlayerController.Instance.FreezeBody();
        UI.HideModal();
        UI.Notify("", 0);
        Persist();
    }

    public void TogglePause()
    {
        if (State == JourneyState.Playing)
        {
            SetState(JourneyState.Paused);
            Persist();
            UI.ShowPause();
        }
        else if (State == JourneyState.Paused)
            Resume();
    }

    public void Resume()
    {
        if (State != JourneyState.Paused || resuming)
            return;
        resuming = true;
        UI.ShowResumeCountdown();
        resumeCountdown = StartCoroutine(ResumeCountdown(3));
    }

    IEnumerator ResumeCountdown(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            UI.SetCountdown(i);
            yield return new WaitForSecondsRealtime(1);
            if (State != JourneyState.Paused)
                yield break;
        }

        resumeCountdown = null;
        SetState(JourneyState.Playing);
        UI.ShowHUD();
    }

    public void SetEventState(JourneyState state)
    {
        if (state != JourneyState.EventChoice && state != JourneyState.EventResult) return;
        SetState(state);
        UI.HideForEvent();
        UI.ClearHeldInput();
    }

    public void ResumeAfterEvent(int seconds)
    {
        SetState(JourneyState.Paused);
        resuming = true;
        UI.ShowResumeCountdown();
        resumeCountdown = StartCoroutine(ResumeCountdown(Mathf.Clamp(seconds, 0, 5)));
    }

    public void PauseAfterEventPreview()
    {
        SetState(JourneyState.Paused);
        UI.ShowPause();
    }

    public void ReachCheckpoint(int segment)
    {
        if (!IsPlaying || segment <= Save.checkpointSegment)
            return;
        Save.checkpointSegment = segment;
        Persist();
        UI.Notify("这一程，已经记下。", 2);
    }

    public void Remember(int slot)
    {
        if (!IsPlaying || !Save.Remember(StageIndex, slot))
            return;
        string text = Profile.memories != null && slot < Profile.memories.Length ? Profile.memories[slot] : "记住这一刻。";
        UI.Notify(text, 5);
        Persist();
    }

    public void CompleteStage()
    {
        if (!IsPlaying || !BossDefeated || (Course != null && Course.Progress < 1))
            return;
        Save.completedStages |= 1 << StageIndex;
        if (StageIndex == Settings.stages.Length - 1)
        {
            Save.finished = true;
            SetState(JourneyState.Ending);
            Persist();
            UI.ShowEnding();
        }
        else
        {
            Save.stageIndex = StageIndex + 1;
            Save.checkpointSegment = 0;
            SetState(JourneyState.Transition);
            Persist();
            UI.ShowTransition(Profile, Settings.stages[StageIndex + 1]);
        }
    }

    public void DefeatBoss()
    {
        if (Encounter != null)
            Encounter.Resolve();
    }

    public void RecordEncounterResolution(string text)
    {
        if (!IsPlaying || EncounterResolved || Encounter == null || !Encounter.IsResolved)
            return;
        Save.demonMask |= 1 << StageIndex;
        UI.Notify(text, 5);
        Persist();
    }

    static int NewRouteSeed()
    {
        int seed = System.Guid.NewGuid().GetHashCode();
        return seed == 0 ? 1 : seed;
    }

    public void NextStage()
    {
        if (State == JourneyState.Transition)
            LoadStage(Save.stageIndex);
    }

    public void ReturnToMenu()
    {
        if (loading != null)
            return;
        Persist();
        Events.ClearRuntime();
        UI.ClearHeldInput();
        SetState(JourneyState.Loading);
        UI.ShowLoading();
        StartCoroutine(LoadScene("GameStart"));
    }

    void LoadStage(int stage)
    {
        if (!IsValidStage(stage) || loading != null)
            return;
        Save.stageIndex = stage;
        UI.ClearHeldInput();
        SetState(JourneyState.Loading);
        UI.ShowLoading();
        StartCoroutine(LoadScene(Settings.stages[stage].sceneName));
    }

    IEnumerator LoadScene(string scene)
    {
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Campaign scene is missing from Build Settings: " + scene);
            SetState(JourneyState.Menu);
            UI.ShowMenu();
            UI.Notify("无法载入这一程，请检查场景配置。", 8);
            yield break;
        }

        loading = SceneManager.LoadSceneAsync(scene);
        while (!loading.isDone)
            yield return null;
        loading = null;
    }

    public void Persist()
    {
        saveTimer = 0;
        JourneySave.Write(Save);
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused)
            return;
        SuspendIfNeeded();
        Persist();
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused)
            SuspendIfNeeded();
    }

    void SuspendIfNeeded()
    {
        if (IsPlaying)
            TogglePause();
        else if (State == JourneyState.Paused && resuming)
        {
            if (resumeCountdown != null)
                StopCoroutine(resumeCountdown);
            resumeCountdown = null;
            resuming = false;
            UI.ShowPause();
        }
    }

    void OnApplicationQuit()
    {
        Persist();
        Time.timeScale = 1;
    }
}
