using System;
using UnityEngine;

[Serializable]
public class PlayerData
{
    [Header("Identity")]
    [SerializeField] private int _playerId;
    [SerializeField] private string _playerName;

    [Header("Progress")]
    [SerializeField] private int _money;
    [SerializeField] private int _score;
    [SerializeField] private int _pokedexOwned;
    [SerializeField] private int _pokedexSeen;

    [Header("Time")]
    [SerializeField] private float _playTimeSeconds;
    [SerializeField] private DateTime _adventureStartDate;

    public PlayerData(int id, string name)
    {
        _playerId = id;
        _playerName = name;
        _money = 0;
        _score = 0;
        _pokedexOwned = 0;
        _pokedexSeen = 0;
        _playTimeSeconds = 0f;
        _adventureStartDate = DateTime.Now;
    }

    public void AddMoney(int amount)
    {
        _money = Mathf.Max(0, _money + amount);
    }

    public bool SpendMoney(int amount)
    {
        if (_money < amount)
            return false;

        _money -= amount;
        return true;
    }

    public void AddScore(int amount)
    {
        _score = Mathf.Max(0, _score + amount);
    }

    public void RegisterSeen()
    {
        _pokedexSeen++;
    }

    public void RegisterOwned()
    {
        _pokedexOwned++;
    }

    public void UpdatePlayTime(float deltaTime)
    {
        _playTimeSeconds += deltaTime;
    }

    public string GetFormattedPlayTime()
    {
        TimeSpan time = TimeSpan.FromSeconds(_playTimeSeconds);
        return $"{time.Hours:D2}:{time.Minutes:D2}";
    }

    public string GetFormattedStartDate()
    {
        return _adventureStartDate.ToString("dd/MM/yyyy");
    }
}