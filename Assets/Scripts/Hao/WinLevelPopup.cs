using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý Popup Qua Màn Mới / Chiến Thắng (Win Level Popup Manager)
/// Tự động hiển thị khi người chơi hoàn thành tất cả nhiệm vụ và bấm Qua Màn
/// </summary>
public class WinLevelPopup : MonoBehaviour
{
    public static WinLevelPopup Instance { get; private set; }

    [Header("UI References")]
    public GameObject winLevelPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI rewardText;
    public Button nextStageButton;

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

        // Đăng ký sự kiện ngay trong Awake để luôn luôn nhận tín hiệu khi Qua Màn!
        GameEvents.OnStagePassed += HandleStagePassed;
    }

    private void Start()
    {
        if (nextStageButton != null)
        {
            nextStageButton.onClick.AddListener(OnNextStageClicked);
        }

        // Đảm bảo mặc định TẮT Popup khi mở Game (Tránh đè UI)
        if (winLevelPanel != null) winLevelPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        GameEvents.OnStagePassed -= HandleStagePassed;
    }

    private void HandleStagePassed(int stagePassed)
    {
        ShowWinPopup(stagePassed);
    }

    public void ShowWinPopup(int newStage)
    {
        int passedStage = newStage - 1;
        Debug.Log($"<color=cyan>[WinLevelPopup] HIỂN THỊ POPUP CHIẾN THẮNG MÀN {passedStage} -> SANG MÀN {newStage}!</color>");

        if (winLevelPanel == null)
        {
            Transform p = transform.root.Find("Canvas (1)/WinLevelPanel");
            if (p == null) p = transform.root.Find("WinLevelPanel");
            if (p != null) winLevelPanel = p.gameObject;
        }

        if (winLevelPanel != null)
        {
            winLevelPanel.SetActive(true);
            winLevelPanel.transform.SetAsLastSibling();
        }
        else
        {
            CreateDynamicWinPopup(passedStage, newStage);
        }

        if (titleText != null) titleText.text = "QUA MAN THANH CONG!";
        if (stageNameText != null) stageNameText.text = $"CHUC MUNG BAN DA HOAN THANH MAN {passedStage}!";
        if (rewardText != null) rewardText.text = $"Mo Khoa Man {newStage} - Nhan Thuong Vang!";
    }

    public void OnNextStageClicked()
    {
        if (winLevelPanel != null) winLevelPanel.SetActive(false);

        // Làm mới giao diện HUD màn chơi mới
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

    private void CreateDynamicWinPopup(int passedStage, int newStage)
    {
        Transform canvasTr = GetMainUICanvasTransform();

        GameObject panel = new GameObject("WinLevelPanel", typeof(RectTransform), typeof(CanvasRenderer));
        panel.transform.SetParent(canvasTr, false);
        panel.transform.SetAsLastSibling();

        RectTransform pRect = panel.GetComponent<RectTransform>();
        pRect.anchorMin = Vector2.zero;
        pRect.anchorMax = Vector2.one;
        pRect.offsetMin = Vector2.zero;
        pRect.offsetMax = Vector2.zero;

        GameObject card = new GameObject("WinCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(panel.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(520f, 440f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImg = card.GetComponent<Image>();
        Sprite winSprite = null;
#if UNITY_EDITOR
        winSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/win_level.png");
#endif
        if (winSprite == null) winSprite = Resources.Load<Sprite>("UI/win_level");
        if (winSprite != null) cardImg.sprite = winSprite;
        else cardImg.color = new Color(0.15f, 0.35f, 0.15f, 0.98f);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 160f);
        titleRect.sizeDelta = new Vector2(600f, 60f);

        titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "QUA MAN THANH CONG!";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.9f, 0.2f);

        // Next Button
        GameObject btnObj = new GameObject("NextStageButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(card.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0f, -170f);
        btnRect.sizeDelta = new Vector2(260f, 70f);

        nextStageButton = btnObj.GetComponent<Button>();
        nextStageButton.onClick.AddListener(OnNextStageClicked);

        winLevelPanel = panel;
    }
}
