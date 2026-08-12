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
        // Chặn UI click ngay lập tức
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.enabled = false;

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
        fadeImage.color = Color.black; 
        
        Shader circleShader = Shader.Find("UI/CircleReveal");
        bool useShader = false;
        if (circleShader != null)
        {
            Material mat = new Material(circleShader);
            mat.SetFloat("_Radius", 1.5f);
            fadeImage.material = mat;
            useShader = true;
        }
        else 
        {
            fadeImage.color = new Color(0, 0, 0, 0); 
        }
        
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float maxSize = Mathf.Max(Screen.width, Screen.height) * 1.5f;
        rt.sizeDelta = new Vector2(maxSize, maxSize);
        rt.anchoredPosition = Vector2.zero;

        // Chặn người chơi bấm nhầm nút trong lúc đang chuyển cảnh
        canvasObj.AddComponent<GraphicRaycaster>();

        // Khép vòng tròn (tăng lên 1s cho chậm rãi), sau đó load lại chính Scene hiện tại
        Tween fadeTween = useShader ? fadeImage.material.DOFloat(0f, "_Radius", 1f).SetEase(Ease.InOutSine) : fadeImage.DOFade(1f, 1f);
        fadeTween.SetUpdate(true); // Đảm bảo tween chạy kể cả khi timeScale = 0
        
        fadeTween.OnComplete(() =>
        {
            // Đưa logic lưu game vào đây, lúc màn hình đã đen hoàn toàn để không thấy game bị khựng
            PlayerPrefs.Save();
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame(true);
            }
            Time.timeScale = 1f;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.enabled = true;

            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            
            // Đợi 0.5s ở màn hình đen rồi từ từ mở vòng tròn lên trong 1s (cho cân bằng với lúc vào Game)
            Tween revealTween = useShader ? fadeImage.material.DOFloat(1.5f, "_Radius", 1f).SetEase(Ease.InOutSine) : fadeImage.DOFade(0f, 1f);
            revealTween.SetDelay(0.5f).OnComplete(() =>
            {
                if (useShader && fadeImage.material != null) Destroy(fadeImage.material);
                Destroy(canvasObj);
            });
        });
    }
}
