using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    private const int _maxCounter = 50;

    [SerializeField] private GameObject _squadPrefab;
    [SerializeField] private GameObject _counterText;
    [SerializeField] private GameObject _sprite;

    private NetworkVariable<Team> _team = new NetworkVariable<Team>(Team.Neutral);

    private LineRenderer _lineRenderer;

    private GameObject _playerClient;
    private GameObject _serverObject;

    private int _counter = 10;

    public Team GetTeam() {
        return _team.Value;
    }

    public void SetServerObject(GameObject server) {
        _serverObject = server;
    }
    public void SetTeam(Team team) {
        _team.Value = team;
    }

    public void SetCounter(int counter) {
        _counter = counter;
        SetCounterTextClientRpc(_counter);
    }

    public void EnableLineRenderer() => _lineRenderer.enabled = true;

    public void DisableLineRenderer() => _lineRenderer.enabled = false;

    [ClientRpc]
    public void UpdateColorClientRpc(Team team) {
        Debug.Log("color");

        _sprite.GetComponent<SpriteRenderer>().color = team switch {
            Team.Neutral => Color.white,
            Team.TeamOne => Color.green,
            Team.TeamTwo => Color.red,
            _ => _sprite.GetComponent<SpriteRenderer>().color,
        };
    }

    [ServerRpc]
    public void SendSquadServerRpc(NetworkObjectReference target) {
        if (!target.TryGet(out NetworkObject targetObject)) return;

        int squadCounter = DetermineSquadCounter();

        if (squadCounter == 0) return;

        SpawnSquad(targetObject, squadCounter);
    }

    public void ReceiveSquad(GameObject squad) {
        Squad squadComponent = squad.GetComponent<Squad>();
        Team squadTeam = squadComponent.GetTeam();
        int squadCounter = squadComponent.Counter;

        if (_team.Value == squadTeam) {
            _counter += squadCounter;
        } else {
            _counter -= squadCounter;
            if (_counter < 0) {
                _team.Value = squadTeam;
                _counter = -_counter;
                UpdateColorClientRpc(_team.Value);
            }
        }

        SetCounterTextClientRpc(_counter);
        _serverObject.GetComponent<Server>().RemoveSquadFromList(squad);
        squad.GetComponent<NetworkObject>().Despawn();
    }

    private void Awake() {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start() {
        SetLineRendererStartPosition();
        InitializePlayerClient();
        SubscribeToServerEvents();
    }

    private void Update() {
        UpdateLaneRendererPosition();
    }

    private void OnMouseEnter() {
        if (!IsClient) return;
        _playerClient.GetComponent<Player>().SelectSpawner(gameObject);
    }

    private void OnMouseExit() {
        if (!IsClient) return;
        _playerClient.GetComponent<Player>().UnselectSpawner();
    }

    private void SetLineRendererStartPosition() {
        _lineRenderer.SetPosition(0, transform.position);
    }

    private void InitializePlayerClient() {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players) {
            if (player.GetComponent<NetworkBehaviour>().IsOwner == true) {
                _playerClient = player;
            }
        }
    }

    private void SubscribeToServerEvents() {
        if (!IsServer) return;
        _serverObject.GetComponent<Server>().OnDefaultSpawn += CalculateCounter;
    }

    private void UpdateLaneRendererPosition() {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        _lineRenderer.SetPosition(1, mousePosition);
    }

    [ClientRpc]
    private void SetCounterTextClientRpc(int counter)
    {
        _counterText.GetComponent<TextMeshPro>().text = counter.ToString();
    }

    private void CalculateCounter(object sender, EventArgs e) {
        if (_team.Value == Team.Neutral) return;

        _counter = Mathf.Clamp(_counter + 1, 0, _maxCounter);
        SetCounterTextClientRpc(_counter);
    }

    private int DetermineSquadCounter() {
        return _counter switch {
            0 => 0,
            1 => 1,
            _ => _counter / 2,
        };
    }

    private void SpawnSquad(NetworkObject targetObject, int squadCounter) {
        GameObject squad = Instantiate(_squadPrefab, transform.position, Quaternion.identity);
        _serverObject.GetComponent<Server>().AddSquadToList(squad);
        squad.GetComponent<NetworkObject>().Spawn();
        squad.GetComponent<Squad>().SetCounter(squadCounter);
        _counter -= squadCounter;

        SetCounterTextClientRpc(_counter);
        Squad squadComponent = squad.GetComponent<Squad>();
        squadComponent.SetTarget(targetObject.gameObject);
        squadComponent.SetColorClientRpc(_sprite.GetComponent<SpriteRenderer>().color);
        squadComponent.SetTeam(_team.Value);
    }
}
