using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    public AudioSource audioSource;
    public AudioClip buttonClickClip;
    public float clickVolume = 0.5f;

    private bool warnedMissingClip;

    private void Awake()
    {
        Instance = this;
        EnsureAudioSource();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayButtonClick()
    {
        EnsureAudioSource();

        if (buttonClickClip == null)
        {
            if (!warnedMissingClip)
            {
                warnedMissingClip = true;
                Debug.LogWarning("[UIAudioManager] Button click clip is missing.");
            }

            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickClip, clickVolume);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }
}
