using System.Reflection;
using UnityEngine;

public class EnergyDebugHUD : MonoBehaviour
{
    private PlayerEnergyStore playerEnergyStore;
    private CaveLevelGenerator caveLevelGenerator;
    private FieldInfo caveAmountField;
    private FieldInfo killAmountField;

    void Awake()
    {
        FindReferences();
    }

    void OnGUI()
    {
        if (playerEnergyStore == null || caveLevelGenerator == null)
        {
            FindReferences();
        }

        GUI.Label(new Rect(10f, 10f, 240f, 24f), GetEnergyText());
        GUI.Label(new Rect(10f, 32f, 240f, 24f), GetCaveText());
        GUI.Label(new Rect(10f, 54f, 240f, 24f), GetKillText());
    }

    private void FindReferences()
    {
        if (playerEnergyStore == null)
        {
            playerEnergyStore = Object.FindFirstObjectByType<PlayerEnergyStore>();
        }

        if (caveLevelGenerator == null)
        {
            caveLevelGenerator = Object.FindFirstObjectByType<CaveLevelGenerator>();
            caveAmountField = null;
            killAmountField = null;
        }

        if (caveLevelGenerator != null && caveAmountField == null)
        {
            caveAmountField = typeof(CaveLevelGenerator).GetField("caveAmount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (caveLevelGenerator != null && killAmountField == null)
        {
            killAmountField = typeof(CaveLevelGenerator).GetField("killAmount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    private string GetEnergyText()
    {
        if (playerEnergyStore == null)
        {
            return "Energy: -- / --";
        }

        return $"Energy: {playerEnergyStore.currentEnergy:0} / {playerEnergyStore.maxEnergy:0}";
    }

    private string GetCaveText()
    {
        if (caveLevelGenerator == null || caveAmountField == null)
        {
            return "Cave: --";
        }

        object value = caveAmountField.GetValue(caveLevelGenerator);
        return $"Cave: {value}";
    }

    private string GetKillText()
    {
        if (caveLevelGenerator == null || killAmountField == null)
        {
            return "Kill: --";
        }

        object value = killAmountField.GetValue(caveLevelGenerator);
        return $"Kill: {value}";
    }
}
