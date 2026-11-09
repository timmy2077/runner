using UnityEngine;

// Awake captures the original pose before the first OnEnable pool reset.
public class ResetObstaclePosition : MonoBehaviour
{
    public bool useInitialPosition = true;
    public Vector3 targetPosition;
    public Vector3 targetRotation;
    Vector3 initialPosition;
    Quaternion initialRotation;
    void Awake()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    void OnEnable()
    {
        ResetPosition();
    }

    void OnDisable()
    {
        ResetPosition();
    }

    void ResetPosition()
    {
        transform.localPosition = useInitialPosition ? initialPosition : targetPosition;
        transform.localRotation = useInitialPosition ? initialRotation : Quaternion.Euler(targetRotation);
    }
}
