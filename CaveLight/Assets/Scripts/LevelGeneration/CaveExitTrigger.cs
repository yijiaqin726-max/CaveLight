using UnityEngine;

public class CaveExitTrigger : MonoBehaviour
{
    private CaveLevelGenerator levelGenerator;
    private bool triggered = false;

    public void SetLevelGenerator(CaveLevelGenerator generator)
    {
        levelGenerator = generator;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggered) return;

        // Check if the colliding object has PlaceholderPlayerController2D
        if (collision.GetComponent<PlaceholderPlayerController2D>() != null)
        {
            triggered = true;
            Debug.Log("[CaveExitTrigger] Player reached exit. Going to next cave.");
            if (levelGenerator != null)
            {
                levelGenerator.GoToNextCave();
            }
        }
    }
}
