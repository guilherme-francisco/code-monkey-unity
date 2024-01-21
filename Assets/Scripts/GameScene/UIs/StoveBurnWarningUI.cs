using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveBurnWarningUI : MonoBehaviour {
    [SerializeField] private StoveCounter stoveCounter;

    private void Start() {
        stoveCounter.onProgressChanged += StoveCounter_OnProgressChanged;

        Hide();
    }

    private void StoveCounter_OnProgressChanged(object sender, IHasProgress.onProgressChangedEventArgs e) {
        float burnShowProgressAmount = .5f;
        bool show = stoveCounter.IsFried() && e.progressNormalized >= burnShowProgressAmount;

        if(show){
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
