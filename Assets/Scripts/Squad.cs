using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Squad : NetworkBehaviour
{
    [SerializeField] private GameObject _counterText;
    [SerializeField] private GameObject _sprite;

    private GameObject _targetSpawner;
    private Team _team;
    private float _speed = 1f;

    public int Counter { get; private set; }

    private void Update() {
        if (!IsServer) return;
        MoveToTarget();
    }

    [ClientRpc]
    private void SetCounterTextClientRpc(int counter) {
        _counterText.GetComponent<TextMeshPro>().text = counter.ToString();
    }

    public void SetCounter(int counter) {
        Counter = counter;
        SetCounterTextClientRpc(counter);
    }

    public void SetTarget(GameObject target) {
        _targetSpawner = target;
    }

    private void MoveToTarget() {
        Vector3 vector = _targetSpawner.transform.position - transform.position;
        if (vector.magnitude < 0.05f) {
            _targetSpawner.GetComponent<Spawner>().ReceiveSquad(gameObject);
        } else {
            Vector3 direction = vector.normalized;
            transform.position = transform.position + direction * Time.deltaTime * _speed;
        }
    }

    [ClientRpc]
    public void SetColorClientRpc(Color color) {
        _sprite.GetComponent<SpriteRenderer>().color = color;
    }

    public void SetTeam(Team team) {
        _team = team;
    } 
    
    public Team GetTeam() {
        return _team;
    }
}
