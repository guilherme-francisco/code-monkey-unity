using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    [SerializeField] private Button playMultiplayerButton;
    [SerializeField] private Button playSingleplayerButton;
    [SerializeField] private Button quitButton;


    private void Awake() {
        playMultiplayerButton.onClick.AddListener(() => {
            KitchenGameMultiplayer.SetPlayMultiplayer(true);
            LoaderScene.Load(LoaderScene.Scene.LobbyScene);
        });

        playSingleplayerButton.onClick.AddListener(() => {
            KitchenGameMultiplayer.SetPlayMultiplayer(false);
            LoaderScene.Load(LoaderScene.Scene.LobbyScene);
        });

        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        Time.timeScale = 1f;
    }
}
