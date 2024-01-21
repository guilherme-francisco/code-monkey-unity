using System;
using UnityEngine;

public class CuttingCounter : BaseCounter, IHasProgress {
    public static event EventHandler OnAnyCut;

    new public static void ResetStaticData() {
        OnAnyCut = null;
    }
    
    public event EventHandler<IHasProgress.onProgressChangedEventArgs> onProgressChanged;
    public event EventHandler OnCut;

    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;
    private int cuttingProgress;

    public override void Interact(Player player) {
        if(!HasKitchenObject()) {
            // There is no KitchenObject here
            if(player.HasKitchenObject()) {
                if(HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO())) {
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                    cuttingProgress = 0;

                    KitchenObjectSO inputKitchenObjectSO = GetKitchenObject().GetKitchenObjectSO();
                    CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);


                    onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                        progressNormalized = (float) cuttingProgress / cuttingRecipeSO.cuttingProgressMax
                    });
                }
            } else {
                // Player not carrying anything
            }
        } else {
            // There is a KitchenObject here
            if(player.HasKitchenObject()) {
                // Player is carrying something
                if(player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    if(plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        GetKitchenObject().DestroySelf();
                    }

                }
            } else {
                // Player is not carrying anything
                GetKitchenObject().SetKitchenObjectParent(player);
            }
         }
    }

    public override void InteractAlternate(Player player) {
        if(HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO())) {
            // There is a KitchenObject here AND it can be cut
            cuttingProgress++;

            CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

            OnCut?.Invoke(this, EventArgs.Empty);
            OnAnyCut?.Invoke(this, EventArgs.Empty);

            onProgressChanged?.Invoke(this, new IHasProgress.onProgressChangedEventArgs {
                progressNormalized = (float) cuttingProgress / cuttingRecipeSO.cuttingProgressMax
            });

            if(cuttingProgress >=  cuttingRecipeSO.cuttingProgressMax) {
                KitchenObjectSO outputKitchenObjectSO = GetOutputForInput(GetKitchenObject().GetKitchenObjectSO());
                
                GetKitchenObject().DestroySelf();

                KitchenObject.SpawnKitchenObject(outputKitchenObjectSO, this);
            }
        }
    }

    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO) {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        return cuttingRecipeSO != null;
    }

    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO) {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        if(cuttingRecipeSO != null) {
            return cuttingRecipeSO.output;
        } else {
            return null;
        }
    }

    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach(CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray) {
            if(cuttingRecipeSO.input == inputKitchenObjectSO) {
                return cuttingRecipeSO;
            }
        }
        return null;
    }
}
