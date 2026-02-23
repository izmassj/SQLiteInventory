using System;
using System.IO;
using Mono.Data.Sqlite;
using UnityEngine;

public sealed class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    [Header("DB")]
    [SerializeField] private string _databaseFileName = "rpg.sqlite";

    private SqliteConnection _connection;
    private SqliteDatabase _db;

    public string DbPath => Path.Combine(Application.persistentDataPath, _databaseFileName);
    public bool IsInitialized { get; private set; }

    public SqliteDatabase Db
    {
        get
        {
            if (!IsInitialized)
                throw new InvalidOperationException("DatabaseManager no está inicializado.");
            return _db;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        Directory.CreateDirectory(Application.persistentDataPath);

        string connString = "URI=file:" + DbPath;
        _connection = new SqliteConnection(connString);
        _connection.Open();

        _db = new SqliteDatabase(_connection);

        SchemaMigrator.ApplyMigrations(_db);
        SeedData.EnsureSeed(_db);

        IsInitialized = true;
    }

    private void OnApplicationQuit()
    {
        try 
        { 
            _connection?.Close(); 
        } 
        catch 
        { 
            /* ignorar cualquier excepcion */ 
        }

        try 
        { 
            _connection?.Dispose(); 
        } 
        catch 
        {
            /* ignorar cualquier excepcion */
        }

        _connection = null;
        _db = null;
        IsInitialized = false;
    }
}
