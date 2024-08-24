using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        UpdateCountdowDisplay();
        CheckForWinner();
    }
    private void UpdateCountdowDisplay() {
        _countdown.GetComponent<TextMeshPro>().text = _countdownTime.Value.ToString();
    }

    private void CheckForWinner() {
        GameObject winner = GetWinner();
        if (winner != null) {
            Debug.Log("Winner found");
            ShowWinnerClientRpc(winner.GetComponent<Player>().Team);
        }
    }

    private void GenerateMap() {
        SpawnPlayerSpawners();
        SpawnNeutralSpawners();
    }

    private void SpawnPlayerSpawners() {
        SpawnSpawner(new Vector3(-7.5f, -3.5f, 0f), Team.TeamOne, 30); // Player 1
        SpawnSpawner(new Vector3(7.5f, 3.5f, 0f), Team.TeamTwo, 30); // Player 2
    }

    private void SpawnNeutralSpawners() {
        Vector3[] neutralPosition = {
            new Vector3(0f, 0f, 0f),
            new Vector3(-4.5f, -3.5f, 0f),
            new Vector3(4.5f, 3.5f, 0f),
            new Vector3(-6f, 0f, 0f),
            new Vector3(6f, 0f, 0f),
            new Vector3(-3f, 0f, 0f),
            new Vector3(3f, 0f, 0f),
            new Vector3(-7.5f, 3.5f, 0f),
            new Vector3(7.5f, -3.5f, 0f),
            new Vector3(-4.5f, 3.5f, 0f),
            new Vector3(4.5f, -3.5f, 0f)
        };

        foreach (var position in neutralPosition) {
            SpawnSpawner(position);
        }
    }

    private void SpawnSpawner(Vector3 position, Team team = Team.Neutral, int counter = 10)
    {
        GameObject spawnedGameObject = Instantiate(_spawner, position, Quaternion.identity);
        spawnedGameObject.GetComponent<NetworkObject>().Spawn();
        _spawners.Add(spawnedGameObject);

        var spawnerComponent = spawnedGameObject.GetComponent<Spawner>();
        spawnerComponent.SetServerObject(gameObject);
        spawnerComponent.SetTeam(team);
        spawnerComponent.UpdateColorClientRpc(team);
        spawnerComponent.SetCounter(counter);
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
        players[0].GetComponent<Player>().Team = Team.TeamOne;
        players[1].GetComponent<Player>().Team = Team.TeamTwo;
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

        return IsPlayerDominatingSpawners(player) && IsPlayerDominatingSquads(player);
    }

    private bool IsPlayerDominatingSpawners(GameObject player) {
        foreach (GameObject spawner in _spawners) {
            if (spawner.GetComponent<Spawner>().GetTeam() != player.GetComponent<Player>().Team &&
                spawner.GetComponent<Spawner>().GetTeam() != Team.Neutral) return false;
        }
        return true;
    }

    private bool IsPlayerDominatingSquads(GameObject player) {
        foreach (GameObject squad in _squads) {
            if (squad.GetComponent<Squad>().GetTeam() != player.GetComponent<Player>().Team &&
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
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(Team team) {
        ResetGame();
    }
}
