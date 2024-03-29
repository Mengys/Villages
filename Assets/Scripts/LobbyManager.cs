using System.Collections;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using ParrelSync;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour {

    [SerializeField] GameObject _serverPrefab;

    private Lobby _connectedLobby;
    private string _playerId;
    private int _maxPlayers = 2;
    
    private UnityTransport _transport;

    private void Awake() {
        _transport = FindObjectOfType<UnityTransport>();
    }

    public async void CreateOrJoinLobby() {
        await Authenticate();

        _connectedLobby = await QuickJoinLobby() ?? await CreateLobby();
    }

    private async Task Authenticate() {

        InitializationOptions options = new InitializationOptions();
#if UNITY_EDITOR
        options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
#endif
        await UnityServices.InitializeAsync(options);

        //await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        _playerId = AuthenticationService.Instance.PlayerId;
    }

    private async Task<Lobby> QuickJoinLobby() {
        try {
            Lobby lobby = await Lobbies.Instance.QuickJoinLobbyAsync();

            var a = await RelayService.Instance.JoinAllocationAsync(lobby.Data["JoinCodeKey"].Value);
            SetTransformAsClient(a);

            NetworkManager.Singleton.StartClient();
            Debug.Log("Client started");
            return lobby;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            return null;
        }
    }

    private async Task<Lobby> CreateLobby() {
        try {
            int maxPlayers = 2;

            var a = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

            var options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject> { { "JoinCodeKey", new DataObject(DataObject.VisibilityOptions.Public, joinCode) } };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby Name", maxPlayers, options);

            StartCoroutine(HeartbeatLobbyCorutine(lobby.Id, 15));

            _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

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
        while (NetworkManager.Singleton.ConnectedClientsList.Count != _maxPlayers) {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    private void SetTransformAsClient(JoinAllocation a) {
        _transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
    }

    private IEnumerator StartGameWhenPlayersConnected() {
        while (NetworkManager.Singleton.ConnectedClientsList.Count != _maxPlayers) {
            Debug.Log(NetworkManager.Singleton.ConnectedClientsList.Count);
            yield return null;
        }
        GameObject server = Instantiate(_serverPrefab, Vector3.zero, Quaternion.identity);
        Debug.Log("server created");
        server.GetComponent<NetworkObject>().Spawn();
    }
}
