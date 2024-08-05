using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoScript : MonoBehaviour, InAppPurchasingCallBacks
{
    private void Start()
    {
        if (PlayerPrefs.GetInt("RemoveAds") != 1)
        {
            Debug.Log("Ads Will Appear");
        }
        else
        {
            Debug.Log("Ads Removed Successfully");
        }
    }
    public void RemoveAdsButton()
    {
        InAppPurchasing.PurchaseItem(InAppPurchasing.instance.SKUS[0].ID, this);
    }
    public bool PurchaseSuccessful(string sku)
    {
        if(sku == InAppPurchasing.instance.SKUS[0].ID)
        {
            PlayerPrefs.SetInt("RemoveAds", 1);
            Debug.Log("Ads Removed Successfully");
            return true;
        }
        return false;
    }
    public void PurchaseFailed(string sku)
    {
        throw new System.NotImplementedException();
    }

}
