using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

public class IntroTimeSync : MonoBehaviour
{
    [Header("Cutscene related")]
    [SerializeField] private PlayableDirector _introDirector;
    [SerializeField] private VideoPlayer _introVideo;

    private bool _isSynced = false;
    private bool _hasPlayed = false;

    private void Start()
    {
        _introVideo.Stop();
    }

    private void Update()
    {
        if (_hasPlayed) return;

        if (_introDirector.time == _introDirector.duration) 
        {
            _hasPlayed = true;
        }

        if (_introDirector.time != _introVideo.time)
        {
            _introDirector.time = _introVideo.time;
            _isSynced = true;
        }

        if (_isSynced)
        {
            _introVideo.Play();
        }
    }
}
