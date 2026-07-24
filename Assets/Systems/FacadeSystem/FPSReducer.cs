using UnityEngine;

public class FPSReducer : MonoBehaviour, ICustomStart
{
    [SerializeField] private int targetFPS = 90;

    public void CustomStart()
    {
        Application.targetFrameRate = targetFPS;
    }
}
