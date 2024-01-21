using TMPro;
using UnityEngine;
using System;

public class GameOverUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI recipeDeliveredText;

    private void Start() {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        Hide();
    }

    private void GameManager_OnStateChanged(object sender, EventArgs e) {
        if(GameManager.Instance.IsGameOver()) {
            
            recipeDeliveredText.text = DeliveryManager.Instance.GetSuccessfulRecipesAmount().ToString();
            Show();
        } else {
            Hide();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
