using System.Collections;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;

#if UNITY_EDITOR
using ParrelSync;
#endif

public class LobbyManager : MonoBehaviour {
    
    private const int MaxConnections = 2;

    [SerializeField] private GameObject _serverPrefab;
    private Lobby _connectedLobby;
    private UnityTransport _transport;
    private string _playerId;

    private void Awake() {
        _transport = FindObjectOfType<UnityTransport>();
    }

    public async void CreateOrJoinLobby() {
        await Authenticate();
        _connectedLobby = await QuickJoinLobby() ?? await CreateLobby();
    }

    private async Task Authenticate() {
        var options = new InitializationOptions();

#if UNITY_EDITOR
        options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
#endif

        await UnityServices.InitializeAsync(options);
        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _playerId = AuthenticationService.Instance.PlayerId;
    }

    private async Task<Lobby> QuickJoinLobby() {
        try {
            Lobby lobby = await Lobbies.Instance.QuickJoinLobbyAsync();
            await JoinRelayAllocation(lobby);
            return lobby;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            return null;
        }
    }

    private async Task<Lobby> CreateLobby() {
        try {
            var allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var lobbyOptions = new CreateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { "JoinCodeKey", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby Name", MaxConnections, lobbyOptions);
            StartCoroutine(HeartbeatLobbyCorutine(lobby.Id, 15));

            SetHostRelayData(allocation);
            NetworkManager.Singleton.StartHost();
            Debug.Log("Host started");

            StartCoroutine(StartGameWhenPlayersConnected());
            return lobby;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            return null;
        }
    }

    private IEnumerator HeartbeatLobbyCorutine(string lobbyId, float waitTimeSeconds) {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (NetworkManager.Singleton.ConnectedClientsList.Count != MaxConnections) {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    private async Task JoinRelayAllocation(Lobby lobby) {
        var allocation = await RelayService.Instance.JoinAllocationAsync(lobby.Data["JoinCodeKey"].Value);
        SetClientRelayData(allocation);
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client started");
    }

    private void SetClientRelayData(JoinAllocation allocation) {
        _transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData);
    }

    private void SetHostRelayData(Allocation allocation) {
        _transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);
    }

    private IEnumerator StartGameWhenPlayersConnected() {
        while (NetworkManager.Singleton.ConnectedClientsList.Count != MaxConnections) {
            Debug.Log(NetworkManager.Singleton.ConnectedClientsList.Count);
            yield return null;
        }
        SpawnServer();
    }

    private void SpawnServer() {
        GameObject server = Instantiate(_serverPrefab, Vector3.zero, Quaternion.identity);
        Debug.Log("server created");
        server.GetComponent<NetworkObject>().Spawn();
    }
}
