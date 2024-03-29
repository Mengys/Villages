using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    [SerializeField] private GameObject _squadPrefab;
    [SerializeField] private GameObject _counterText;
    [SerializeField] private GameObject _sprite;

    private NetworkVariable<Team> _team = new NetworkVariable<Team>(Team.Neutral);

    private LineRenderer _lineRenderer;

    private GameObject _playerClient;
    private GameObject _serverObject;

    private int _maxCounter = 50;
    private int _counter = 10;

    private void Awake() {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start() {

        SetLineRendererStartPosition();

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players) {
            if (player.GetComponent<NetworkBehaviour>().IsOwner == true) {
                _playerClient = player;
            }
        }

        if (!IsServer) return;
        _serverObject.GetComponent<Server>().OnDefaultSpawn += CalculateCounter;
    }

    private void Update()
    {

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        _lineRenderer.SetPosition(1, mousePosition);

        if (!NetworkManager.Singleton.IsServer) return;
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 0) return;
    }

    [ClientRpc]
    private void SetCounterTextClientRpc(int counter)
    {
        _counterText.GetComponent<TextMeshPro>().text = counter.ToString();
    }

    public void SetServerObject(GameObject server) {
        _serverObject = server;
    }

    private void CalculateCounter(object sender, EventArgs e) {
        if (_team.Value != Team.Neutral) {
            if (_counter < _maxCounter) _counter++;
            if (_counter > _maxCounter) _counter--;
        }
        SetCounterTextClientRpc(_counter);
    }

    public void SetTeam(Team team) {
        _team.Value = team;

    }

    [ClientRpc]
    public void UpdateColorClientRpc(Team team) {
        Debug.Log("color");
        switch (team) {
            case Team.Neutral: _sprite.GetComponent<SpriteRenderer>().color = Color.white; break;
            case Team.TeamOne: _sprite.GetComponent<SpriteRenderer>().color = Color.green; break;
            case Team.TeamTwo: _sprite.GetComponent<SpriteRenderer>().color = Color.red; break;
        }
    }

    private void OnMouseEnter() {
        if (!IsClient) return;
        _playerClient.GetComponent<Player>().FocusSpawner(gameObject);
    }

    private void OnMouseExit() {
        if (!IsClient) return;
        _playerClient.GetComponent<Player>().UnfocusSpawner();
    }

    public void EnableLineRenderer() {
        _lineRenderer.enabled = true;
    }

    public void DisableLineRenderer() {
        _lineRenderer.enabled = false;
    }

    [ServerRpc]
    public void SendSquadServerRpc(NetworkObjectReference target) {
        if (target.TryGet(out NetworkObject targetObject)) {
            int squadCounter;
            switch (_counter) {
                case 0: squadCounter = 0; break;
                case 1: squadCounter = 1; break;
                default: squadCounter = _counter / 2; break;
            }
            if (squadCounter !=  0) {
                GameObject squad = Instantiate(_squadPrefab, transform.position, Quaternion.identity);
                _serverObject.GetComponent<Server>().AddSquadToList(squad);
                squad.GetComponent<NetworkObject>().Spawn();
                squad.GetComponent<Squad>().SetCounter(squadCounter);
                _counter = _counter - squadCounter;
                SetCounterTextClientRpc(_counter);
                squad.GetComponent<Squad>().SetTarget(targetObject.gameObject);
                squad.GetComponent<Squad>().SetColorClientRpc(_sprite.GetComponent<SpriteRenderer>().color);
                squad.GetComponent<Squad>().SetTeam(_team.Value);
            }
        }
    }

    public Team GetTeam() {
        return _team.Value;
    }

    public void ReceiveSquad(GameObject squad) {
        if (_team.Value == squad.GetComponent<Squad>().GetTeam()) {
            _counter += squad.GetComponent<Squad>().Counter;
        } else if (_team.Value != squad.GetComponent<Squad>().GetTeam()) {
            int counter = _counter - squad.GetComponent<Squad>().Counter;
            if (counter < 0) {
                _team.Value = squad.GetComponent<Squad>().GetTeam();
                _counter = -counter;
                UpdateColorClientRpc(_team.Value);
            } else {
                _counter = counter;
            }
        }
        SetCounterTextClientRpc(_counter);
        _serverObject.GetComponent<Server>().RemoveSquadFromList(squad);
        squad.GetComponent<NetworkObject>().Despawn();
    }

    public void SetCounter(int counter) {
        _counter = counter;
        SetCounterTextClientRpc(_counter);
    }

    private void SetLineRendererStartPosition() {
        _lineRenderer.SetPosition(0, transform.position);
    }
}
