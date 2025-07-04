using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource SFXSource;

    [Header("Audio Clip")]
    public AudioClip background;
    public AudioClip Count;
    public AudioClip Win;

    private bool isMainScene = false;
    private bool isRaceWon = false;

    private void Awake()
    {
        // Đảm bảo AudioManager tồn tại duy nhất trong toàn bộ game (singleton)
        if (FindObjectsOfType<AudioManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    if (scene.name == "Main")
    {
        isMainScene = true;
        isRaceWon = false; // Đã reset nhưng cần đảm bảo nó hoạt động
        Debug.Log("AudioManager: Reset isRaceWon to false in Main scene");
        StartCoroutine(PlayMainSceneAudioSequence());
    }
    else
    {
        isMainScene = false;
        isRaceWon = false;
        if (musicSource.isPlaying) musicSource.Stop();
        musicSource.clip = background;
        musicSource.loop = true;
        musicSource.Play();
    }
}

public void ResetRaceState()
{
    isRaceWon = false;
    Debug.Log("AudioManager: ResetRaceState called, isRaceWon set to false");
}

    private System.Collections.IEnumerator PlayMainSceneAudioSequence()
    {
        if (Count != null && SFXSource != null)
        {
            SFXSource.clip = Count;
            SFXSource.Play();
            yield return new WaitForSeconds(Count.length); // Chờ Count kết thúc
        }

        if (background != null && musicSource != null)
        {
            musicSource.clip = background;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void OnWinLineTrigger()
    {
        Debug.Log("OnWinLineTrigger called, isMainScene: " + isMainScene + ", isRaceWon: " + isRaceWon);
        if (isMainScene && !isRaceWon)
        {
            if (musicSource.isPlaying) musicSource.Stop();
            if (Win != null && SFXSource != null)
            {
                isRaceWon = true;
                SFXSource.clip = Win;
                SFXSource.Play();
                StartCoroutine(ResumeBackgroundAfterWin());
            }
        }
    }

    private System.Collections.IEnumerator ResumeBackgroundAfterWin()
    {
        yield return new WaitForSeconds(Win != null ? Win.length : 3f); // Chờ Win kết thúc
        if (background != null && musicSource != null)
        {
            musicSource.clip = background;
            musicSource.loop = true;
            musicSource.Play();
            isRaceWon = false;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}