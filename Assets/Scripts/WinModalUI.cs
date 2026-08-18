using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class WinModalUI : MonoBehaviour
{
    public static WinModalUI Instance;

    [Header("Win VFX")]
    [Tooltip("Kéo thả tất cả các hiệu ứng pháo hoa, vfx win mà bạn đặt trên Scene vào mảng này")]
    public GameObject[] winVFXs;

    [Header("UI Elements")]
    public CanvasGroup bgCanvasGroup;
    public RectTransform youWinImage;
    public CanvasGroup textCanvasGroup;
    public RectTransform returnHomeButton;

    private bool hasTriggeredWin = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        // Chỉ tắt nếu game vừa mở (chưa thắng)
        if (!hasTriggeredWin)
        {
            gameObject.SetActive(false);
            if (winVFXs != null)
            {
                foreach (var vfx in winVFXs)
                {
                    if (vfx != null) vfx.SetActive(false);
                }
            }
        }
    }

    public void TriggerWinSequence()
    {
        if (hasTriggeredWin) return; // Chống gọi trùng lặp nếu 2 boss chết cùng lúc
        hasTriggeredWin = true;

        // Setup trạng thái tàng hình TỪ TRƯỚC khi bật GameObject lên để không bị chớp hình
        if (bgCanvasGroup != null) bgCanvasGroup.alpha = 0f;
        if (youWinImage != null) youWinImage.localScale = Vector3.zero;
        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;
        if (returnHomeButton != null) returnHomeButton.localScale = Vector3.zero;

        // Bật GameObject (lúc này nó đang tàng hình 100%)
        gameObject.SetActive(true);

        StartCoroutine(WinSequenceRoutine());
    }

    private IEnumerator WinSequenceRoutine()
    {
        // 1. Bật toàn bộ VFX Win trước theo yêu cầu của bạn
        if (winVFXs != null)
        {
            foreach (var vfx in winVFXs)
            {
                if (vfx != null) vfx.SetActive(true);
            }
        }

        // Đợi VFX nổ tưng bừng 1 lúc (bạn có thể tăng giảm số giây này)
        yield return new WaitForSeconds(1.5f);

        // Kịch bản (Sequence) biểu diễn UI
        Sequence seq = DOTween.Sequence();
        
        // Kéo nền mờ từ từ hiện ra
        if (bgCanvasGroup != null) 
            seq.Append(bgCanvasGroup.DOFade(1f, 0.5f));
        
        // Chữ "You Win" nhảy BUM ra đập vào mắt (OutBack tạo độ nảy)
        if (youWinImage != null) 
            seq.Append(youWinImage.DOScale(1f, 0.7f).SetEase(Ease.OutBack));
        
        // Hiện text cảm ơn và chúc mừng
        if (textCanvasGroup != null) 
            seq.Append(textCanvasGroup.DOFade(1f, 0.5f));
        
        // Cuối cùng nút bấm lòi ra lúng liếng (OutElastic)
        if (returnHomeButton != null) 
            seq.Append(returnHomeButton.DOScale(1f, 0.8f).SetEase(Ease.OutElastic));
    }

    // Hàm gắn vào Button Return Home
    public void ReturnToMenu()
    {
        // Chặn người chơi bấm nhầm bằng màng chắn (thay vì tắt EventSystem gây lỗi)
        GameObject canvasObj = new GameObject("TransitionCanvas");
        DontDestroyOnLoad(canvasObj);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 9999;
        canvas.sortingLayerName = "Camera";
        
        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(540, 960);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
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
        // Dùng tọa độ chuẩn của CanvasScaler (540x960) nên 1500f là chắc chắn phủ kín mọi góc
        rt.sizeDelta = new Vector2(1500f, 1500f);
        rt.anchoredPosition = Vector2.zero;

        canvasObj.AddComponent<GraphicRaycaster>();

        Tween fadeTween = useShader ? fadeImage.material.DOFloat(0f, "_Radius", 1f).SetEase(Ease.InOutSine) : fadeImage.DOFade(1f, 1f);
        fadeTween.SetUpdate(true); // Đảm bảo tween chạy ngay cả khi timeScale = 0
        
        fadeTween.OnComplete(() =>
        {
            // Lưu dữ liệu lúc màn hình đã đen
            PlayerPrefs.SetInt("GameWon", 1);
            PlayerPrefs.Save();
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame(true);
            }

            // Tắt bảng Win
            this.gameObject.SetActive(false);

            // Bật Main Menu lên (Thay vì reload Scene cực kỳ tốn tài nguyên và dễ sinh bug)
            MainMenuController mainMenu = FindObjectOfType<MainMenuController>(true);
            if (mainMenu != null)
            {
                mainMenu.gameObject.SetActive(true);
                
                // Đảm bảo Main Menu luôn nằm trên cùng để nhận click
                Canvas mmCanvas = mainMenu.GetComponentInParent<Canvas>();
                if (mmCanvas != null)
                {
                    mmCanvas.sortingOrder = 99;
                }
                
                mainMenu.ShowMenu(); // Gọi trực tiếp để bật Canvas và set timeScale = 0
            }

            // Tắt ngay Raycaster của tấm màn đen chuyển cảnh để không cản trở click
            GraphicRaycaster raycaster = canvasObj.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false;

            Tween revealTween = useShader ? fadeImage.material.DOFloat(1.5f, "_Radius", 1f).SetEase(Ease.InOutSine) : fadeImage.DOFade(0f, 1f);
            revealTween.SetUpdate(true).SetDelay(0.5f).OnComplete(() =>
            {
                if (useShader && fadeImage.material != null) Destroy(fadeImage.material);
                Destroy(canvasObj);
            });
        });
    }
}
