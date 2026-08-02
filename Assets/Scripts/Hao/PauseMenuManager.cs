using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Bảng Tạm Dừng Game (Pause Menu Manager)
/// Tự động dừng thời gian Time.timeScale = 0f khi bật bảng
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("Pause UI References")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button settingsButton;
    public Button mainMenuButton;

    [Header("State")]
    public bool isPaused = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        // Đảm bảo mặc định TẮT Pause Panel khi mở Game (Tránh đè UI)
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    private void Update()
    {
        // Nhấn phím ESC hoặc P để bật/tắt Pause Menu
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Dừng thời gian game

        // 1. Tự động tìm PauseMenuPanel nếu chưa kéo gán
        if (pauseMenuPanel == null)
        {
            Transform p = transform.root.Find("Canvas (1)/PauseMenuPanel");
            if (p == null) p = transform.root.Find("PauseMenuPanel");
            if (p == null) p = GameObject.Find("PauseMenuPanel")?.transform;
            if (p != null) pauseMenuPanel = p.gameObject;
        }

        // Ẩn bảng nhiệm vụ MissionPanel đằng sau để chống đè UI
        GameObject mPanel = GameObject.Find("MissionPanel");
        if (mPanel == null) mPanel = transform.root.Find("Canvas (1)/MissionPanel")?.gameObject;
        if (mPanel != null) mPanel.SetActive(false);

        if (StageHUDUI.Instance != null && StageHUDUI.Instance.hudPanelObject != null)
        {
            StageHUDUI.Instance.hudPanelObject.SetActive(false);
        }

        // 2. Nếu tìm thấy Panel, bật hiển thị đè lên trên cùng và tự động căn to nút bấm
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            pauseMenuPanel.transform.SetAsLastSibling(); // Đưa lên lớp trên cùng

            // Ép phủ full màn hình
            RectTransform rect = pauseMenuPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            // Tự động căn to kích thước 3 nút bấm vừa khít lòng khung gỗ
            FormatButtons(pauseMenuPanel.transform);
        }
        else
        {
            // 3. Nếu chưa có Panel trong Scene, tự động tạo động
            CreateDynamicPauseMenu();
        }

        Debug.Log("<color=yellow>[PauseMenuManager] ĐÃ TẠM DỪNG GAME (PAUSE)!</color>");
    }

    private void FormatButtons(Transform parent)
    {
        // Chuyển Tiêu đề TAM DUNG GAME bự hơn & vị trí chuẩn
        Transform titleObj = parent.Find("Title") ?? parent.Find("Card/Title");
        if (titleObj != null)
        {
            RectTransform rTitle = titleObj.GetComponent<RectTransform>();
            rTitle.anchoredPosition = new Vector2(0f, 440f);
            rTitle.sizeDelta = new Vector2(700f, 100f);
            TextMeshProUGUI txt = titleObj.GetComponent<TextMeshProUGUI>();
            if (txt != null) txt.fontSize = 54; // Bự hơn một chút
        }

        Transform b1 = parent.Find("ResumeButton") ?? parent.Find("Resume") ?? parent.Find("Card/ResumeButton");
        if (b1 != null)
        {
            RectTransform r1 = b1.GetComponent<RectTransform>();
            r1.anchorMin = r1.anchorMax = r1.pivot = new Vector2(0.5f, 0.5f);
            r1.anchoredPosition = new Vector2(0f, 270f);
            r1.sizeDelta = new Vector2(520f, 300f); // Giảm bớt chiều cao
        }

        Transform b2 = parent.Find("SettingsButton") ?? parent.Find("Setting") ?? parent.Find("Settings") ?? parent.Find("Card/SettingsButton");
        if (b2 != null)
        {
            RectTransform r2 = b2.GetComponent<RectTransform>();
            r2.anchorMin = r2.anchorMax = r2.pivot = new Vector2(0.5f, 0.5f);
            r2.anchoredPosition = new Vector2(0f, 10f);
            r2.sizeDelta = new Vector2(520f, 460f); // Setting bự hơn 40px (460px)
        }

        Transform b3 = parent.Find("MainMenuButton") ?? parent.Find("MainMenu") ?? parent.Find("Card/MainMenuButton");
        if (b3 != null)
        {
            RectTransform r3 = b3.GetComponent<RectTransform>();
            r3.anchorMin = r3.anchorMax = r3.pivot = new Vector2(0.5f, 0.5f);
            r3.anchoredPosition = new Vector2(0f, -250f);
            r3.sizeDelta = new Vector2(520f, 300f); // Giảm bớt chiều cao
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Chạy lại thời gian game

        if (pauseMenuPanel == null)
        {
            pauseMenuPanel = GameObject.Find("PauseMenuPanel");
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Bật lại MissionPanel nếu trước đó đang mở
        if (StageHUDUI.Instance != null && StageHUDUI.Instance.hudPanelObject != null && StageHUDUI.Instance.isPanelOpen)
        {
            StageHUDUI.Instance.hudPanelObject.SetActive(true);
        }

        Debug.Log("<color=green>[PauseMenuManager] CHẠY LẠI GAME (RESUME)!</color>");
    }

    public void OpenSettings()
    {
        Debug.Log("<color=yellow>[PauseMenuManager] MỞ BẢNG SETTINGS TỪ PAUSE MENU!</color>");
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.OnSettingsClicked();
        }
        else
        {
            GameObject sModal = GameObject.Find("SettingsModal");
            if (sModal != null)
            {
                sModal.SetActive(true);
                sModal.transform.SetAsLastSibling();
            }
            else
            {
                CreateDynamicSettingsModal();
            }
        }
    }

    private void CreateDynamicSettingsModal()
    {
        Transform canvasTr = GetMainUICanvasTransform();

        GameObject modal = new GameObject("SettingsModal", typeof(RectTransform), typeof(CanvasRenderer));
        modal.transform.SetParent(canvasTr, false);
        modal.transform.SetAsLastSibling();

        RectTransform modalRect = modal.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero; modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero; modalRect.offsetMax = Vector2.zero;

        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

        GameObject bgOverlay = new GameObject("BackgroundOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgOverlay.transform.SetParent(modal.transform, false);
        RectTransform bgRect = bgOverlay.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgOverlay.GetComponent<Image>();
        bgImg.sprite = whiteSprite;
        bgImg.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(modal.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f); cardRect.anchorMax = new Vector2(0.5f, 0.5f); cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(560f, 640f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImg = card.GetComponent<Image>();
        Sprite boardSprite = null;
#if UNITY_EDITOR
        boardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/main_menu_empty_center_board.png");
        if (boardSprite == null) boardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/main_menu_large_board.png");
#endif
        cardImg.sprite = boardSprite != null ? boardSprite : whiteSprite;
        if (boardSprite == null) cardImg.color = new Color(0.2f, 0.14f, 0.08f, 0.98f);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 220f); titleRect.sizeDelta = new Vector2(500f, 60f);
        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.text = "CAI DAT GAME"; titleTxt.alignment = TextAlignmentOptions.Center; titleTxt.fontSize = 36; titleTxt.fontStyle = FontStyles.Bold; titleTxt.color = new Color(1f, 0.85f, 0.3f);

        GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(card.transform, false);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchoredPosition = new Vector2(0f, -200f); closeRect.sizeDelta = new Vector2(240f, 70f);
        Image closeImg = closeObj.GetComponent<Image>();
        Sprite closeBtnSprite = null;
#if UNITY_EDITOR
        closeBtnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/button_resume.png");
#endif
        closeImg.sprite = closeBtnSprite != null ? closeBtnSprite : whiteSprite;
        if (closeBtnSprite == null) closeImg.color = new Color(0.8f, 0.2f, 0.1f, 1f);
        Button closeBtn = closeObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(() => modal.SetActive(false));

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        RectTransform closeTxtRect = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRect.anchorMin = Vector2.zero; closeTxtRect.anchorMax = Vector2.one; closeTxtRect.offsetMin = Vector2.zero; closeTxtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTxt = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTxt.text = "DONG"; closeTxt.alignment = TextAlignmentOptions.Center; closeTxt.fontSize = 24; closeTxt.fontStyle = FontStyles.Bold; closeTxt.color = Color.white;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Chạy lại thời gian
        isPaused = false;

        if (pauseMenuPanel == null)
        {
            pauseMenuPanel = GameObject.Find("PauseMenuPanel");
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (MainMenuManager.Instance != null && MainMenuManager.Instance.mainMenuPanel != null)
        {
            MainMenuManager.Instance.mainMenuPanel.SetActive(true);
            MainMenuManager.Instance.mainMenuPanel.transform.SetAsLastSibling();
        }

        Debug.Log("<color=cyan>[PauseMenuManager] TRỞ VỀ MAIN MENU CHÍNH!</color>");
    }

    private Transform GetMainUICanvasTransform()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c.transform;
            }
        }
        GameObject cObj = GameObject.Find("Canvas (1)") ?? GameObject.Find("Canvas");
        if (cObj != null) return cObj.transform;
        return transform.root;
    }

    private void CreateDynamicPauseMenu()
    {
        Transform canvasTr = GetMainUICanvasTransform();

        // Panel nền đen mờ 85% phủ full màn hình
        GameObject panel = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasTr, false);
        panel.transform.SetAsLastSibling();

        RectTransform pRect = panel.GetComponent<RectTransform>();
        pRect.anchorMin = Vector2.zero;
        pRect.anchorMax = Vector2.one;
        pRect.offsetMin = Vector2.zero;
        pRect.offsetMax = Vector2.zero;

        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

        Image bgImg = panel.GetComponent<Image>();
        bgImg.sprite = whiteSprite;
        bgImg.color = new Color(0f, 0f, 0f, 0.85f); // Nền đen mờ 85% full màn hình

        // Card khung gỗ Pause Menu (Phủ FULL MÀN HÌNH 100%)
        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(panel.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;

        Image cardImg = card.GetComponent<Image>();
        Sprite pauseMenuSprite = null;
#if UNITY_EDITOR
        pauseMenuSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/pause_menu.png");
        if (pauseMenuSprite == null) pauseMenuSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/main_menu_empty_center_board.png");
        if (pauseMenuSprite == null) pauseMenuSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/main_menu_large_board.png");
        if (pauseMenuSprite == null) pauseMenuSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/main_menu.png");
#endif
        if (pauseMenuSprite == null) pauseMenuSprite = Resources.Load<Sprite>("UI/pause_menu");
        cardImg.sprite = pauseMenuSprite != null ? pauseMenuSprite : whiteSprite;
        if (pauseMenuSprite == null) cardImg.color = new Color(0.20f, 0.13f, 0.08f, 0.98f);

        // Title TẠM DỪNG GAME (Phía trên cùng bảng gỗ - Thấp hơn xuống một chút)
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 440f); // Thấp xuống một chút (440px)
        titleRect.sizeDelta = new Vector2(600f, 80f);

        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.text = "TAM DUNG GAME";
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.fontSize = 42;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = new Color(1f, 0.85f, 0.3f);

        // Nút 1: RESUME (520x340 - Giảm 40px)
        GameObject btn1 = new GameObject("ResumeButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btn1.transform.SetParent(card.transform, false);
        RectTransform b1Rect = btn1.GetComponent<RectTransform>();
        b1Rect.anchorMin = new Vector2(0.5f, 0.5f);
        b1Rect.anchorMax = new Vector2(0.5f, 0.5f);
        b1Rect.pivot = new Vector2(0.5f, 0.5f);
        b1Rect.anchoredPosition = new Vector2(0f, 270f);
        b1Rect.sizeDelta = new Vector2(520f, 340f);
        resumeButton = btn1.GetComponent<Button>();
        resumeButton.onClick.AddListener(ResumeGame);
        Image img1 = btn1.GetComponent<Image>();
        Sprite s1 = null;
#if UNITY_EDITOR
        s1 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/button_resume.png");
#endif
        if (s1 == null) s1 = Resources.Load<Sprite>("UI/button_resume");
        img1.sprite = s1 != null ? s1 : whiteSprite;
        if (s1 == null) img1.color = new Color(0.35f, 0.22f, 0.12f, 1f);

        if (s1 == null)
        {
            GameObject t1Obj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            t1Obj.transform.SetParent(btn1.transform, false);
            RectTransform t1Rect = t1Obj.GetComponent<RectTransform>();
            t1Rect.anchorMin = Vector2.zero; t1Rect.anchorMax = Vector2.one;
            t1Rect.offsetMin = Vector2.zero; t1Rect.offsetMax = Vector2.zero;
            TextMeshProUGUI t1Txt = t1Obj.GetComponent<TextMeshProUGUI>();
            t1Txt.text = "RESUME"; t1Txt.alignment = TextAlignmentOptions.Center; t1Txt.fontSize = 32; t1Txt.fontStyle = FontStyles.Bold; t1Txt.color = Color.white;
        }

        // Nút 2: SETTINGS (520x420 - Bự hơn 40px)
        GameObject btn2 = new GameObject("SettingsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btn2.transform.SetParent(card.transform, false);
        RectTransform b2Rect = btn2.GetComponent<RectTransform>();
        b2Rect.anchorMin = new Vector2(0.5f, 0.5f);
        b2Rect.anchorMax = new Vector2(0.5f, 0.5f);
        b2Rect.pivot = new Vector2(0.5f, 0.5f);
        b2Rect.anchoredPosition = new Vector2(0f, 10f);
        b2Rect.sizeDelta = new Vector2(520f, 420f);
        settingsButton = btn2.GetComponent<Button>();
        settingsButton.onClick.AddListener(OpenSettings);
        Image img2 = btn2.GetComponent<Image>();
        Sprite s2 = null;
#if UNITY_EDITOR
        s2 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/button_settings.png");
#endif
        if (s2 == null) s2 = Resources.Load<Sprite>("UI/button_settings");
        img2.sprite = s2 != null ? s2 : whiteSprite;
        if (s2 == null) img2.color = new Color(0.35f, 0.22f, 0.12f, 1f);

        if (s2 == null)
        {
            GameObject t2Obj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            t2Obj.transform.SetParent(btn2.transform, false);
            RectTransform t2Rect = t2Obj.GetComponent<RectTransform>();
            t2Rect.anchorMin = Vector2.zero; t2Rect.anchorMax = Vector2.one;
            t2Rect.offsetMin = Vector2.zero; t2Rect.offsetMax = Vector2.zero;
            TextMeshProUGUI t2Txt = t2Obj.GetComponent<TextMeshProUGUI>();
            t2Txt.text = "SETTINGS"; t2Txt.alignment = TextAlignmentOptions.Center; t2Txt.fontSize = 32; t2Txt.fontStyle = FontStyles.Bold; t2Txt.color = Color.white;
        }

        // Nút 3: MAIN MENU (520x340 - Giảm 40px)
        GameObject btn3 = new GameObject("MainMenuButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btn3.transform.SetParent(card.transform, false);
        RectTransform b3Rect = btn3.GetComponent<RectTransform>();
        b3Rect.anchorMin = new Vector2(0.5f, 0.5f);
        b3Rect.anchorMax = new Vector2(0.5f, 0.5f);
        b3Rect.pivot = new Vector2(0.5f, 0.5f);
        b3Rect.anchoredPosition = new Vector2(0f, -250f);
        b3Rect.sizeDelta = new Vector2(520f, 340f);
        mainMenuButton = btn3.GetComponent<Button>();
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        Image img3 = btn3.GetComponent<Image>();
        Sprite s3 = null;
#if UNITY_EDITOR
        s3 = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/button_mainmenu.png");
#endif
        if (s3 == null) s3 = Resources.Load<Sprite>("UI/button_mainmenu");
        img3.sprite = s3 != null ? s3 : whiteSprite;
        if (s3 == null) img3.color = new Color(0.35f, 0.22f, 0.12f, 1f);

        if (s3 == null)
        {
            GameObject t3Obj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            t3Obj.transform.SetParent(btn3.transform, false);
            RectTransform t3Rect = t3Obj.GetComponent<RectTransform>();
            t3Rect.anchorMin = Vector2.zero; t3Rect.anchorMax = Vector2.one;
            t3Rect.offsetMin = Vector2.zero; t3Rect.offsetMax = Vector2.zero;
            TextMeshProUGUI t3Txt = t3Obj.GetComponent<TextMeshProUGUI>();
            t3Txt.text = "MAIN MENU"; t3Txt.alignment = TextAlignmentOptions.Center; t3Txt.fontSize = 28; t3Txt.fontStyle = FontStyles.Bold; t3Txt.color = Color.white;
        }

        pauseMenuPanel = panel;
    }
}
