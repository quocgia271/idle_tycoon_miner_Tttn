using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.IO;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo CanvasGroup của Menu (nếu chưa có thì script sẽ tự thêm)")]
    public CanvasGroup menuCanvasGroup;
    
    [Header("End Game Setup")]
    [Tooltip("Nút Play Game để có thể tắt đi nếu đã phá đảo")]
    public Button playButton;
    [Tooltip("Text/GameObject Coming Soon để bật lên khi phá đảo")]
    public GameObject comingSoonText;

    [Header("Transition Settings")]
    [Tooltip("Thời gian màn hình tối đi khi bắt đầu Play")]
    public float fadeOutTime = 0.5f;
    [Tooltip("Thời gian hiện chữ Loading giả")]
    public float fakeLoadingDuration = 1.5f;
    [Tooltip("Thời gian màn hình sáng dần để vào Game")]
    public float fadeInTime = 0.5f;

    [Header("Fake Loading Customization")]
    [Tooltip("Font chữ cho Loading (kéo thả Font vào đây, để trống dùng mặc định)")]
    public Font customLoadingFont;
    [Tooltip("Đổi màu liên tục? (Tắt đi nếu chỉ muốn 1 màu cố định)")]
    public bool useRainbowColor = true;
    [Tooltip("Màu của chữ Loading (chỉ có tác dụng nếu TẮT cầu vồng ở trên)")]
    public Color staticLoadingColor = Color.white;

    // Cờ này để báo hiệu rẳng lần tới mở lại Scene thì bắt buộc hiện Menu
    public static bool forceShowMenu = false;

    // Cờ này để nhảy thẳng vào Game không hiện Menu, không hiện cả Fake Loading (dành cho lúc Qua Màn hoặc Chuyển Sinh)
    public static bool skipMenuInstantly = false;
    
    // Đảm bảo chỉ auto-skip ở lần mở game ĐẦU TIÊN
    private static bool hasDoneInitialLoad = false;

    private void Awake()
    {
        if (menuCanvasGroup == null) 
        {
            menuCanvasGroup = GetComponent<CanvasGroup>();
            if (menuCanvasGroup == null) menuCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Chặn tương tác trong lúc setup
        menuCanvasGroup.interactable = false;

        if (skipMenuInstantly)
        {
            skipMenuInstantly = false;
            menuCanvasGroup.alpha = 0;
            menuCanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            return;
        }

        // Nếu có lệnh ép buộc hiện Menu (từ Settings)
        if (forceShowMenu)
        {
            forceShowMenu = false;
            ShowMenu();
            return;
        }

        // Xử lý vào thẳng Game (tự động bật Fake Loading) cho người chơi cũ
        if (!hasDoneInitialLoad)
        {
            hasDoneInitialLoad = true;
            string savePath = Path.Combine(Application.persistentDataPath, "idle_tycoon_save.json");
            
            // CHỈ auto skip vào game nếu người chơi CHƯA win game
            if (File.Exists(savePath) && PlayerPrefs.GetInt("GameWon", 0) == 0)
            {
                // Tắt Menu đi
                menuCanvasGroup.alpha = 0;
                menuCanvasGroup.blocksRaycasts = false;

                // Bật Fake Loading nhưng xuất phát từ nền đen thui luôn
                StartFakeLoading(true);
                return;
            }
        }
        
        ShowMenu();
    }

    public void ShowMenu()
    {
        Time.timeScale = 0f; // Dừng hoàn toàn game khi ở Menu

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseGameAudio(); // Tắt các tiếng ồn của Game
        }

        menuCanvasGroup.alpha = 1;
        menuCanvasGroup.interactable = true;
        menuCanvasGroup.blocksRaycasts = true;

        // Kểm tra trạng thái Win Game để khóa nút Play
        if (PlayerPrefs.GetInt("GameWon", 0) == 1)
        {
            if (playButton != null) playButton.interactable = false;
            if (comingSoonText != null) comingSoonText.SetActive(true);
        }
        else
        {
            if (playButton != null) playButton.interactable = true;
            if (comingSoonText != null) comingSoonText.SetActive(false);
        }
    }

    public void PlayGame()
    {
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        // Tính toán lại tiến trình Offline (nếu người chơi vừa treo máy ở Menu hoặc mới mở game)
        if (OfflineProgressionManager.Instance != null && SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
        {
            OfflineProgressionManager.Instance.ProcessOfflineProgression(SaveManager.Instance.CurrentSaveData);
        }

        // Bật Fake Loading với hiệu ứng mờ dần từ sáng sang đen
        StartFakeLoading(false);
    }

    private void StartFakeLoading(bool startInstantlyBlack)
    {
        // Tạo lại màn đen bao phủ và chữ Loading lượn sóng (Fake Loading Transition)
        GameObject canvasObj = new GameObject("TransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; 
        
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = Color.black; 
        
        Shader circleShader = Shader.Find("UI/CircleReveal");
        if (circleShader != null)
        {
            Material mat = new Material(circleShader);
            mat.SetFloat("_Radius", startInstantlyBlack ? 0f : 1.5f);
            fadeImage.material = mat;
        }
        else 
        {
            fadeImage.color = startInstantlyBlack ? new Color(0, 0, 0, 1) : new Color(0, 0, 0, 0); 
        }
        
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float maxSize = Mathf.Max(Screen.width, Screen.height) * 1.5f;
        rt.sizeDelta = new Vector2(maxSize, maxSize);
        rt.anchoredPosition = Vector2.zero;

        // Container chứa chữ Loading dạng gợn sóng
        GameObject textContainer = new GameObject("LoadingTextContainer");
        textContainer.transform.SetParent(canvasObj.transform, false);
        CanvasGroup textCanvasGroup = textContainer.AddComponent<CanvasGroup>();
        textCanvasGroup.alpha = 0; 
        
        RectTransform containerRt = textContainer.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(1, 0);
        containerRt.anchorMax = new Vector2(1, 0);
        containerRt.pivot = new Vector2(1, 0);
        containerRt.anchoredPosition = new Vector2(-50, 50);
        containerRt.sizeDelta = new Vector2(400, 100);

        string textStr = "Loading...";
        float spacing = 22f;
        float totalWidth = textStr.Length * spacing;
        
        for (int i = 0; i < textStr.Length; i++)
        {
            GameObject letterObj = new GameObject("Letter");
            letterObj.transform.SetParent(textContainer.transform, false);
            Text letter = letterObj.AddComponent<Text>();
            letter.text = textStr[i].ToString();
            // Ưu tiên dùng Font tùy chỉnh nếu được gán, nếu không thì dùng mặc định
            if (customLoadingFont != null)
            {
                letter.font = customLoadingFont;
            }
            else
            {
                letter.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (letter.font == null) letter.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            
            letter.fontSize = 35;
            letter.color = staticLoadingColor;
            letter.alignment = TextAnchor.MiddleCenter;

            RectTransform letterRt = letter.GetComponent<RectTransform>();
            letterRt.sizeDelta = new Vector2(spacing, 100);
            letterRt.anchorMin = new Vector2(1, 0.5f);
            letterRt.anchorMax = new Vector2(1, 0.5f);
            letterRt.pivot = new Vector2(1, 0.5f);
            
            float xPos = -totalWidth + (i * spacing);
            letterRt.anchoredPosition = new Vector2(xPos, 0);

            // Hiệu ứng gợn sóng (nhảy lên xuống)
            letterRt.DOAnchorPosY(15f, 0.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(i * 0.1f).SetUpdate(true);

            // Hiệu ứng màu
            if (useRainbowColor)
            {
                // Cầu vồng mượt mà trôi qua từng chữ
                DOVirtual.Float(0f, 1f, 2f, (hue) => {
                    if (letter != null) 
                        letter.color = Color.HSVToRGB(hue, 0.55f, 1f);
                }).SetLoops(-1, LoopType.Restart).SetDelay(i * 0.15f).SetTarget(letterObj).SetUpdate(true);
            }
        }

        canvasObj.AddComponent<GraphicRaycaster>();

        // Kịch bản (Sequence) hiệu ứng chuyển cảnh "Fake Loading" mượt 100%
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true); // Ignore Time.timeScale = 0
        
        bool useShader = circleShader != null;

        if (startInstantlyBlack)
        {
            // Nếu đã đen sẵn thì chỉ làm mờ chữ Loading hiện lên
            seq.Append(textCanvasGroup.DOFade(1f, fadeOutTime));
        }
        else
        {
            // Nếu từ Menu thì Màn tối dần (vòng tròn khép lại) và chữ hiện lên
            if (useShader)
                seq.Append(fadeImage.material.DOFloat(0f, "_Radius", fadeOutTime).SetEase(Ease.InOutSine));
            else
                seq.Append(fadeImage.DOFade(1f, fadeOutTime));
                
            seq.Join(textCanvasGroup.DOFade(1f, fadeOutTime));
            
            // Lúc màn đen hoàn toàn, ẩn Menu gốc đi 
            seq.AppendCallback(() => {
                if (menuCanvasGroup != null) menuCanvasGroup.alpha = 0;
            });
        }

        // 3. Để chữ lượn sóng cho người chơi thưởng thức
        seq.AppendInterval(fakeLoadingDuration);

        // 4. Chữ mờ đi (Tốc độ mờ chữ luôn fix sẵn 0.4s cho đẹp)
        seq.Append(textCanvasGroup.DOFade(0f, 0.4f));

        // Gọi hàm bật lại thời gian và âm thanh ngay TRƯỚC KHI hé mở màn hình
        seq.AppendCallback(() => {
            Time.timeScale = 1f; 
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeGameAudio(); 
            }
        });

        // 5. Mở sáng màn hình, lộ ra Game đang chạy sẵn (vòng tròn mở rộng ra)
        if (useShader)
            seq.Append(fadeImage.material.DOFloat(1.5f, "_Radius", fadeInTime).SetEase(Ease.InOutSine));
        else
            seq.Append(fadeImage.DOFade(0f, fadeInTime));

        // 6. Xóa Canvas ảo và tắt Script Menu
        seq.OnComplete(() => {
            if (useShader && fadeImage.material != null) Destroy(fadeImage.material);
            Destroy(canvasObj);
            gameObject.SetActive(false);
        });
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit!");
    }
}
