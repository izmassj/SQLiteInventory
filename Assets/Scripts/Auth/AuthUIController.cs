using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField _username;
    [SerializeField] private TMP_InputField _password;
    [SerializeField] private TMP_Text _feedback;
    [SerializeField] private TMP_Text _usernameText;
    [SerializeField] private TMP_Text _passwordText;

    [Header("Scenes")]
    [SerializeField] private string _gameSceneName;

    private UserRepository _users;

    private void Awake()
    {
        _users = new UserRepository(DatabaseManager.Instance.Db);
        SetFeedback(string.Empty);
    }

    private void Start()
    {
        _usernameText.text = "demo";
        _password.text = "demo123";
    }

    public void OnLoginClicked()
    {
        string u = _username != null ? _username.text : string.Empty;
        string p = _password != null ? _password.text : string.Empty;

        var result = _users.Login(u, p);
        if (!result.ok)
        {
            SetFeedback(result.error);
            return;
        }

        SessionManager.Instance.SetUser(result.userId, u.Trim());
        SceneManager.LoadScene(_gameSceneName);
    }

    public void OnRegisterClicked()
    {
        string u = _username != null ? _username.text : string.Empty;
        string p = _password != null ? _password.text : string.Empty;

        var result = _users.CreateUser(u, p);
        if (!result.ok)
        {
            SetFeedback(result.error);
            return;
        }

        SessionManager.Instance.SetUser(result.userId, u.Trim());
        SceneManager.LoadScene(_gameSceneName);
    }

    private void SetFeedback(string msg)
    {
        if (_feedback != null)
            _feedback.text = msg ?? string.Empty;
    }
}
