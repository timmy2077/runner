using UnityEngine;

// Scene-owned. No encounter object is ever parented to a streamed level segment.
public class JourneyEncounterDirector : MonoBehaviour
{
    public JourneyEncounterState State { get; private set; }
    public JourneyDemonDefinition Definition { get; private set; }
    public JourneyBossController Actor { get; private set; }
    public bool IsResolved => State == JourneyEncounterState.Resolved;
    public bool IsPresent => State == JourneyEncounterState.Approaching || State == JourneyEncounterState.Active;
    public float MovementLimit => Definition != null && Definition.IsCombat && !IsResolved && journey.Course != null ? journey.Course.XAtProgress(Definition.arenaEndProgress) - 2 : float.PositiveInfinity;

    LifeJourneySession journey;
    public void Initialize(LifeJourneySession session)
    {
        journey = session;
        Definition = session.Settings.demonSettings.Find(session.Profile.demon);
        State = session.Profile.demon == MindDemon.None || (session.Save.demonMask & (1 << session.StageIndex)) != 0 ? JourneyEncounterState.Resolved : JourneyEncounterState.Dormant;
        if (session.Profile.demon != MindDemon.None && (Definition == null || !Definition.IsValid))
        {
            Debug.LogError("Missing or invalid mind demon definition: " + session.Profile.demon);
            enabled = false;
        }
    }

    void Update()
    {
        if (journey == null || !journey.IsPlaying || journey.Course == null || Definition == null || IsResolved)
            return;
        if (State == JourneyEncounterState.Dormant && journey.Course.Progress >= Definition.startProgress)
        {
            var actor = new GameObject("Mind Demon - " + Definition.title);
            actor.transform.SetParent(transform, false);
            Actor = actor.AddComponent<JourneyBossController>();
            State = JourneyEncounterState.Approaching;
            Actor.Initialize(journey, this, Definition);
            journey.UI.Notify("心魔 · " + Definition.title + "\n" + Definition.introduction, 6);
        }

        if (State == JourneyEncounterState.Approaching && Actor != null && Actor.Phase != JourneyDemonPhase.Arriving)
            State = JourneyEncounterState.Active;
        if (!Definition.IsCombat && State == JourneyEncounterState.Active && journey.Course.Progress >= Definition.arenaEndProgress)
            Resolve();
    }

    public void Resolve()
    {
        if (IsResolved || journey == null || !journey.IsPlaying || Actor == null)
            return;
        if (Definition.IsCombat && Actor.Health > 0)
            return;
        if (!Definition.IsCombat && (journey.Course == null || journey.Course.Progress < Definition.arenaEndProgress || State != JourneyEncounterState.Active))
            return;
        State = JourneyEncounterState.Resolved;
        Actor.Resolve();
        journey.RecordEncounterResolution(Definition.resolution);
    }

    public void CancelAttacks()
    {
        if (Actor != null)
            Actor.CancelAttacks();
    }
}
