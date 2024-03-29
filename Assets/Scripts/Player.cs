using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Player : NetworkBehaviour
{
    private GameObject _spawnerUnderMouse;
    private List<GameObject> _focusedSpawners = new List<GameObject>();

    private NetworkVariable<Team> _team = new NetworkVariable<Team>(Team.Neutral);

    private void Start()
    {
        if (IsOwner) Debug.Log("Player created");
    }

    private void Update() {
        if (!IsOwner) return;

        if (Input.GetMouseButtonUp(0)) {
            if (_spawnerUnderMouse != null) {
                foreach (GameObject spawner in _focusedSpawners) {
                    SendSquadServerRpc(spawner.GetComponent<NetworkObject>(), _spawnerUnderMouse.GetComponent<NetworkObject>());
                }
            }

            foreach (GameObject spawner in _focusedSpawners) {
                spawner.GetComponent<Spawner>().DisableLineRenderer();
            }
            _focusedSpawners.Clear();
        }
    }

    public void FocusSpawner(GameObject gameObject) {
        _spawnerUnderMouse = gameObject;
    }

    public void UnfocusSpawner() {
        if (Input.GetMouseButton(0)) {
            if (_spawnerUnderMouse.GetComponent<Spawner>().GetTeam() == _team.Value) {
                _focusedSpawners.Add(_spawnerUnderMouse);
                _spawnerUnderMouse.GetComponent<Spawner>().EnableLineRenderer();
            }
        }
        _spawnerUnderMouse = null;
    }

    public void SetTeam(Team team) {
        _team.Value = team;
    }
    
    public Team GetTeam() {
        return _team.Value;
    }

    [ServerRpc]
    private void SendSquadServerRpc(NetworkObjectReference startSpawner, NetworkObjectReference targetSpawner) {
        if (startSpawner.TryGet(out NetworkObject spawnerObject)) {
            spawnerObject.gameObject.GetComponent<Spawner>().SendSquadServerRpc(targetSpawner);
        }
    }
}
