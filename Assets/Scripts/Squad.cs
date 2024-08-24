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

    private TextMeshPro _textMeshPro;
    private SpriteRenderer _spriteRenderer;

    public int Counter { get; private set; }

    public Team GetTeam() {
        return _team;
    }

    [ClientRpc]
    public void SetColorClientRpc(Color color) {
        _spriteRenderer.color = color;
    }

    public void SetTeam(Team team) {
        _team = team;
    }

    public void SetCounter(int counter) {
        Counter = counter;
        SetCounterTextClientRpc(counter);
    }

    public void SetTarget(GameObject target) {
        _targetSpawner = target;
    }

    private void Awake() {
        _textMeshPro = _counterText.GetComponent<TextMeshPro>();
        _spriteRenderer = _sprite.GetComponent<SpriteRenderer>();
    }

    private void Update() {
        if (!IsServer) return;
        MoveToTarget();
    }

    [ClientRpc]
    private void SetCounterTextClientRpc(int counter) {
        _counterText.GetComponent<TextMeshPro>().text = counter.ToString();
    }

    private void MoveToTarget() {
        if (_targetSpawner == null) return;

        Vector3 direction = _targetSpawner.transform.position - transform.position;
        if (direction.magnitude < 0.05f) {
            _targetSpawner.GetComponent<Spawner>().ReceiveSquad(gameObject);
        } else {
            transform.position += direction.normalized * Time.deltaTime * _speed;
        }
    }
}
