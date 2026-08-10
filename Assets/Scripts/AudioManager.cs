using UnityEngine;
using UnityEngine.EventSystems;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Source chạy nhạc nền (lặp lại)")]
    public AudioSource bgmSource;
    [Tooltip("Source chạy các hiệu ứng âm thanh nhỏ lẻ (bắn súng, cháy nổ, click)")]
    public AudioSource sfxSource;
    [Tooltip("Source chạy âm thanh cháy âm ỉ dưới nền (lặp lại, âm lượng nhỏ hơn BGM)")]
    public AudioSource fireLoopSource;

    [Header("Default Audio Clips")]
    public AudioClip bgmClip;
    public AudioClip popClickClip;

    [Header("PlayerPrefs Keys")]
    private const string BGM_VOL_KEY = "BGMVolume";
    private const string SFX_VOL_KEY = "SFXVolume";

    private int fireVFXCount = 0;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Tùy chọn: giữ audio khi chuyển scene
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tải settings lưu trữ từ trước
        LoadSettings();
    }

    private void Start()
    {
        // Bật nhạc nền
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // Cài đặt cho fire loop (tự động loop)
        if (fireLoopSource != null)
        {
            fireLoopSource.loop = true;
            fireLoopSource.Stop(); // Đảm bảo chưa chạy khi chưa có lửa
        }
    }

    private void Update()
    {
        // Global Click Detection - Phát tiếng Pop khi người dùng click chuột
        if (Input.GetMouseButtonDown(0))
        {
            CheckAndPlayClickSound();
        }
    }

    private void CheckAndPlayClickSound()
    {
        // 1. Kiểm tra xem có đang click vào một UI Button / Element nào đó không
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            PlaySFX(popClickClip);
            return;
        }

        // 2. Nếu không click vào UI, dùng Raycast 2D để xem có trúng vật thể Game nào không (Hầm, Thợ mỏ, Boss)
        if (Camera.main != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                // Nếu click trúng bất cứ thứ gì có Collider (vd BoxCollider2D của Miner, Shaft, quái)
                PlaySFX(popClickClip);
            }
        }
    }

    #region PUBLIC API
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void AddFireCount()
    {
        fireVFXCount++;
        if (fireVFXCount > 0 && fireLoopSource != null && !fireLoopSource.isPlaying)
        {
            fireLoopSource.Play();
        }
    }

    public void RemoveFireCount()
    {
        fireVFXCount--;
        if (fireVFXCount <= 0)
        {
            fireVFXCount = 0;
            if (fireLoopSource != null && fireLoopSource.isPlaying)
            {
                fireLoopSource.Stop();
            }
        }
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = volume;
        }
        PlayerPrefs.SetFloat(BGM_VOL_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
        if (fireLoopSource != null)
        {
            // Âm lượng của Loop cháy có thể đặt bằng SFX, hoặc tự scale xuống nhỏ hơn 1 chút
            fireLoopSource.volume = volume; 
        }
        PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
    }
    #endregion

    public void LoadSettings()
    {
        // Mặc định âm lượng là 1 (Max) nếu chưa từng lưu
        float bgmVol = PlayerPrefs.GetFloat(BGM_VOL_KEY, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);

        SetBGMVolume(bgmVol);
        SetSFXVolume(sfxVol);
    }
}
