using System.Collections.Generic;
using UnityEngine;

// Actor lifetime and battle phases only. Pattern geometry and attack timing live separately.
public class JourneyBossController : MonoBehaviour
{
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public JourneyDemonPhase Phase { get; private set; }
    public JourneyDemonDefinition Definition { get; private set; }
    public bool IsAlive => Definition != null && Definition.IsCombat && Health > 0;
    public bool CanTakeHit => IsAlive && Phase != JourneyDemonPhase.Arriving && Phase != JourneyDemonPhase.Departing;
    public bool SecondPhase => IsAlive && Health <= MaxHealth / 2;
    public int ActiveAttackCount => attacks.Count;
    public string Status => Definition != null && !Definition.IsCombat ? "与它同行" : Phase == JourneyDemonPhase.Arriving ? "正在靠近" : Phase == JourneyDemonPhase.Telegraph ? "蓄势 · 留意预警" : Phase == JourneyDemonPhase.Attacking ? "出招 · 等待间隙" : "喘息 · J：攻击";

    LifeJourneySession journey;
    JourneyEncounterDirector director;
    SpriteRenderer visual;
    float phaseTime, flashUntil;
    int sequence;
    readonly List<JourneyDemonAttack> attacks = new List<JourneyDemonAttack>();
    public void Initialize(LifeJourneySession session, JourneyEncounterDirector owner, JourneyDemonDefinition definition)
    {
        journey = session;
        director = owner;
        Definition = definition;
        Health = MaxHealth = definition.IsCombat ? Mathf.Max(1, session.Profile.bossHealth) : 0;
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = definition.sprite != null ? definition.sprite : session.Settings.bossSprite;
        visual.sortingOrder = 9;
        if (visual.sprite != null)
            transform.localScale = Vector3.one * (2 / Mathf.Max(visual.sprite.bounds.size.y, 0.01f));
        Enter(JourneyDemonPhase.Arriving);
        FollowPlayer();
    }

    void Update()
    {
        if (journey == null || !journey.IsPlaying)
            return;
        phaseTime += Time.deltaTime;
        FollowPlayer();
        var tint = Definition.tint;
        tint.a = Phase == JourneyDemonPhase.Arriving ? Mathf.Clamp01(phaseTime / Definition.arrivalSeconds) : Phase == JourneyDemonPhase.Departing && Definition.IsCombat ? 1 - Mathf.Clamp01(phaseTime) : 0.85f;
        if (visual != null)
            visual.color = Time.time < flashUntil ? Color.white : tint;
        if (Phase == JourneyDemonPhase.Departing)
        {
            if (Definition.IsCombat && phaseTime >= 1)
                Destroy(gameObject);
            return; // Death remains a harmless companion until the scene ends.
        }

        if (Phase == JourneyDemonPhase.Arriving && phaseTime >= Definition.arrivalSeconds)
            Enter(JourneyDemonPhase.Recovering);
        if (!Definition.IsCombat)
            return;
        if (Phase == JourneyDemonPhase.Recovering && phaseTime >= (SecondPhase ? Definition.secondPhaseRecovery : Definition.recoverySeconds))
        {
            PrepareAttack();
            Enter(JourneyDemonPhase.Telegraph);
        }
        else if (Phase == JourneyDemonPhase.Telegraph && phaseTime >= Definition.warningSeconds)
            Enter(JourneyDemonPhase.Attacking);
        else if (Phase == JourneyDemonPhase.Attacking && phaseTime >= Definition.activeSeconds)
        {
            CancelAttacks();
            Enter(JourneyDemonPhase.Recovering);
        }

        attacks.RemoveAll(item => item == null);
    }

    void FollowPlayer()
    {
        var player = PlayerController.Instance;
        if (player == null || journey.Course == null)
            return;
        float ahead = Camera.main != null ? Mathf.Clamp(Camera.main.orthographicSize * Camera.main.aspect * 0.6f, 3, 7) : 6;
        if (!Definition.IsCombat)
            ahead = Mathf.Lerp(ahead, 0.9f, Mathf.InverseLerp(Definition.startProgress, Definition.arenaEndProgress, journey.Course.Progress));
        transform.position = new Vector3(player.transform.position.x + ahead, journey.Course.GroundTop + (Definition.IsCombat ? 2.1f : 1.1f), 0);
    }

    void PrepareAttack()
    {
        CancelAttacks();
        var player = PlayerController.Instance;
        if (player == null || journey.Course == null)
            return;
        var strikes = JourneyDemonPatterns.Build(Definition.demon, sequence++, SecondPhase, player.transform.position.x, journey.Profile.moveSpeed, director.MovementLimit);
        foreach (var strike in strikes)
        {
            var root = new GameObject("Telegraphed " + Definition.title);
            // Parent to director, not the scaled/following actor or a pooled chunk.
            root.transform.SetParent(director.transform, false);
            var attack = root.AddComponent<JourneyDemonAttack>();
            attack.Initialize(journey, Definition, strike);
            attacks.Add(attack);
        }
    }

    void Enter(JourneyDemonPhase next)
    {
        Phase = next;
        phaseTime = 0;
    }

    public void TakeHit(int amount)
    {
        if (!CanTakeHit || amount <= 0 || journey == null || !journey.IsPlaying)
            return;
        Health = Mathf.Max(0, Health - amount);
        flashUntil = Time.time + 0.15f;
        if (Health == 0)
            director.Resolve();
    }

    public void Resolve()
    {
        CancelAttacks();
        Enter(JourneyDemonPhase.Departing);
    }

    public void CancelAttacks()
    {
        foreach (var attack in attacks)
            if (attack != null)
            {
                attack.gameObject.SetActive(false);
                Destroy(attack.gameObject);
            }

        attacks.Clear();
    }

    void OnDestroy()
    {
        CancelAttacks();
    }
}
