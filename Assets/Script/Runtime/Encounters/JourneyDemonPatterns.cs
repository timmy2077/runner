using UnityEngine;

public struct JourneyDemonStrike
{
    public JourneyObstacle kind;
    public float x, width, travel;
}

// No spawning, scene lookups, or random global state here.
public static class JourneyDemonPatterns
{
    public static JourneyDemonStrike[] Build(MindDemon demon, int sequence, bool secondPhase, float playerX, float speed, float movementLimit)
    {
        float ahead = Mathf.Max(3.5f, speed * 2.5f);
        // At the arena edge, attacks stay ahead: stopping must always remain a valid response.
        bool atEdge = playerX + speed * 3 >= movementLimit;
        switch (demon)
        {
            case MindDemon.Fear:
                return new[]
                {
                    Strike(JourneyObstacle.Gate, playerX + ahead, secondPhase ? 1.2f : 0.8f)
                };
            case MindDemon.Anxiety:
                return secondPhase ? new[]
                {
                    Strike(JourneyObstacle.Falling, playerX + ahead, 0.7f),
                    Strike(JourneyObstacle.Falling, playerX + ahead + 2, 0.7f)
                }

                : new[]
                {
                    Strike(JourneyObstacle.Falling, playerX + ahead, 0.8f)
                };
            case MindDemon.Confusion:
                return new[]
                {
                    new JourneyDemonStrike
                    {
                        kind = JourneyObstacle.Moving,
                        x = playerX + ahead + 1,
                        width = 0.7f,
                        travel = secondPhase ? 1.4f : 0.8f
                    }
                };
            case MindDemon.Pressure:
                return new[]
                {
                    Strike(JourneyObstacle.Pressure, playerX + (atEdge || sequence % 2 != 0 ? ahead : 0), secondPhase ? 2.4f : 1.8f)
                };
            default:
                return new JourneyDemonStrike[0];
        }
    }

    static JourneyDemonStrike Strike(JourneyObstacle kind, float x, float width) => new JourneyDemonStrike
    {
        kind = kind,
        x = x,
        width = width
    };
}
