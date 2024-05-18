using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    private void Awake() {
        mainMenuButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.LeaveLobby();
            NetworkManager.Singleton.Shutdown();
            LoaderScene.Load(LoaderScene.Scene.MainMenuScene);
        });
    
        readyButton.onClick.AddListener(() => {
            CharacterSelectReady.Instance.SetPlayerReady();
        });
    }

    private void Start() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetJoinedLobby();
        lobbyNameText.text = "Lobby Name: " + joinedLobby.Name;
        lobbyCodeText.text = "Lobby Code: " + joinedLobby.LobbyCode;
    }
}
