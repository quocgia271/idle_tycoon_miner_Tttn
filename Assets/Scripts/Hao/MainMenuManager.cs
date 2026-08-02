using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Màn hình Main Menu chính của Game (Start Game, Setting, Credit, Quit Game)
/// Phong cách Tiki Tropical Island Idle Tycoon Miner
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Main Menu UI References")]
    public GameObject mainMenuPanel;
    public Button startGameButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitGameButton;

    [Header("Settings Modal References")]
    public GameObject settingsModalObject;
    public Toggle sfxToggle;
    public Toggle musicToggle;
    public Toggle vibrationToggle;
    public Button closeSettingsButton;

    [Header("Credits Modal References")]
    public GameObject creditsModalObject;
    public TextMeshProUGUI creditsText;
    public Button closeCreditsButton;

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
        AutoBindButtons();

        // Gán sự kiện OnClick cho các nút bấm Main Menu
        if (startGameButton != null) { startGameButton.onClick.RemoveAllListeners(); startGameButton.onClick.AddListener(OnStartGameClicked); }
        if (settingsButton != null) { settingsButton.onClick.RemoveAllListeners(); settingsButton.onClick.AddListener(OnSettingsClicked); }
        if (creditsButton != null) { creditsButton.onClick.RemoveAllListeners(); creditsButton.onClick.AddListener(OnCreditsClicked); }
        if (quitGameButton != null) { quitGameButton.onClick.RemoveAllListeners(); quitGameButton.onClick.AddListener(OnQuitClicked); }

        // Gán sự kiện đóng Modal
        if (closeSettingsButton != null) { closeSettingsButton.onClick.RemoveAllListeners(); closeSettingsButton.onClick.AddListener(CloseSettingsModal); }
        if (closeCreditsButton != null) { closeCreditsButton.onClick.RemoveAllListeners(); closeCreditsButton.onClick.AddListener(CloseCreditsModal); }

        // Đảm bảo mở Main Menu lúc đầu nếu chưa bấm Start
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsModalObject != null) settingsModalObject.SetActive(false);
        if (creditsModalObject != null) creditsModalObject.SetActive(false);
    }

    private void AutoBindButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        foreach (Button b in allButtons)
        {
            string name = b.name.ToLower();
            if (startGameButton == null && (name.Contains("play") || name.Contains("start")))
            {
                startGameButton = b;
            }
            else if (settingsButton == null && name.Contains("setting"))
            {
                settingsButton = b;
            }
            else if (creditsButton == null && name.Contains("credit"))
            {
                creditsButton = b;
            }
            else if (quitGameButton == null && (name.Contains("quit") || name.Contains("exit")))
            {
                quitGameButton = b;
            }
        }
    }

    /// <summary>
    /// Nút START GAME: Bắt đầu vào Game Play
    /// </summary>
    public void OnStartGameClicked()
    {
        Debug.Log("<color=green>[MainMenuManager] BẮT ĐẦU VÀO GAMEPLAY TIKI MINER!</color>");

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false); // Ẩn Main Menu
        }

        // Bật Bảng HUD Màn chơi
        if (StageHUDUI.Instance != null)
        {
            StageHUDUI.Instance.RefreshUI();
        }
    }

    /// <summary>
    /// Nút SETTING: Mở Bảng Cài đặt
    /// </summary>
    public void OnSettingsClicked()
    {
        Debug.Log("<color=yellow>[MainMenuManager] Mở Bảng Cài đặt (Settings)!</color>");
        
        // Tự động dọn dẹp Modal cũ bị lỗi trong Scene để nảy bật Popup chuẩn 100%
        GameObject oldSettings = GameObject.Find("SettingsModal");
        if (oldSettings != null && oldSettings != settingsModalObject)
        {
            Destroy(oldSettings);
        }
        if (settingsModalObject != null)
        {
            Destroy(settingsModalObject);
            settingsModalObject = null;
        }

        CreateDynamicSettingsModal();
    }

    /// <summary>
    /// Nút CREDIT: Mở Bảng Tác giả / Đội ngũ sản xuất
    /// </summary>
    public void OnCreditsClicked()
    {
        Debug.Log("<color=yellow>[MainMenuManager] Mở Bảng Credit (Tác giả)!</color>");

        // Tự động dọn dẹp Modal cũ bị lỗi trong Scene để nảy bật Popup chuẩn 100%
        GameObject oldCredits = GameObject.Find("CreditsModal");
        if (oldCredits != null && oldCredits != creditsModalObject)
        {
            Destroy(oldCredits);
        }
        if (creditsModalObject != null)
        {
            Destroy(creditsModalObject);
            creditsModalObject = null;
        }

        CreateDynamicCreditsModal();
    }

    /// <summary>
    /// Nút QUIT GAME: Thoát trò chơi
    /// </summary>
    public void OnQuitClicked()
    {
        Debug.Log("<color=red>[MainMenuManager] ĐÃ BẤM THOÁT GAME (QUIT GAME)!</color>");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CloseSettingsModal()
    {
        if (settingsModalObject != null) settingsModalObject.SetActive(false);
    }

    public void CloseCreditsModal()
    {
        if (creditsModalObject != null) creditsModalObject.SetActive(false);
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

    private Sprite GetCleanWoodSprite()
    {
        Sprite sprite = null;
#if UNITY_EDITOR
        string path = "Assets/UI/button_credits_clean_wood.png";
        UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        if (importer != null && importer.textureType != UnityEditor.TextureImporterType.Sprite)
        {
            importer.textureType = UnityEditor.TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#endif
        return sprite;
    }

    private void FormatCreditsModal(Transform modal)
    {
        // 1. Cập nhật RectTransform của chính Modal hoặc Card
        RectTransform modalRect = modal.GetComponent<RectTransform>();
        if (modalRect != null && modal.name != "Canvas (1)" && modal.name != "Canvas")
        {
            if (modal.Find("Card") == null)
            {
                modalRect.anchorMin = new Vector2(0.5f, 0.5f); modalRect.anchorMax = new Vector2(0.5f, 0.5f); modalRect.pivot = new Vector2(0.5f, 0.5f);
                modalRect.sizeDelta = new Vector2(880f, 1450f);
                modalRect.anchoredPosition = new Vector2(0f, 350f);
            }
        }

        Transform card = modal.Find("Card") ?? modal;
        if (card != modal)
        {
            RectTransform cRect = card.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f); cRect.anchorMax = new Vector2(0.5f, 0.5f); cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(880f, 1450f);
            cRect.anchoredPosition = new Vector2(0f, 350f);

            Image cardImg = card.GetComponent<Image>();
            if (cardImg != null)
            {
                Sprite cleanSp = GetCleanWoodSprite();
                if (cleanSp != null) { cardImg.sprite = cleanSp; cardImg.color = Color.white; }
                else { cardImg.sprite = null; cardImg.color = new Color(0.18f, 0.12f, 0.07f, 0.96f); }
            }
        }

        Transform title = card.Find("TitleTextObject") ?? card.Find("Title") ?? modal.Find("TitleTextObject") ?? modal.Find("Title");
        if (title != null)
        {
            RectTransform tRect = title.GetComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 520f);
            tRect.sizeDelta = new Vector2(780f, 120f);
            TextMeshProUGUI txt = title.GetComponent<TextMeshProUGUI>();
            if (txt != null) { txt.fontSize = 54; txt.fontStyle = FontStyles.Bold; txt.alignment = TextAlignmentOptions.Center; }
        }

        Transform content = card.Find("CreditsContentTextObject") ?? card.Find("Content") ?? modal.Find("CreditsContentTextObject") ?? modal.Find("Content");
        if (content != null)
        {
            // TẮT HOÀN TOÀN IMAGE NỀN GỖ BỊ TRỔ CHỮ CREDITS TRÊN TEXT NỘI DUNG!
            Image contentImg = content.GetComponent<Image>();
            if (contentImg != null)
            {
                contentImg.enabled = false;
            }

            RectTransform mRect = content.GetComponent<RectTransform>();
            mRect.anchoredPosition = new Vector2(0f, 120f);
            mRect.sizeDelta = new Vector2(760f, 600f);
            TextMeshProUGUI txt = content.GetComponent<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.fontSize = 30;
                txt.alignment = TextAlignmentOptions.Center;
                creditsText = txt;
                if (txt.text.Contains("(Ban co the"))
                {
                    txt.text = "<b>IDLE TYCOON MINER TIKI EDITION</b>\n\n<b>Phat Trien:</b> Studio Team\n<b>Do Hoa UI/UX:</b> Tropical Tiki Art\n<b>Phien Ban:</b> v1.0.0";
                }
            }
        }

        // Tìm tất cả nút đóng có trong Modal
        Button[] modalButtons = modal.GetComponentsInChildren<Button>(true);
        foreach (Button b in modalButtons)
        {
            RectTransform bRect = b.GetComponent<RectTransform>();
            if (bRect != null)
            {
                bRect.anchoredPosition = new Vector2(0f, -480f);
                bRect.sizeDelta = new Vector2(560f, 160f); // ÉP SIÊU TO GẤP ĐÔI!
            }

            Image bImg = b.GetComponent<Image>();
            if (bImg != null)
            {
                Sprite cleanSprite = GetCleanWoodSprite();
                if (cleanSprite != null) { bImg.sprite = cleanSprite; bImg.color = Color.white; }
                else { bImg.sprite = null; bImg.color = new Color(0.8f, 0.2f, 0.1f, 1f); }
                bImg.preserveAspect = false;
            }

            // Xóa/ẩn các component Text cũ gây đè chữ
            Text[] legacyTexts = b.GetComponentsInChildren<Text>(true);
            foreach (Text lt in legacyTexts)
            {
                lt.text = "";
                lt.enabled = false;
            }

            TextMeshProUGUI txt = b.GetComponentInChildren<TextMeshProUGUI>();
            if (txt == null)
            {
                GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                tObj.transform.SetParent(b.transform, false);
                txt = tObj.GetComponent<TextMeshProUGUI>();
            }

            RectTransform tRect = txt.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;
            txt.text = (modal.name.Contains("Credit") || b.name.Contains("Credit")) ? "QUAY LAI" : "DONG";
            txt.fontSize = 44;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
        }
    }

    private void FormatSettingsModal(Transform modal)
    {
        RectTransform modalRect = modal.GetComponent<RectTransform>();
        if (modalRect != null && modal.name != "Canvas (1)" && modal.name != "Canvas")
        {
            if (modal.Find("Card") == null)
            {
                modalRect.anchorMin = new Vector2(0.5f, 0.5f); modalRect.anchorMax = new Vector2(0.5f, 0.5f); modalRect.pivot = new Vector2(0.5f, 0.5f);
                modalRect.sizeDelta = new Vector2(880f, 1450f);
                modalRect.anchoredPosition = new Vector2(0f, 350f); // DỊCH CAO LÊN TRÊN +350px!
            }
        }

        Transform card = modal.Find("Card") ?? modal;
        if (card != modal)
        {
            RectTransform cRect = card.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f); cRect.anchorMax = new Vector2(0.5f, 0.5f); cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(880f, 1450f);
            cRect.anchoredPosition = new Vector2(0f, 350f); // DỊCH CAO LÊN TRÊN +350px!
        }

        Transform title = card.Find("Title") ?? modal.Find("Title");
        if (title != null)
        {
            RectTransform tRect = title.GetComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 520f);
            tRect.sizeDelta = new Vector2(750f, 100f);
            TextMeshProUGUI txt = title.GetComponent<TextMeshProUGUI>();
            if (txt != null) { txt.fontSize = 42; }
        }

        Transform sfx = card.Find("SFXRow") ?? modal.Find("SFXRow");
        if (sfx != null)
        {
            RectTransform sRect = sfx.GetComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 220f);
            sRect.sizeDelta = new Vector2(740f, 130f);
            TextMeshProUGUI txt = sfx.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) { txt.fontSize = 30; }
        }

        Transform music = card.Find("MusicRow") ?? modal.Find("MusicRow");
        if (music != null)
        {
            RectTransform mRect = music.GetComponent<RectTransform>();
            mRect.anchoredPosition = new Vector2(0f, 0f);
            mRect.sizeDelta = new Vector2(740f, 130f);
            TextMeshProUGUI txt = music.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) { txt.fontSize = 30; }
        }

        Transform vib = card.Find("VibRow") ?? modal.Find("VibRow");
        if (vib != null)
        {
            RectTransform vRect = vib.GetComponent<RectTransform>();
            vRect.anchoredPosition = new Vector2(0f, -220f);
            vRect.sizeDelta = new Vector2(740f, 130f);
            TextMeshProUGUI txt = vib.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) { txt.fontSize = 30; }
        }

        Button[] modalButtons = modal.GetComponentsInChildren<Button>(true);
        foreach (Button b in modalButtons)
        {
            RectTransform bRect = b.GetComponent<RectTransform>();
            if (bRect != null)
            {
                bRect.anchoredPosition = new Vector2(0f, -480f);
                bRect.sizeDelta = new Vector2(560f, 160f); // ÉP SIÊU TO GẤP ĐÔI!
            }

            Image bImg = b.GetComponent<Image>();
            if (bImg != null)
            {
                Sprite cleanSprite = GetCleanWoodSprite();
                if (cleanSprite != null) { bImg.sprite = cleanSprite; bImg.color = Color.white; }
                else { bImg.sprite = null; bImg.color = new Color(0.8f, 0.2f, 0.1f, 1f); }
                bImg.preserveAspect = false;
            }

            // Xóa/ẩn các component Text cũ gây đè chữ
            Text[] legacyTexts = b.GetComponentsInChildren<Text>(true);
            foreach (Text lt in legacyTexts)
            {
                lt.text = "";
                lt.enabled = false;
            }

            TextMeshProUGUI txt = b.GetComponentInChildren<TextMeshProUGUI>();
            if (txt == null)
            {
                GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                tObj.transform.SetParent(b.transform, false);
                txt = tObj.GetComponent<TextMeshProUGUI>();
            }

            RectTransform tRect = txt.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;
            txt.text = (modal.name.Contains("Credit") || b.name.Contains("Credit")) ? "QUAY LAI" : "DONG";
            txt.fontSize = 44;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
        }
    }

    /// <summary>
    /// Tạo Popup Cài đặt động chuẩn màu sắc Tiki mượt mà
    /// </summary>
    private void CreateDynamicSettingsModal()
    {
        Transform canvasTr = GetMainUICanvasTransform();

        // 1. Root Modal (Full screen overlay chống click đè)
        GameObject modal = new GameObject("SettingsModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modal.transform.SetParent(canvasTr, false);
        modal.transform.SetAsLastSibling();

        RectTransform modalRect = modal.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero; modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero; modalRect.offsetMax = Vector2.zero;

        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

        // Nền đen mờ 85% phủ full màn hình và CHẶN MỌI CLICK CHUỘT
        Image bgImg = modal.GetComponent<Image>();
        bgImg.sprite = whiteSprite;
        bgImg.color = new Color(0f, 0f, 0f, 0.85f);
        bgImg.raycastTarget = true;

        // 2. Card bảng gỗ Cài đặt Tiki (880x1450 - Kéo cao hẳn lên trên phủ toàn bộ bảng gỗ)
        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(modal.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(880f, 1450f);
        cardRect.anchoredPosition = new Vector2(0f, 350f);

        Image cardImg = card.GetComponent<Image>();
        Sprite cleanWoodSprite = null;
#if UNITY_EDITOR
        cleanWoodSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/button_credits_clean_wood.png");
#endif
        cardImg.sprite = cleanWoodSprite != null ? cleanWoodSprite : whiteSprite;
        if (cleanWoodSprite == null) cardImg.color = new Color(0.20f, 0.13f, 0.08f, 0.98f);

        // Title CAI DAT GAME
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 520f);
        titleRect.sizeDelta = new Vector2(750f, 100f);

        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.text = "CAI DAT GAME";
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.fontSize = 42;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = new Color(1f, 0.85f, 0.3f);

        // 1. Dòng SFX Sound (Âm thanh hiệu ứng)
        GameObject sfxRow = new GameObject("SFXRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sfxRow.transform.SetParent(card.transform, false);
        RectTransform sfxRect = sfxRow.GetComponent<RectTransform>();
        sfxRect.anchoredPosition = new Vector2(0f, 220f);
        sfxRect.sizeDelta = new Vector2(740f, 130f);
        sfxRow.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.05f, 0.85f);

        GameObject sfxTxtObj = new GameObject("SFXLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        sfxTxtObj.transform.SetParent(sfxRow.transform, false);
        RectTransform sfxTxtRect = sfxTxtObj.GetComponent<RectTransform>();
        sfxTxtRect.anchoredPosition = new Vector2(-120f, 0f);
        sfxTxtRect.sizeDelta = new Vector2(400f, 80f);
        TextMeshProUGUI sfxTxt = sfxTxtObj.GetComponent<TextMeshProUGUI>();
        sfxTxt.text = "AM THANH (SFX)";
        sfxTxt.alignment = TextAlignmentOptions.Left;
        sfxTxt.fontSize = 30;
        sfxTxt.fontStyle = FontStyles.Bold;
        sfxTxt.color = Color.white;

        // 2. Dòng Music (Nhạc nền)
        GameObject musicRow = new GameObject("MusicRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        musicRow.transform.SetParent(card.transform, false);
        RectTransform musicRect = musicRow.GetComponent<RectTransform>();
        musicRect.anchoredPosition = new Vector2(0f, 0f);
        musicRect.sizeDelta = new Vector2(740f, 130f);
        musicRow.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.05f, 0.85f);

        GameObject musicTxtObj = new GameObject("MusicLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        musicTxtObj.transform.SetParent(musicRow.transform, false);
        RectTransform musicTxtRect = musicTxtObj.GetComponent<RectTransform>();
        musicTxtRect.anchoredPosition = new Vector2(-120f, 0f);
        musicTxtRect.sizeDelta = new Vector2(400f, 80f);
        TextMeshProUGUI musicTxt = musicTxtObj.GetComponent<TextMeshProUGUI>();
        musicTxt.text = "NHAC NEN (MUSIC)";
        musicTxt.alignment = TextAlignmentOptions.Left;
        musicTxt.fontSize = 30;
        musicTxt.fontStyle = FontStyles.Bold;
        musicTxt.color = Color.white;

        // 3. Dòng Rung (Vibration)
        GameObject vibRow = new GameObject("VibRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        vibRow.transform.SetParent(card.transform, false);
        RectTransform vibRect = vibRow.GetComponent<RectTransform>();
        vibRect.anchoredPosition = new Vector2(0f, -220f);
        vibRect.sizeDelta = new Vector2(740f, 130f);
        vibRow.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.05f, 0.85f);

        GameObject vibTxtObj = new GameObject("VibLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        vibTxtObj.transform.SetParent(vibRow.transform, false);
        RectTransform vibTxtRect = vibTxtObj.GetComponent<RectTransform>();
        vibTxtRect.anchoredPosition = new Vector2(-120f, 0f);
        vibTxtRect.sizeDelta = new Vector2(400f, 80f);
        TextMeshProUGUI vibTxt = vibTxtObj.GetComponent<TextMeshProUGUI>();
        vibTxt.text = "RUNG CAM UNG";
        vibTxt.alignment = TextAlignmentOptions.Left;
        vibTxt.fontSize = 30;
        vibTxt.fontStyle = FontStyles.Bold;
        vibTxt.color = Color.white;

        // Nút Đóng (CLOSE BUTTON - TO GẤP ĐÔI: 480x140)
        GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(card.transform, false);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchoredPosition = new Vector2(0f, -500f);
        closeRect.sizeDelta = new Vector2(480f, 140f);

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
        closeTxtRect.anchorMin = Vector2.zero; closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = Vector2.zero; closeTxtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeTxt = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTxt.text = "DONG";
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.fontSize = 38;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.color = Color.white;

        settingsModalObject = modal;
    }

    /// <summary>
    /// Tạo Popup Credit phủ FULL 100% từ trên nút PLAY xuống hết bảng gỗ Tiki!
    /// </summary>
    private void CreateDynamicCreditsModal()
    {
        Transform canvasTr = GetMainUICanvasTransform();

        // 1. Root Modal (Full screen overlay đen mờ chống click đè)
        GameObject modal = new GameObject("CreditsModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modal.transform.SetParent(canvasTr, false);
        modal.transform.SetAsLastSibling();

        RectTransform modalRect = modal.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero; modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero; modalRect.offsetMax = Vector2.zero;

        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

        // Nền đen mờ 85% phủ full màn hình và CHẶN MỌI CLICK CHUỘT
        Image bgImg = modal.GetComponent<Image>();
        bgImg.sprite = whiteSprite;
        bgImg.color = new Color(0f, 0f, 0f, 0.85f);
        bgImg.raycastTarget = true;

        // 2. Card Bảng Gỗ Credit (880x1450 - Kéo cao hẳn Y = +350 phủ kín 100% nút PLAY)
        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(modal.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(880f, 1450f);
        cardRect.anchoredPosition = new Vector2(0f, 350f);

        Image cardImg = card.GetComponent<Image>();
        Sprite cleanWoodSprite = GetCleanWoodSprite();
        if (cleanWoodSprite != null) { cardImg.sprite = cleanWoodSprite; cardImg.color = Color.white; }
        else { cardImg.sprite = null; cardImg.color = new Color(0.18f, 0.12f, 0.07f, 0.98f); }

        // Title THONG TIN TAC GIA (Ở vị trí cao đỉnh bảng gỗ)
        GameObject titleObj = new GameObject("TitleTextObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 520f);
        titleRect.sizeDelta = new Vector2(780f, 120f);

        TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
        titleTxt.text = "THONG TIN TAC GIA";
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.fontSize = 54;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = new Color(1f, 0.85f, 0.3f);

        // Object Text hiển thị nội dung Credit (Chữ lớn 30pt lọt lòng bảng gỗ rộng 760px)
        GameObject msgObj = new GameObject("CreditsContentTextObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        msgObj.transform.SetParent(card.transform, false);
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchoredPosition = new Vector2(0f, 120f);
        msgRect.sizeDelta = new Vector2(760f, 600f);

        creditsText = msgObj.GetComponent<TextMeshProUGUI>();
        creditsText.alignment = TextAlignmentOptions.Center;
        creditsText.fontSize = 30;
        creditsText.color = Color.white;
        creditsText.text = "<b>IDLE TYCOON MINER TIKI EDITION</b>\n\n<b>Phat Trien:</b> Studio Team\n<b>Do Hoa UI/UX:</b> Tropical Tiki Art\n<b>Phien Ban:</b> v1.0.0";

        // Nút QUAY LAI (KÍCH THƯỚC SIÊU TO GẤP ĐÔI: 560x160)
        GameObject closeObj = new GameObject("CloseCreditsBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(card.transform, false);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchoredPosition = new Vector2(0f, -480f);
        closeRect.sizeDelta = new Vector2(560f, 160f);

        Image closeImg = closeObj.GetComponent<Image>();
        Sprite closeBtnSprite = GetCleanWoodSprite();
        if (closeBtnSprite != null) { closeImg.sprite = closeBtnSprite; closeImg.color = Color.white; }
        else { closeImg.sprite = null; closeImg.color = new Color(0.8f, 0.2f, 0.1f, 1f); }
        closeImg.preserveAspect = false;

        Button closeBtn = closeObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(() => modal.SetActive(false));

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        RectTransform closeTxtRect = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRect.anchorMin = Vector2.zero; closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = Vector2.zero; closeTxtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeTxt = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTxt.text = "QUAY LAI";
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.fontSize = 44;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.color = Color.white;

        creditsModalObject = modal;
    }
}
