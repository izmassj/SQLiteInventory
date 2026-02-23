using UnityEngine;

/// <summary>
/// Mantiene el usuario logueado entre escenas.
/// </summary>
public sealed class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    public int UserId { get; private set; }
    public string Username { get; private set; }
    public bool IsLoggedIn => UserId > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetUser(int userId, string username)
    {
        UserId = userId;
        Username = username;
        Debug.Log($"[Session] Logged as {Username} (id {UserId})");
    }

    public void Logout()
    {
        Debug.Log("[Session] Logout");
        UserId = 0;
        Username = null;
    }
}
