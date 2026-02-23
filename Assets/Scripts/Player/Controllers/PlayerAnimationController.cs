using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator _lucasAnimator;

    [Header("Animator Parameters")]
    [SerializeField] private string _lucasAnimatorHorDir;
    [SerializeField] private string _lucasAnimatorVerDir;
    [SerializeField] private string _lucasAnimatorRunning;

    public void SetRunning(bool running)
    {
        if (_lucasAnimator == null) return;
        _lucasAnimator.SetFloat(_lucasAnimatorRunning, running ? 1f : 0f);
    }

    public void SetDirection(Vector2 direction)
    {
        if (_lucasAnimator == null) return;
        _lucasAnimator.SetInteger(_lucasAnimatorHorDir, (int)direction.x);
        _lucasAnimator.SetInteger(_lucasAnimatorVerDir, (int)direction.y);
    }

    public void SetStepFrame(bool isLeftStep)
    {
        PlayFrame(isLeftStep ? 1 : 3);
    }

    public void SetIdleFrame(bool isLeftStep)
    {
        PlayFrame(isLeftStep ? 2 : 0);
    }

    private void PlayFrame(int frame)
    {
        if (_lucasAnimator == null) return;

        var clips = _lucasAnimator.GetCurrentAnimatorClipInfo(0);
        if (clips == null || clips.Length == 0) return;

        var clipName = clips[0].clip.name;

        clipName = clipName.Replace("Run", "Walk");

        float normalizedTime = frame / 4f;
        normalizedTime = Mathf.Min(normalizedTime, 0.9f);

        _lucasAnimator.Play(clipName, 0, normalizedTime);
    }
}
