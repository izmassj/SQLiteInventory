using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;

/// <summary>
/// Wrapper pequeño para ejecutar SQL sin exponer comandos/conexiones al resto del juego.
/// Mantiene la capa de acceso a datos concentrada.
/// </summary>
public sealed class SqliteDatabase : IDisposable
{
    private readonly SqliteConnection _conn;

    public SqliteDatabase(SqliteConnection conn)
    {
        _conn = conn ?? throw new ArgumentNullException(nameof(conn));
    }

    public int ExecuteNonQuery(string sql, params (string name, object value)[] parameters)
    {
        using var cmd = CreateCommand(sql, parameters);
        return cmd.ExecuteNonQuery();
    }

    public T ExecuteScalar<T>(string sql, params (string name, object value)[] parameters)
    {
        using var cmd = CreateCommand(sql, parameters);
        object result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            return default;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public List<T> Query<T>(string sql, Func<IDataReader, T> map, params (string name, object value)[] parameters)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));

        using var cmd = CreateCommand(sql, parameters);
        using IDataReader reader = cmd.ExecuteReader();

        var list = new List<T>();
        while (reader.Read())
            list.Add(map(reader));

        return list;
    }

    public void WithTransaction(Action<SqliteDatabase> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        using var tx = _conn.BeginTransaction();
        try
        {
            action(this);
            tx.Commit();
        }
        catch
        {
            try 
            { 
                tx.Rollback(); 
            } 
            catch 
            { 
                /*  */ 
            }
            throw;
        }
    }

    private SqliteCommand CreateCommand(string sql, params (string name, object value)[] parameters)
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;

        if (parameters != null)
        {
            foreach (var p in parameters)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = p.name;
                param.Value = p.value ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
        }

        return (SqliteCommand)cmd;
    }

    public void Dispose()
    {
    }
}
