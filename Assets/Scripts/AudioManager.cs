using UnityEngine;
using System.Collections;

[System.Serializable]
public class GameAudioClips
{
    [Header("Player Interaction Sounds")]
    public AudioClip buttonPress;           // When player presses E
    public AudioClip bookPickup;            // When picking up a book
    public AudioClip bookStamp;             // When stamping at checkout
    public AudioClip bookSort;              // When sorting book covers
    
    [Header("Customer Sounds")]
    public AudioClip customerHappy;         // Successful delivery
    public AudioClip customerAngry;         // Wrong delivery
    public AudioClip customerSpawn;         // When customer spawns
    
    [Header("Game State Sounds")]
    public AudioClip timerWarning;          // When timer gets low
    public AudioClip timerDanger;           // When timer is critically low
    public AudioClip dayComplete;           // Day completed successfully
    public AudioClip dayFailed;            // Day failed
    public AudioClip dayStart;              // Day starts
    
    [Header("UI Sounds")]
    public AudioClip uiClick;               // Menu button clicks
    public AudioClip uiHover;               // Menu button hover
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Clips")]
    public GameAudioClips gameClips;
    
    [Header("Audio Sources")]
    public AudioSource sfxSource;          // For sound effects
    public AudioSource uiSource;           // For UI sounds
    public AudioSource musicSource;        // For background music
    public AudioSource ambientSource;      // For ambient sounds
    
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float uiVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float ambientVolume = 0.3f;
    
    [Header("Timer Audio Settings")]
    public float warningThreshold = 30f;    // Warning at 30 seconds
    public float dangerThreshold = 10f;     // Danger at 10 seconds
    private bool warningPlayed = false;
    private bool dangerPlayed = false;
    private Coroutine timerBeepCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyVolumeSettings();
        
        // Subscribe to timer events if GameTimer exists
        GameTimer.OnTimerUpdate += OnTimerUpdate;
    }

    void OnDestroy()
    {
        GameTimer.OnTimerUpdate -= OnTimerUpdate;
    }

    void SetupAudioSources()
    {
        // Create audio sources if they don't exist
        if (sfxSource == null)
            sfxSource = CreateAudioSource("SFX Source");
        
        if (uiSource == null)
            uiSource = CreateAudioSource("UI Source");
            
        if (musicSource == null)
        {
            musicSource = CreateAudioSource("Music Source");
            musicSource.loop = true;
        }
        
        if (ambientSource == null)
        {
            ambientSource = CreateAudioSource("Ambient Source");
            ambientSource.loop = true;
        }
    }

    AudioSource CreateAudioSource(string name)
    {
        GameObject audioObj = new GameObject(name);
        audioObj.transform.SetParent(transform);
        return audioObj.AddComponent<AudioSource>();
    }

    void ApplyVolumeSettings()
    {
        if (sfxSource) sfxSource.volume = sfxVolume * masterVolume;
        if (uiSource) uiSource.volume = uiVolume * masterVolume;
        if (musicSource) musicSource.volume = musicVolume * masterVolume;
        if (ambientSource) ambientSource.volume = ambientVolume * masterVolume;
    }

    // Public methods for playing sounds
    public void PlayButtonPress()
    {
        PlaySFX(gameClips.buttonPress);
    }

    public void PlayBookPickup()
    {
        PlaySFX(gameClips.bookPickup);
    }

    public void PlayBookStamp()
    {
        PlaySFX(gameClips.bookStamp);
    }

    public void PlayBookSort()
    {
        PlaySFX(gameClips.bookSort);
    }

    public void PlayCustomerHappy()
    {
        PlaySFX(gameClips.customerHappy);
    }

    public void PlayCustomerAngry()
    {
        PlaySFX(gameClips.customerAngry);
    }

    public void PlayCustomerSpawn()
    {
        PlaySFX(gameClips.customerSpawn);
    }

    public void PlayDayComplete()
    {
        PlaySFX(gameClips.dayComplete);
    }

    public void PlayDayFailed()
    {
        PlaySFX(gameClips.dayFailed);
    }

    public void PlayDayStart()
    {
        PlaySFX(gameClips.dayStart);
    }

    public void PlayUIClick()
    {
        PlayUI(gameClips.uiClick);
    }

    public void PlayUIHover()
    {
        PlayUI(gameClips.uiHover);
    }

    // Timer audio handling
    void OnTimerUpdate(float timeRemaining)
    {
        if (timeRemaining <= dangerThreshold && !dangerPlayed)
        {
            PlayTimerDanger();
            dangerPlayed = true;
            warningPlayed = true; // Skip warning if we're already in danger
        }
        else if (timeRemaining <= warningThreshold && !warningPlayed)
        {
            PlayTimerWarning();
            warningPlayed = true;
        }
        
        // Reset flags when timer resets (new day)
        if (timeRemaining > warningThreshold)
        {
            warningPlayed = false;
            dangerPlayed = false;
            StopTimerBeeping();
        }
    }

    void PlayTimerWarning()
    {
        PlaySFX(gameClips.timerWarning);
    }

    void PlayTimerDanger()
    {
        PlaySFX(gameClips.timerDanger);
        
        // Start urgent beeping
        if (timerBeepCoroutine != null)
            StopCoroutine(timerBeepCoroutine);
        timerBeepCoroutine = StartCoroutine(PlayUrgentBeeping());
    }

    IEnumerator PlayUrgentBeeping()
    {
        while (true)
        {
            PlaySFX(gameClips.timerDanger, 0.3f); // Quieter beeps
            yield return new WaitForSeconds(1f); // Beep every second
        }
    }

    void StopTimerBeeping()
    {
        if (timerBeepCoroutine != null)
        {
            StopCoroutine(timerBeepCoroutine);
            timerBeepCoroutine = null;
        }
    }

    // Core audio playback methods
    void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeMultiplier);
        }
    }

    void PlayUI(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && uiSource != null)
        {
            uiSource.PlayOneShot(clip, volumeMultiplier);
        }
    }

    public void PlayMusic(AudioClip musicClip)
    {
        if (musicClip != null && musicSource != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlayAmbient(AudioClip ambientClip)
    {
        if (ambientClip != null && ambientSource != null)
        {
            ambientSource.clip = ambientClip;
            ambientSource.Play();
        }
    }

    // Volume control methods
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }

    // Test methods for the inspector
    [ContextMenu("Test Book Pickup Sound")]
    public void TestBookPickupSound()
    {
        PlayBookPickup();
    }

    [ContextMenu("Test Button Press Sound")]
    public void TestButtonPressSound()
    {
        PlayButtonPress();
    }

    [ContextMenu("Test Customer Happy Sound")]
    public void TestCustomerHappySound()
    {
        PlayCustomerHappy();
    }
}
