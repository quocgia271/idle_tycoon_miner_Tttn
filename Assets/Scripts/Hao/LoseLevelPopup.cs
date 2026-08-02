using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Popup Thất Bại / Game Over (Lose Level Popup Manager)
/// Tự động hiển thị khi đồng hồ đếm ngược hết giờ (stageTimeRemaining <= 0f)
/// </summary>
public class LoseLevelPopup : MonoBehaviour
{
    public static LoseLevelPopup Instance { get; private set; }

    [Header("UI References")]
    public GameObject loseLevelPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI failReasonText;
    public TextMeshProUGUI penaltyDetailsText;
    public Button retryButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Đăng ký sự kiện ngay trong Awake để luôn luôn nhận tín hiệu khi Hết Giờ / Thất Bại!
        GameEvents.OnCheckpointFailed += HandleCheckpointFailed;
    }

    private void Start()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        // Đảm bảo mặc định TẮT Popup khi mở Game (Tránh đè UI)
        if (loseLevelPanel != null) loseLevelPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        GameEvents.OnCheckpointFailed -= HandleCheckpointFailed;
    }

    private void HandleCheckpointFailed(int failCount)
    {
        ShowLosePopup(failCount);
    }

    public void ShowLosePopup(int failCount)
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 1;
        Debug.LogWarning($"<color=red>[LoseLevelPopup] HIỂN THỊ POPUP THẤT BẠI MÀN {stage}! Lần phạt: {failCount}/2</color>");

        // TỰ ĐỘNG TÌM LOSE LEVEL PANEL NẾU CHƯA KÉO GÁN
        if (loseLevelPanel == null)
        {
            Transform p = transform.root.Find("Canvas (1)/LoseLevelPanel");
            if (p == null) p = transform.root.Find("LoseLevelPanel");
            if (p == null) p = GameObject.Find("LoseLevelPanel")?.transform;
            if (p != null) loseLevelPanel = p.gameObject;
        }

        // TỰ ĐỘNG ẨN BẢNG NHIỆM VỤ NẰM Ở ĐẰNG SAU ĐỂ KHÔNG BỊ ĐÈ UI!
        if (StageHUDUI.Instance != null && StageHUDUI.Instance.hudPanelObject != null)
        {
            StageHUDUI.Instance.hudPanelObject.SetActive(false);
        }

        if (loseLevelPanel != null)
        {
            loseLevelPanel.SetActive(true);
            loseLevelPanel.transform.SetAsLastSibling(); // Đưa Popup Thua đè lên trên cùng
        }
        else
        {
            CreateDynamicLosePopup(failCount, stage);
        }

        if (titleText != null)
        {
            titleText.text = failCount == 1 ? "HET GIO MAN CHOI!" : "GAME OVER!";
        }

        if (failReasonText != null)
        {
            failReasonText.text = $"Thoi gian Man {stage} da het (00:00)!";
        }

        if (penaltyDetailsText != null)
        {
            if (failCount == 1)
            {
                penaltyDetailsText.text = "<b>• Tien trinh Ham Mo da duoc Reset lai.</b>\n<b>• So Vang cua ban duoc GIU NGUYEN 100%.</b>\n• Bo nhiem vu moi da duoc khoi tao.";
            }
            else
            {
                penaltyDetailsText.text = "<b>• Tien trinh Round da bi Reset lai tu dau.</b>\n<color=red><b>• Phat tru 30% so Vang tich luy.</b></color>";
            }
        }
    }

    public void OnRetryClicked()
    {
        if (loseLevelPanel != null) loseLevelPanel.SetActive(false);

        // Làm mới UI HUD sau khi bấm Thử Lại
        if (StageHUDUI.Instance != null)
        {
            StageHUDUI.Instance.RefreshUI();
        }
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

    private void CreateDynamicLosePopup(int failCount, int stage)
    {
        Transform canvasTr = GetMainUICanvasTransform();

        GameObject panel = new GameObject("LoseLevelPanel", typeof(RectTransform), typeof(CanvasRenderer));
        panel.transform.SetParent(canvasTr, false);
        panel.transform.SetAsLastSibling();

        RectTransform pRect = panel.GetComponent<RectTransform>();
        pRect.anchorMin = Vector2.zero;
        pRect.anchorMax = Vector2.one;
        pRect.offsetMin = Vector2.zero;
        pRect.offsetMax = Vector2.zero;

        GameObject card = new GameObject("LoseCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(panel.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(520f, 440f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImg = card.GetComponent<Image>();
        Sprite loseSprite = null;
#if UNITY_EDITOR
        loseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Lose_level.png");
#endif
        if (loseSprite == null) loseSprite = Resources.Load<Sprite>("UI/Lose_level");
        if (loseSprite != null) cardImg.sprite = loseSprite;
        else cardImg.color = new Color(0.25f, 0.12f, 0.12f, 0.98f);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 160f);
        titleRect.sizeDelta = new Vector2(600f, 60f);

        titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = failCount == 1 ? "HET GIO MAN CHOI!" : "GAME OVER!";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.red;

        // Retry Button
        GameObject btnObj = new GameObject("RetryButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(card.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0f, -170f);
        btnRect.sizeDelta = new Vector2(260f, 70f);

        retryButton = btnObj.GetComponent<Button>();
        retryButton.onClick.AddListener(OnRetryClicked);

        loseLevelPanel = panel;
    }
}
