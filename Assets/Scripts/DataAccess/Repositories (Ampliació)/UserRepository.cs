using System;
using System.Linq;

public sealed class UserRepository
{
    private readonly SqliteDatabase _db;

    public UserRepository(SqliteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public bool UsernameExists(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        int count = _db.ExecuteScalar<int>("SELECT COUNT(1) FROM users WHERE username = @u;", ("@u", username.Trim()));
        return count > 0;
    }

    public (bool ok, int userId, string error) CreateUser(string username, string password)
    {
        username = (username ?? string.Empty).Trim();
        if (username.Length < 3)
            return (false, 0, "El nombre de usuario debe tener al menos 3 caracteres.");

        if (string.IsNullOrEmpty(password) || password.Length < 4)
            return (false, 0, "La contraseña debe tener al menos 4 caracteres.");

        if (UsernameExists(username))
            return (false, 0, "Ese usuario ya existe.");

        _db.ExecuteNonQuery(
            "INSERT INTO users(username, password, created_at) VALUES (@u, @p, @c);",
            ("@u", username),
            ("@p", password),
            ("@c", DateTime.UtcNow.ToString("o"))
        );

        int id = _db.ExecuteScalar<int>("SELECT id FROM users WHERE username = @u;", ("@u", username));
        return (true, id, null);
    }

    public (bool ok, int userId, string error) Login(string username, string password)
    {
        username = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, 0, "Usuario y contraseña son obligatorios.");

        var rows = _db.Query(
            "SELECT id, password FROM users WHERE username = @u;",
            r => new
            {
                Id = r.GetInt32(0),
                Password = r.GetString(1)
            },
            ("@u", username)
        );

        var row = rows.FirstOrDefault();
        if (row == null)
            return (false, 0, "Usuario o contraseña incorrectos.");

        bool ok = string.Equals(password, row.Password, StringComparison.Ordinal);
        if (!ok)
            return (false, 0, "Usuario o contraseña incorrectos.");

        return (true, row.Id, null);
    }
}
