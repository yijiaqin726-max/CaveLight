using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MerchantRoomController : MonoBehaviour
{
    [Header("Player")]
    public PlayerEnergyStore playerEnergyStore;
    public EnergyAbsorbController energyAbsorbController;

    [Header("Item Buttons")]
    public Button itemButton1;
    public Button itemButton2;
    public Button itemButton3;

    [Header("Item Text")]
    public Text itemText1;
    public Text itemText2;
    public Text itemText3;
    public TMP_Text itemTmpText1;
    public TMP_Text itemTmpText2;
    public TMP_Text itemTmpText3;

    [Header("Navigation")]
    public Button enterNextCaveButton;

    private bool backpackPurchased;
    private bool slowBurnPurchased;
    private bool biggerAbsorbPurchased;

    private const float BackpackCost = 30f;
    private const float SlowBurnCost = 25f;
    private const float BiggerAbsorbCost = 20f;

    void Awake()
    {
        FindReferences();
        BindButtons();
    }

    void OnEnable()
    {
        FindReferences();
        RefreshButtons();
    }

    public void OnEnterMerchantRoom()
    {
        backpackPurchased = false;
        slowBurnPurchased = false;
        biggerAbsorbPurchased = false;
        FindReferences();
        BindButtons();
        RefreshButtons();
    }

    private void FindReferences()
    {
        if (playerEnergyStore == null)
        {
            playerEnergyStore = FindFirstObjectByType<PlayerEnergyStore>();
        }

        if (energyAbsorbController == null)
        {
            energyAbsorbController = FindFirstObjectByType<EnergyAbsorbController>();
        }

        AutoFindUiReferences();
    }

    private void BindButtons()
    {
        if (itemButton1 != null)
        {
            itemButton1.onClick.RemoveListener(BuyEnergyBackpack);
            itemButton1.onClick.AddListener(BuyEnergyBackpack);
        }

        if (itemButton2 != null)
        {
            itemButton2.onClick.RemoveListener(BuySlowBurn);
            itemButton2.onClick.AddListener(BuySlowBurn);
        }

        if (itemButton3 != null)
        {
            itemButton3.onClick.RemoveListener(BuyBiggerAbsorb);
            itemButton3.onClick.AddListener(BuyBiggerAbsorb);
        }
    }

    private void BuyEnergyBackpack()
    {
        if (backpackPurchased)
        {
            return;
        }

        if (!TrySpendEnergy(BackpackCost, "Energy Backpack"))
        {
            return;
        }

        playerEnergyStore.maxEnergy += 20f;
        playerEnergyStore.AddEnergy(20f);
        backpackPurchased = true;
        Debug.Log("[MerchantRoomController] Purchased Energy Backpack.");
        RefreshButtons();
    }

    private void BuySlowBurn()
    {
        if (slowBurnPurchased)
        {
            return;
        }

        if (!TrySpendEnergy(SlowBurnCost, "Slow Burn"))
        {
            return;
        }

        playerEnergyStore.drainPerSecond *= 0.8f;
        slowBurnPurchased = true;
        Debug.Log("[MerchantRoomController] Purchased Slow Burn.");
        RefreshButtons();
    }

    private void BuyBiggerAbsorb()
    {
        if (biggerAbsorbPurchased)
        {
            return;
        }

        if (!TrySpendEnergy(BiggerAbsorbCost, "Bigger Absorb"))
        {
            return;
        }

        if (energyAbsorbController != null)
        {
            energyAbsorbController.absorbRadius += 1f;
        }

        biggerAbsorbPurchased = true;
        Debug.Log("[MerchantRoomController] Purchased Bigger Absorb.");
        RefreshButtons();
    }

    private bool TrySpendEnergy(float cost, string itemName)
    {
        FindReferences();

        if (playerEnergyStore == null || playerEnergyStore.currentEnergy < cost)
        {
            Debug.Log($"[MerchantRoomController] Not enough Energy for {itemName}.");
            return false;
        }

        playerEnergyStore.ConsumeEnergy(cost);
        return true;
    }

    private void RefreshButtons()
    {
        SetItemState(itemButton1, itemText1, itemTmpText1, "Energy Backpack - 30 Energy\nMax Energy +20", backpackPurchased);
        SetItemState(itemButton2, itemText2, itemTmpText2, "Slow Burn - 25 Energy\nDrain x0.8", slowBurnPurchased);
        SetItemState(itemButton3, itemText3, itemTmpText3, "Bigger Absorb - 20 Energy\nAbsorb Radius +1", biggerAbsorbPurchased);
    }

    private void SetItemState(Button button, Text text, TMP_Text tmpText, string availableText, bool purchased)
    {
        if (button != null)
        {
            button.interactable = !purchased;
        }

        string displayText = purchased ? "Purchased" : availableText;
        if (text != null)
        {
            text.text = displayText;
        }

        if (tmpText != null)
        {
            tmpText.text = displayText;
        }
    }

    private void AutoFindUiReferences()
    {
        if (itemButton1 == null)
        {
            itemButton1 = FindButtonInChildren("Energy Backpack");
        }

        if (itemButton2 == null)
        {
            itemButton2 = FindButtonInChildren("Slow Burn");
        }

        if (itemButton3 == null)
        {
            itemButton3 = FindButtonInChildren("Bigger Absorb");
        }

        if (enterNextCaveButton == null)
        {
            enterNextCaveButton = FindButtonInChildren("Enter Next Cave");
        }

        AutoFindTextReferences(itemButton1, ref itemText1, ref itemTmpText1);
        AutoFindTextReferences(itemButton2, ref itemText2, ref itemTmpText2);
        AutoFindTextReferences(itemButton3, ref itemText3, ref itemTmpText3);
    }

    private Button FindButtonInChildren(string objectName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == objectName)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void AutoFindTextReferences(Button button, ref Text text, ref TMP_Text tmpText)
    {
        if (button == null)
        {
            return;
        }

        if (text == null)
        {
            text = button.GetComponentInChildren<Text>(true);
        }

        if (tmpText == null)
        {
            tmpText = button.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
