using System.Collections;
using UnityEngine;

public class PlayerInvincibility : MonoBehaviour
{
    public float invincibleDuration = 1f;
    public bool IsInvincible { get; private set; }

    private PlayerEnergyStore energyStore;
    private SpriteRenderer spriteRenderer;
    private Coroutine invincibleCoroutine;
    private bool missingEnergyStoreWarned;

    void Awake()
    {
        energyStore = GetComponent<PlayerEnergyStore>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    void OnValidate()
    {
        invincibleDuration = Mathf.Max(0.05f, invincibleDuration);
    }

    public bool TryTakeEnergyDamage(float amount)
    {
        if (IsInvincible)
        {
            return false;
        }

        if (energyStore == null)
        {
            energyStore = GetComponent<PlayerEnergyStore>();
        }

        if (energyStore == null)
        {
            if (!missingEnergyStoreWarned)
            {
                Debug.LogWarning("[PlayerInvincibility] Missing PlayerEnergyStore. Energy damage ignored.");
                missingEnergyStoreWarned = true;
            }

            return false;
        }

        energyStore.TakeEnergyDamage(amount);
        StartInvincible();
        return true;
    }

    private void StartInvincible()
    {
        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
        }

        invincibleCoroutine = StartCoroutine(InvincibleRoutine());
    }

    private IEnumerator InvincibleRoutine()
    {
        IsInvincible = true;

        float timer = 0f;
        while (timer < invincibleDuration)
        {
            timer += Time.deltaTime;

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                float blink = Mathf.PingPong(timer * 10f, 1f);
                color.a = Mathf.Lerp(0.4f, 1f, blink);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        IsInvincible = false;
        RestoreSpriteAlpha();
        invincibleCoroutine = null;
    }

    private void OnDisable()
    {
        IsInvincible = false;
        RestoreSpriteAlpha();
    }

    private void RestoreSpriteAlpha()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;
    }
}
