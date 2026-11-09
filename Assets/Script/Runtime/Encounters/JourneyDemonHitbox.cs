using UnityEngine;

public class JourneyDemonHitbox : MonoBehaviour
{
    JourneyDemonAttack owner;
    public void Initialize(JourneyDemonAttack attack)
    {
        owner = attack;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null)
            owner.Touch(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (owner != null)
            owner.Touch(other);
    }
}
