using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour {
    
    public static DeliveryManager Instance {get; private set;}
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    [SerializeField] private int waitingRecipeMax;
    [SerializeField] private float spawnRecipeTimerMax;
    [SerializeField] private RecipeListSO recipeListSO;
    [SerializeField] private float spawRecipeTimer;
    private List<RecipeSO> waitingRecipeSOList;
    
    private int successfulRecipeAmount; 

    private void Awake() {
        Instance = this;
        waitingRecipeSOList = new List<RecipeSO>();
        successfulRecipeAmount = 0;
    }

    private void Update() {
        if(!IsServer) {
            return;
        }

        spawRecipeTimer -= Time.deltaTime;
        if (spawRecipeTimer <= 0f) {
            spawRecipeTimer = spawnRecipeTimerMax;

            if(GameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipeMax) {            
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);                
                
                SpawnNewWaitiginRecipeClientRpc(waitingRecipeSOIndex);
            }
        }
    }


    [ClientRpc]
    private void SpawnNewWaitiginRecipeClientRpc(int waitingRecipeSOIndex){
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];

        waitingRecipeSOList.Add(waitingRecipeSO);

        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    public bool DeliveryRecipe(PlateKitchenObject plateKitchenObject) {
        for(int i = 0; i < waitingRecipeSOList.Count; i++) {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            if(waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.getKichenObjectSOList().Count) {
                // Has the same number of ingredints
                bool plateContentsMatchesRecipe = true;
                foreach(KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) {
                    // Cycling through all ingreidents in the recipe
                    bool ingredientFound = false;
                    foreach(KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.getKichenObjectSOList()) {
                        // Cycling through all ingreidents in the plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO) {
                            ingredientFound = true;
                            break;
                        }
                    }
                    if(!ingredientFound) {
                        // This Recipe ingredient was not found on the Plate
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe) {
                    // Player delivered the correct recipe!
                    DeliverCorrectRecipeServerRpc(i);
                    return true;
                }
            }
        }
        // No matches found!
        // Player did not deliver the correct recipe.
        DeliverIncorrectRecipeServerRpc();
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverIncorrectRecipeServerRpc() {
        DeliverIncorrectRecipeClientRpc();
    }

    [ClientRpc]
     private void DeliverIncorrectRecipeClientRpc() {
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverCorrectRecipeServerRpc(int waitingRecipeSOListIndex) {
        DeliverCorrectRecipeClientRpc(waitingRecipeSOListIndex);
    }

    [ClientRpc]
    private void DeliverCorrectRecipeClientRpc(int waitingRecipeSOListIndex) {
        successfulRecipeAmount++;

        waitingRecipeSOList.RemoveAt(waitingRecipeSOListIndex);

        OnRecipeCompleted?.Invoke(OnRecipeSpawned, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    public List<RecipeSO> GetWaitingRecipeSOList() {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount(){
        return successfulRecipeAmount;
    }
}
