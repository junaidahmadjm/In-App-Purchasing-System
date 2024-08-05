using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using System;
using UnityEngine.Purchasing.Security;
using System.Collections;
using UnityEngine.UI;

public class InAppPurchasing : MonoBehaviour, IStoreListener
{
    private static IStoreController m_StoreController;
    private static IExtensionProvider m_StoreExtensionProvider;
    public List<SKU> SKUS;
    public GameObject ConfirmDialog;
    private static Product currentProduct;
    private static bool mIsLiveContext;
    public static InAppPurchasing instance;

    public static void log(string msg)
    {

    }
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (m_StoreController == null)
        {
            log("Initin IAP");
            InitializePurchasing();
            log("Init IAP Done");
        }
    }

    public void InitializePurchasing()
    {
        if (IsInitialized())
        {
            return;
        }
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        UnRegister();
        foreach (var inAppID in SKUS)
        {
            if (inAppID.productType == ProductType.Consumable)
            {
                builder.AddProduct(inAppID.ID, UnityEngine.Purchasing.ProductType.Consumable);
            }
            else if (inAppID.productType == ProductType.NonConsumable)
            {
                builder.AddProduct(inAppID.ID, UnityEngine.Purchasing.ProductType.NonConsumable);
            }
            else if (inAppID.productType == ProductType.Subscription)
            {
                builder.AddProduct(inAppID.ID, UnityEngine.Purchasing.ProductType.Subscription);
            }
        }
        UnityPurchasing.Initialize(this, builder);
        log("SKU List Size::" + SKUS.Count);
    }

    public static bool IsInitialized()
    {
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }

    private static void BuyProductID(string productId)
    {
        if (IsInitialized())
        {
            Product product = m_StoreController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                m_StoreController.InitiatePurchase(product);
            }
            else
            {
                log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        else
        {
            log("BuyProductID FAIL. Not initialized.");
        }
    }

    private static InAppPurchasingCallBacks callbackObj;
    public static void Register(InAppPurchasingCallBacks pCallbackObj)
    {
        callbackObj = pCallbackObj;
    }

    public static void UnRegister()
    {
        callbackObj = null;
    }

    public static void PurchaseItem(string sku, MonoBehaviour context)
    {
        log("purchase item::" + sku);
        Register(context as InAppPurchasingCallBacks);
        BuyProductID(sku);
    }

    public static void restorePurchases(MonoBehaviour context)
    {
        if (context != null)
        {
            callbackObj = context as InAppPurchasingCallBacks;
            if (!IsInitialized())
            {
                log("RestorePurchases FAIL. Not initialized.");
                return;
            }
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                log("RestorePurchases started ...");
                var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
                apple.RestoreTransactions((result) =>
                {
                    log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
                });
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                var google = m_StoreExtensionProvider.GetExtension<IGooglePlayStoreExtensions>();
                google.RestoreTransactions((result) =>
                {
                    log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
                });
            }
            else
            {
                log("RestorePurchases FAIL. Not supported on this platform.");
            }
        }
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        log("OnInitialized: PASS");
        m_StoreController = controller;
        m_StoreExtensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        log("OnInitializeFailed InitializationFailureReason:" + error);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        log("In ProcessPurchase:" + args.ToString());
        bool isValid = ValidatePurchase(args);
        currentProduct = args.purchasedProduct;
        if (isValid)
        {
            if (callbackObj != null)
            {
                log("Call back is not null");
                callbackObj.PurchaseSuccessful(currentProduct.definition.id);
                ShowAcknowledgePurchase(true);
                UnRegister();
            }
            return PurchaseProcessingResult.Complete;
        }
        else
        {
            UnRegister();
            return PurchaseProcessingResult.Complete;
        }
    }

    public bool ifInAppisPurchased(string productid)
    {
        if (IsInitialized())
        {
            Product product = m_StoreController.products.WithID(productid);
            if (product != null && product.hasReceipt)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public bool CheckIfSubscriptionExist(string subsId)
    {
        if (IsInitialized())
        {
            try
            {
                Product product = m_StoreController.products.WithID(subsId);
                SubscriptionManager p = new SubscriptionManager(product, null);
                SubscriptionInfo info = p.getSubscriptionInfo();
                if (info.isFreeTrial() == Result.True)
                    return true;
                if (info.isSubscribed() == Result.True)
                    return true;
            }
            catch (Exception ex)
            {
                log("CheckIfSubscriptionExist Exception: " + ex.Message);
            }
        }
        return false;
    }

    public bool CheckIfSubscriptionExpired(string subsId)
    {
        if (IsInitialized())
        {
            try
            {
                Product product = m_StoreController.products.WithID(subsId);
                SubscriptionManager p = new SubscriptionManager(product, null);
                SubscriptionInfo info = p.getSubscriptionInfo();
                if (info.isExpired() == Result.True)
                    return true;
                if (info.isCancelled() == Result.True)
                    return true;
            }
            catch (Exception ex)
            {
                log("CheckIfSubscriptionExpired Exception: " + ex.Message);
            }
        }
        return false;
    }

    public void ShowAcknowledgePurchase(bool liveContext)
    {
        log("In ShowAcknowledgePurchase");
        mIsLiveContext = liveContext;
        ConfirmDialog.SetActive(true);
        string idName = getProductName(currentProduct.definition.id);
    }

    public string getProductName(string packageId)
    {
        string name = "";
        foreach (SKU sku in SKUS)
        {
            if (sku.ID.Equals(packageId))
            {
                name = sku.ID;
            }
        }
        return name;
    }

    public void HideAckPurchase()
    {
        ConfirmDialog.SetActive(false);
    }

    private bool ValidatePurchase(PurchaseEventArgs e)
    {
        bool validPurchase = true;
        return validPurchase;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        if (callbackObj != null)
        {
            callbackObj.PurchaseFailed(product.definition.id);
        }
        log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
    }

    public void onRestoreTransactionsFinished(bool success)
    {
        if (callbackObj != null)
        {
            callbackObj.PurchaseSuccessful(null);
            ShowAcknowledgePurchase(true);
            log("InAppPurchasing::callbackObj.PurchaseSuccessful");
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        throw new NotImplementedException();
    }
}

public interface InAppPurchasingCallBacks
{
    bool PurchaseSuccessful(string sku);
    void PurchaseFailed(string sku);
}

[System.Serializable]
public class SKU
{
    public string ID;
    public ProductType productType;
}

public enum ProductType { Consumable, NonConsumable, Subscription }
