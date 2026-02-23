using UnityEngine;

public class FPSCap : MonoBehaviour
{
    [SerializeField] private int frameRate;

    void Start()
    {
        Application.targetFrameRate = frameRate;
    }
}
