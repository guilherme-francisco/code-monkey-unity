using System;
using UnityEngine;

public class PlatesCounter : BaseCounter {
    public event EventHandler OnPlateSpawned;
    public event EventHandler OnPlateRemoved;
    [SerializeField] private int platesSpawnedAmountMax;
    [SerializeField] private float spawnPlateTimerMax;
    [SerializeField] private KitchenObjectSO platesKitchenOjectSO;
    private float spawnPlateTimer;
    private int plateSpawnedAmount;

    private void Update() {
        spawnPlateTimer += Time.deltaTime;
        if(spawnPlateTimer > spawnPlateTimerMax) {
            spawnPlateTimer = 0f;
            if (GameManager.Instance.IsGamePlaying() && plateSpawnedAmount < platesSpawnedAmountMax) {
                plateSpawnedAmount++;

                OnPlateSpawned?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public override void Interact(Player player) {
        if(!player.HasKitchenObject()) {
            if(plateSpawnedAmount > 0) {
                plateSpawnedAmount --;
                
                KitchenObject.SpawnKitchenObject(platesKitchenOjectSO, player);

                OnPlateRemoved?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
