using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Server : NetworkBehaviour
{
    [SerializeField] private GameObject _spawner;
    [SerializeField] private GameObject _village;
    [SerializeField] private GameObject _countdown;
    [SerializeField] private float _serverTickTime = 0.05f;

    private NetworkVariable<int> _countdownTime = new NetworkVariable<int>();

    private List<GameObject> _spawners = new List<GameObject>();
    private List<GameObject> _squads = new List<GameObject>();
    private List<GameObject> _players = new List<GameObject>();

    private int _tick = 0;
    private int _defaultSpawnTimerMax = 20;
    private int _defaultSpawnTimer = 0;

    public event EventHandler OnServerTick;
    public event EventHandler OnDefaultSpawn;

    private void Start()
    {
        if (!IsServer) return;
        StartCoroutine(Countdown(10));
        OnServerTick += DefaultSpawnTimer;
    }

    private void Update() {
        _countdown.GetComponent<TextMeshPro>().text = _countdownTime.Value.ToString();
        //if (GetWinner() != null) {
        //    GameObject winner = GetWinner();
        //    Debug.Log("Winner found");
        //    ShowWinnerClientRpc(winner.GetComponent<Player>().GetTeam());
        //}
    }

    private void GenerateMap()
    {


        //Спавнер первого игрока
        SpawnSpawner(new Vector3(-7.5f, -3.5f, 0f), Team.TeamOne, 30);
        //Спавнер второго игрока
        SpawnSpawner(new Vector3(7.5f, 3.5f, 0f), Team.TeamTwo, 30);

        SpawnSpawner(new Vector3(0f, 0f, 0f));
        SpawnSpawner(new Vector3(-4.5f, -3.5f, 0f));
        SpawnSpawner(new Vector3(4.5f, 3.5f, 0f));
        SpawnSpawner(new Vector3(-6f, 0f, 0f));
        SpawnSpawner(new Vector3(6f, 0f, 0f));
        SpawnSpawner(new Vector3(-3f, 0f, 0f));
        SpawnSpawner(new Vector3(3f, 0f, 0f));
        SpawnSpawner(new Vector3(-7.5f, 3.5f, 0f));
        SpawnSpawner(new Vector3(7.5f, -3.5f, 0f));
        SpawnSpawner(new Vector3(-4.5f, 3.5f, 0f));
        SpawnSpawner(new Vector3(4.5f, -3.5f, 0f));
    }

    private void SpawnSpawner(Vector3 position, Team team = Team.Neutral, int counter = 10)
    {
        GameObject spawnedGameObject = Instantiate(_spawner, position, Quaternion.identity);
        spawnedGameObject.GetComponent<NetworkObject>().Spawn();
        _spawners.Add(spawnedGameObject);
        spawnedGameObject.GetComponent<Spawner>().SetServerObject(gameObject);
        spawnedGameObject.GetComponent<Spawner>().SetTeam(team);
        spawnedGameObject.GetComponent<Spawner>().UpdateColorClientRpc(team);
        spawnedGameObject.GetComponent<Spawner>().SetCounter(counter);
    }

    private IEnumerator Countdown(int timeInSeconds) {
        var delay = new WaitForSecondsRealtime(1);
        while (timeInSeconds > 0) {
            timeInSeconds--;
            _countdownTime.Value = timeInSeconds;
            Debug.Log(timeInSeconds);
            yield return delay;
        }
        AssignTeams();
        GenerateMap();
        HideCountdownTimerClientRpc();
        StartCoroutine(ServerTick());
    }

    [ClientRpc]
    private void HideCountdownTimerClientRpc() {
        _countdown.SetActive(false);
    }

    private IEnumerator ServerTick() {
        var delay = new WaitForSecondsRealtime(_serverTickTime);
        while (true) {
            _tick++;
            OnServerTick?.Invoke(this, EventArgs.Empty);
            yield return delay;
        }
    }

    private void DefaultSpawnTimer(object sender, EventArgs e) {
        _defaultSpawnTimer++;
        if (_defaultSpawnTimer == _defaultSpawnTimerMax - 1) {
            OnDefaultSpawn?.Invoke(this, EventArgs.Empty);
            _defaultSpawnTimer = 0;
        }
    }

    private void AssignTeams() {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        _players.Add(players[0]);
        _players.Add(players[1]);
        players[0].GetComponent<Player>().SetTeam(Team.TeamOne);
        players[1].GetComponent<Player>().SetTeam(Team.TeamTwo);
    }

    private GameObject GetWinner() {
        foreach (var player in _players) {
            if (IsPlayerWon(player)) {
                return player;
            }
        }
        return null;
    }

    private bool IsPlayerWon(GameObject player) {

        foreach (GameObject spawner in _spawners) {
            if (spawner.GetComponent<Spawner>().GetTeam() != player.GetComponent<Player>().GetTeam() &&
                spawner.GetComponent<Spawner>().GetTeam() != Team.Neutral) return false;
        }

        foreach (GameObject squad in _squads) {
            if (squad.GetComponent<Squad>().GetTeam() != player.GetComponent<Player>().GetTeam() &&
                squad.GetComponent<Squad>().GetTeam() != Team.Neutral) return false;
        }

        return true;
    }

    public void AddSquadToList(GameObject squad) {
        _squads.Add(squad);
    }

    public void RemoveSquadFromList(GameObject squad) {
        _squads.Remove(squad);
    }

    private void ResetGame() {

    }

    [ClientRpc]
    private void ShowWinnerClientRpc(Team team) {

    }
}
