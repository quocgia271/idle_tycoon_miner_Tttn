using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.IO;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo CanvasGroup của Menu (nếu chưa có thì script sẽ tự thêm)")]
    public CanvasGroup menuCanvasGroup;

    [Header("Transition Settings")]
    [Tooltip("Thời gian màn hình tối đi khi bắt đầu Play")]
    public float fadeOutTime = 0.5f;
    [Tooltip("Thời gian hiện chữ Loading giả")]
    public float fakeLoadingDuration = 1.5f;
    [Tooltip("Thời gian màn hình sáng dần để vào Game")]
    public float fadeInTime = 0.5f;

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
            if (File.Exists(savePath))
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

    private void ShowMenu()
    {
        menuCanvasGroup.alpha = 1;
        menuCanvasGroup.interactable = true;
        menuCanvasGroup.blocksRaycasts = true;
    }

    public void PlayGame()
    {
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
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
        // Nếu load thẳng vào thì đen luôn, nếu từ Menu thì trong suốt
        fadeImage.color = startInstantlyBlack ? new Color(0, 0, 0, 1) : new Color(0, 0, 0, 0); 
        
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

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
            letter.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (letter.font == null) letter.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            letter.fontSize = 35;
            letter.color = Color.white;
            letter.alignment = TextAnchor.MiddleCenter;

            RectTransform letterRt = letter.GetComponent<RectTransform>();
            letterRt.sizeDelta = new Vector2(spacing, 100);
            letterRt.anchorMin = new Vector2(1, 0.5f);
            letterRt.anchorMax = new Vector2(1, 0.5f);
            letterRt.pivot = new Vector2(1, 0.5f);
            
            float xPos = -totalWidth + (i * spacing);
            letterRt.anchoredPosition = new Vector2(xPos, 0);

            // Hiệu ứng gợn sóng (nhảy lên xuống)
            letterRt.DOAnchorPosY(15f, 0.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(i * 0.1f);

            // Hiệu ứng màu Blend (Cầu vồng mượt mà trôi qua từng chữ)
            // Dùng DOVirtual để chạy biến Hue từ 0 đến 1, tạo ra đủ các dải màu xen kẽ
            DOVirtual.Float(0f, 1f, 2f, (hue) => {
                if (letter != null) 
                    letter.color = Color.HSVToRGB(hue, 0.55f, 1f); // Saturation 0.55 để màu blend dạng pastel nịnh mắt
            }).SetLoops(-1, LoopType.Restart).SetDelay(i * 0.15f).SetTarget(letterObj);
        }

        canvasObj.AddComponent<GraphicRaycaster>();

        // Kịch bản (Sequence) hiệu ứng chuyển cảnh "Fake Loading" mượt 100%
        Sequence seq = DOTween.Sequence();
        
        if (startInstantlyBlack)
        {
            // Nếu đã đen sẵn thì chỉ làm mờ chữ Loading hiện lên
            seq.Append(textCanvasGroup.DOFade(1f, fadeOutTime));
        }
        else
        {
            // Nếu từ Menu thì Màn tối dần và chữ hiện lên
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

        // 5. Mở sáng màn hình, lộ ra Game đang chạy sẵn
        seq.Append(fadeImage.DOFade(0f, fadeInTime));

        // 6. Xóa Canvas ảo và tắt Script Menu
        seq.OnComplete(() => {
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
