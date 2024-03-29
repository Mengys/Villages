using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace HelloWorld {
    public class HelloWorldManager : MonoBehaviour {

        [SerializeField] GameObject _serverPrefab;
        [SerializeField] GameObject _startButton;

        private int _maxPlayers = 2;

        void OnGUI() {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) {
                StartButtons();
            } else {
                StatusLabels();
                _startButton.GetComponent<StartButton>().HideButton();
            }

            GUILayout.EndArea();
        }

        void StartButtons() {
            if (GUILayout.Button("Host")) {
                NetworkManager.Singleton.StartHost();
                StartCoroutine(StartGameWhenPlayersConnected());
            }
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server")) {
                NetworkManager.Singleton.StartServer();
                StartCoroutine(StartGameWhenPlayersConnected());
            }
        }

        void StatusLabels() {
            var mode = NetworkManager.Singleton.IsHost ?
                "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
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
}