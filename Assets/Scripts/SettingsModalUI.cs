using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Gắn script này vào UI Panel chứa cấu hình Setting.
/// Nhớ kéo thả 2 thanh Slider BGM và SFX vào đây thông qua Inspector.
/// </summary>
public class SettingsModalUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Thanh trượt điều chỉnh âm lượng BGM")]
    public Slider bgmSlider;
    [Tooltip("Text hiển thị phần trăm âm lượng BGM")]
    public TextMeshProUGUI bgmText;
    
    [Tooltip("Thanh trượt điều chỉnh âm lượng SFX")]
    public Slider sfxSlider;
    [Tooltip("Text hiển thị phần trăm âm lượng SFX")]
    public TextMeshProUGUI sfxText;

    [Header("Stats References")]
    [Tooltip("Text hiển thị tổng số tiền đã kiếm được (Lifetime Cash)")]
    public TextMeshProUGUI lifetimeCashText;

    private void Awake()
    {
        // Gắn listener 1 lần duy nhất ở Awake để tránh bị add trùng nhiều lần
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void OnEnable()
    {
        // Khi bảng Setting được bật lên, tải giá trị volume hiện tại lên Slider
        if (bgmSlider != null)
        {
            float bgmVol = PlayerPrefs.GetFloat("BGMVolume", 1f);
            bgmSlider.value = bgmVol;
            UpdateBGMText(bgmVol);
        }

        if (sfxSlider != null)
        {
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.value = sfxVol;
            UpdateSFXText(sfxVol);
        }

        // Cập nhật Lifetime Cash khi mở Setting
        if (lifetimeCashText != null && Gamemanager.Instance != null)
        {
            lifetimeCashText.text = "Tổng tiền đã cày: " + CurrencyFormatter.FormatMoney(Gamemanager.Instance.LifetimeCash);
        }
    }

    private void OnBGMVolumeChanged(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(volume);
        }
        UpdateBGMText(volume);
    }

    private void OnSFXVolumeChanged(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
        }
        UpdateSFXText(volume);
    }

    private void UpdateBGMText(float volume)
    {
        if (bgmText != null)
        {
            // Hiển thị dạng phần trăm từ 0 - 100
            bgmText.text = Mathf.RoundToInt(volume * 100f).ToString() + "%";
        }
    }

    private void UpdateSFXText(float volume)
    {
        if (sfxText != null)
        {
            sfxText.text = Mathf.RoundToInt(volume * 100f).ToString() + "%";
        }
    }

    /// <summary>
    /// Nếu có nút Save (Lưu) hoặc Nút Đóng (Close) trên bảng Setting, 
    /// bạn có thể gán hàm này vào sự kiện OnClick() của nút đó để lưu lại cài đặt.
    /// </summary>
    public void SaveSettingsAndClose()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Gán hàm này vào sự kiện OnClick() của nút Mở Cài Đặt (trên Canvas gốc)
    /// </summary>
    public void OpenSettings()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Gán hàm này vào sự kiện OnClick() của nút "Menu" để trở về màn hình chính
    /// </summary>
    public void ReturnToMenu()
    {
        // Lưu lại cài đặt trước khi thoát
        PlayerPrefs.Save();
        
        // Lưu game ngay lập tức trước khi ra Menu để bảo toàn dữ liệu và làm mốc tính Offline
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame(true);
        }

        // Đảm bảo Time.timeScale trở lại bình thường nếu game đang bị pause
        Time.timeScale = 1f;

        // Bật cờ bắt buộc hiện Menu để MainMenuController không tự động nhảy vào Game nữa
        MainMenuController.forceShowMenu = true;

        // Tạo màn đen mờ dần để che lúc reload Scene
        GameObject canvasObj = new GameObject("TransitionCanvas");
        DontDestroyOnLoad(canvasObj);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); 
        
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Chặn người chơi bấm nhầm nút trong lúc đang chuyển cảnh
        canvasObj.AddComponent<GraphicRaycaster>();

        // Fade ra đen (tăng lên 1s cho chậm rãi), sau đó load lại chính Scene hiện tại
        fadeImage.DOFade(1f, 1f).OnComplete(() =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            
            // Đợi 0.5s ở màn hình đen rồi từ từ mở lên trong 1s (cho cân bằng với lúc vào Game)
            fadeImage.DOFade(0f, 1f).SetDelay(0.5f).OnComplete(() =>
            {
                Destroy(canvasObj);
            });
        });
    }
}
