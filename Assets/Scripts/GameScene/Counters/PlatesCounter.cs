using System;
using Unity.Netcode;
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
        if(!IsServer) {
            return;
        }

        spawnPlateTimer += Time.deltaTime;
        if(spawnPlateTimer > spawnPlateTimerMax) {
            spawnPlateTimer = 0f;
            if (GameManager.Instance.IsGamePlaying() && plateSpawnedAmount < platesSpawnedAmountMax) {
                SpawnPlateServerRpc();
            }
        }
    }

    [ServerRpc]
    private void SpawnPlateServerRpc() {
        SpawnPlateClientRpc();
    }

    [ClientRpc]
    private void SpawnPlateClientRpc() {
        plateSpawnedAmount++;

        OnPlateSpawned?.Invoke(this, EventArgs.Empty);
    }

    public override void Interact(Player player) {
        if(!player.HasKitchenObject()) {
            if(plateSpawnedAmount > 0) {
                KitchenObject.SpawnKitchenObject(platesKitchenOjectSO, player);

                InteractLogicServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc() {
        plateSpawnedAmount --;
        InteractLogicClientRpc();

    }

    [ClientRpc]
    private void InteractLogicClientRpc() {
        OnPlateRemoved?.Invoke(this, EventArgs.Empty);

    }
}
