using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounterSound : MonoBehaviour {
    [SerializeField] private StoveCounter stoveCounter;
    [SerializeField] private float warningSoundTimerMax;
    private AudioSource audioSource;
    private float warningSoundTimer;
    private bool playWarningSound;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        warningSoundTimer = warningSoundTimerMax;
    }

    private void Start() {
        stoveCounter.OnStateChanged += StoveCounter_OnStateChanged;
        stoveCounter.onProgressChanged += StoveCounter_OnProgressChanged;
    }

    private void StoveCounter_OnProgressChanged(object sender, IHasProgress.onProgressChangedEventArgs e) {
        float burnShowProgressAmount = .5f;
        playWarningSound = stoveCounter.IsFried() && e.progressNormalized >= burnShowProgressAmount;

    }

    private void StoveCounter_OnStateChanged(object sender, StoveCounter.OnStateChangedEventArgs e) {
        bool playSound = e.state == StoveCounter.State.Frying || e.state == StoveCounter.State.Fried;
        if (playSound) {
            audioSource.Play();
        } else {
            audioSource.Pause();
        }
    }

    private void Update() {
        if(playWarningSound){
            warningSoundTimer -= Time.deltaTime;
            if (warningSoundTimer < 0f) {
                warningSoundTimer = warningSoundTimerMax;

                SoundManager.Instance.PlayWarningSound(gameObject.transform.position);
            }
        }
    }
}
