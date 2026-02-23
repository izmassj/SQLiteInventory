using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// Adjuntar en la escena Intro: cuando el PlayableDirector termina, carga otra escena.
/// (Para tu loop: Intro -> Login)
/// </summary>
public class MenuExit : MonoBehaviour
{
    [SerializeField] private string _nextSceneName;

    public void LoadLoginRegisterScene()
    {
        if (!string.IsNullOrWhiteSpace(_nextSceneName))
            SceneManager.LoadScene(_nextSceneName);
    }
}
