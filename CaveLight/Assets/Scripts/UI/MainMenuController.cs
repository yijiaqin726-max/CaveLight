using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    private const string GameSceneName = "GameScene";

    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
    }

    public void QuitGame()
    {
        StartCoroutine(QuitGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        Time.timeScale = 1f;
        UIAudioManager.Instance?.PlayButtonClick();
        yield return new WaitForSecondsRealtime(0.1f);
        RunStatsManager.Instance.ResetRun();
        SceneManager.LoadScene(GameSceneName);
    }

    private IEnumerator QuitGameRoutine()
    {
        Time.timeScale = 1f;
        UIAudioManager.Instance?.PlayButtonClick();
        yield return new WaitForSecondsRealtime(0.1f);

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
