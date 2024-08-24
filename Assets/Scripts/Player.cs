using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private GameObject _spawnerUnderMouse;
    private List<GameObject> _selectedSpawners = new List<GameObject>();

    private NetworkVariable<Team> _team = new NetworkVariable<Team>(Team.Neutral);

    public Team Team {
        get => _team.Value;
        set => _team.Value = value; 
    }

    public void SelectSpawner(GameObject gameObject) {
        _spawnerUnderMouse = gameObject;
    }

    public void UnselectSpawner() {
        if (Input.GetMouseButton(0)) {
            if (_spawnerUnderMouse.GetComponent<Spawner>().GetTeam() == _team.Value) {
                AddToSelectedSpawners();
            }
        }
        _spawnerUnderMouse = null;
    }

    private void AddToSelectedSpawners() {
        _selectedSpawners.Add(_spawnerUnderMouse);
        _spawnerUnderMouse.GetComponent<Spawner>().EnableLineRenderer();
    }

    private void Start()
    {
        if (IsOwner) Debug.Log("Player created");
    }

    private void Update() {
        if (!IsOwner) return;
        if (Input.GetMouseButtonUp(0)) {
            ProcessSelectedSpawners();
        }
    }

    private void ProcessSelectedSpawners() {
        if (_spawnerUnderMouse != null) {
            foreach (GameObject spawner in _selectedSpawners) {
                SendSquadServerRpc(spawner.GetComponent<NetworkObject>(), _spawnerUnderMouse.GetComponent<NetworkObject>());
            }
        }
        ClearSelectedSpawners();
    }

    [ServerRpc]
    private void SendSquadServerRpc(NetworkObjectReference startSpawner, NetworkObjectReference targetSpawner) {
        if (startSpawner.TryGet(out NetworkObject spawnerObject)) {
            spawnerObject.gameObject.GetComponent<Spawner>().SendSquadServerRpc(targetSpawner);
        }
    }

    private void ClearSelectedSpawners() {
        foreach (GameObject spawner in _selectedSpawners) {
            spawner.GetComponent<Spawner>().DisableLineRenderer();
        }
        _selectedSpawners.Clear();
    }
}
