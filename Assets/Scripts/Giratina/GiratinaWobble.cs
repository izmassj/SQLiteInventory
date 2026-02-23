using UnityEngine;
using DG.Tweening;

public class GiratinaWobble : MonoBehaviour
{
    [Header("Start & Finish Y Position")]
    [SerializeField] float startY;
    [SerializeField] float endY;

    private Tween moveYTween;

    private void Start()
    {
        transform.position = new Vector3(
            transform.position.x,
            startY,
            transform.position.z
        );

        moveYTween = transform.DOMoveY(endY, 1f, false).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo); 
    }
}
