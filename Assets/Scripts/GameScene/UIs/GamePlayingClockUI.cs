using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayingClockUI : MonoBehaviour {
    [SerializeField] private UnityEngine.UI.Image timerImage;

    private void Update() {
      timerImage.fillAmount = GameManager.Instance.GetPlayingTimerNormalized();
    }
}
