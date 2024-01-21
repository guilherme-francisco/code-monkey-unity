using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DeliveryManager : MonoBehaviour {
    
    public static DeliveryManager Instance {get; private set;}
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    [SerializeField] private int waitingRecipeMax;
    [SerializeField] private float spawnRecipeTimerMax;
    [SerializeField] private RecipeListSO recipeListSO;
    private List<RecipeSO> waitingRecipeSOList;
    private float spawRecipeTimer;
    
    private int successfulRecipeAmount; 

    private void Awake() {
        Instance = this;
        waitingRecipeSOList = new List<RecipeSO>();
        successfulRecipeAmount = 0;
    }

    private void Update() {
        spawRecipeTimer -= Time.deltaTime;
        if (spawRecipeTimer <= 0f) {
            spawRecipeTimer = spawnRecipeTimerMax;

            if(GameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipeMax) {            
                RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count)];
                
                waitingRecipeSOList.Add(waitingRecipeSO);

                OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
            }
        }
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
                        // This Recipe ingredient was not found on the Plte
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe) {
                    // Player delivered the correct recipe!

                    successfulRecipeAmount++;

                    waitingRecipeSOList.RemoveAt(i);

                    OnRecipeCompleted?.Invoke(OnRecipeSpawned, EventArgs.Empty);
                    OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
                    return true; 
                }
            }
        }
        // No matches found!
        // Player did not deliver the correct recipe.
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
        return false;
    }

    public List<RecipeSO> GetWaitingRecipeSOList() {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount(){
        return successfulRecipeAmount;
    }
}
