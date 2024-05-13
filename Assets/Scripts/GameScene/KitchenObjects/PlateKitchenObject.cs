using System;
using System.Collections.Generic;
using UnityEngine;

public class PlateKitchenObject : KitchenObject {
    public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
    public class OnIngredientAddedEventArgs : EventArgs {
        public KitchenObjectSO KitchenObjectSO;
    }

   [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;
    private List<KitchenObjectSO> kitchenObjectSOList;

    protected override void Awake() {
        base.Awake();
        kitchenObjectSOList = new List<KitchenObjectSO>();
    }
    
    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO) {
        if(!kitchenObjectSOList.Contains(kitchenObjectSO) && validKitchenObjectSOList.Contains(kitchenObjectSO)) {
            kitchenObjectSOList.Add(kitchenObjectSO);

            OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs {
                KitchenObjectSO = kitchenObjectSO
            });
            
            return true;
        } else {
            return false;
        }
    }

    public List<KitchenObjectSO> getKichenObjectSOList() {
        return kitchenObjectSOList;
    }
}
