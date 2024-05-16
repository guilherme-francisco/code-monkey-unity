using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;


    private void Awake() {
        playButton.onClick.AddListener(() => {
            LoaderScene.Load(LoaderScene.Scene.LobbyScene);
        });

        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        Time.timeScale = 1f;
    }
}
