using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSound : MonoBehaviour {
    [SerializeField] private float volume;
    [SerializeField] private float footStepTimerMax;
    [SerializeField] private Player player;

    private float footstepTimer;
    private void Awake() {
        player = GetComponent<Player>();
    }

    private void Update() {
        footstepTimer -= Time.deltaTime;
        if (footstepTimer < 0f) {
            footstepTimer = footStepTimerMax;

            if (player.IsWalking()) {
                SoundManager.Instance.PlayFootStepSounds(player.transform.position, volume);
            }
        }
    }
}
