using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// StageHUDUI quan ly BẢNG HUD HIỂN THỊ: MÀN 1, 2, 3 + Đồng hồ đếm ngược BỰ Ở GIỮA (Không kèm chú thích rườm rà)
/// </summary>
public class StageHUDUI : MonoBehaviour
{
    public static StageHUDUI Instance { get; private set; }

    [Header("UI Text References")]
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI timeText; // Text hien thi dong ho BỰ o giua
    public TextMeshProUGUI missionTitleText;
    public TextMeshProUGUI missionListText;
    public TextMeshProUGUI stageGateCostText;

    [Header("UI Button References")]
    public Button passStageButton;
    public Button toggleHUDButton;

    [Header("UI Panel & Image References")]
    public GameObject hudPanelObject;   // Khung panel chính chứa Bảng Hub.png
    public Image panelImage;             // Gán ảnh Hub.png
    public Image toggleButtonImage;      // Gán ảnh Icon.png
    public Image passStageButtonImage;   // Gán ảnh Button_NextStage.png

    [Header("Dynamic Mission Items (Optional)")]
    public Transform missionContainer;  // Container chứa danh sách nhiệm vụ (Vertical Layout Group)
    public GameObject missionItemPrefab;// Prefab hàng nhiệm vụ (dùng Bar.png)

    [Header("HUD Panel State")]
    public bool isPanelOpen = true; // Auto Popup khi moi vao game

    [Header("Auto GUI Settings")]
    public bool useRuntimeGUIIfNoUI = true; // Bật OnGUI mặc định để hiển thị liền lúc chưa gắn Canvas

    private GUIStyle bigTimeStyle;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        isPanelOpen = true; // Auto Popup khi vừa vào game
        RegisterEvents();
        StartCoroutine(InitRefreshDelay());

        // 1. Auto Binding cho PassStageButton
        if (passStageButton == null && passStageButtonImage != null)
        {
            passStageButton = passStageButtonImage.GetComponent<Button>();
            if (passStageButton == null) passStageButton = passStageButtonImage.gameObject.AddComponent<Button>();
        }
        if (passStageButton != null)
        {
            passStageButton.onClick.RemoveListener(OnPassStageClicked);
            passStageButton.onClick.AddListener(OnPassStageClicked);
        }

        // 2. Auto Binding cho ToggleHUDButton (Icon.png)
        if (toggleHUDButton == null && toggleButtonImage != null)
        {
            toggleHUDButton = toggleButtonImage.GetComponent<Button>();
            if (toggleHUDButton == null) toggleHUDButton = toggleButtonImage.gameObject.AddComponent<Button>();
        }
        if (toggleHUDButton != null)
        {
            toggleHUDButton.onClick.RemoveListener(ToggleHUD);
            toggleHUDButton.onClick.AddListener(ToggleHUD);
        }
    }

    private System.Collections.IEnumerator InitRefreshDelay()
    {
        yield return null; // Chờ 1 khung hình cho MissionManager khởi tạo xong
        RefreshUI();
    }

    private void Update()
    {
        // ⏰ Tự động cập nhật đồng hồ đếm ngược mỗi giây liên tục
        if (timeText != null && StageManager.Instance != null)
        {
            timeText.text = StageManager.Instance.GetFormattedTimeRemaining();
        }
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }

    private void RegisterEvents()
    {
        GameEvents.OnStagePassed += HandleStagePassed;
        GameEvents.OnMissionProgressUpdated += HandleMissionProgress;
        GameEvents.OnMissionCompleted += HandleMissionCompleted;
        GameEvents.OnMoneyChanged += HandleMoneyChanged;
        GameEvents.OnShaftPurchased += HandleShaftPurchased;
        GameEvents.OnCheckpointFailed += HandleCheckpointFailed;
    }

    private void UnregisterEvents()
    {
        GameEvents.OnStagePassed -= HandleStagePassed;
        GameEvents.OnMissionProgressUpdated -= HandleMissionProgress;
        GameEvents.OnMissionCompleted -= HandleMissionCompleted;
        GameEvents.OnMoneyChanged -= HandleMoneyChanged;
        GameEvents.OnShaftPurchased -= HandleShaftPurchased;
        GameEvents.OnShaftUpgraded -= HandleShaftUpgraded;
        GameEvents.OnCheckpointFailed -= HandleCheckpointFailed;
    }

    private string customTitleNotice = ""; // Cờ lưu thông báo Game Over khi hết giờ

    private void HandleCheckpointFailed(int failCount)
    {
        // Ẩn Bảng Nhiệm Vụ MissionPanel để nhường chỗ cho Popup Lose Level hiển thị cực kỳ đẹp mắt
        if (hudPanelObject == null && panelImage != null) hudPanelObject = panelImage.gameObject;
        if (hudPanelObject == null)
        {
            Transform p = transform.Find("MissionPanel");
            if (p == null) p = transform.root.Find("Canvas (1)/MissionPanel");
            if (p == null) p = transform.root.Find("MissionPanel");
            if (p != null) hudPanelObject = p.gameObject;
        }

        if (hudPanelObject != null)
        {
            hudPanelObject.SetActive(false); // Ẩn MissionPanel chống đè UI!
        }

        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 1;

        if (failCount == 1)
        {
            customTitleNotice = $"HET GIO MAN {stage}! THAT BAI LAN 1/2";
        }
        else
        {
            customTitleNotice = $"GAME OVER! THAT BAI TOAN BO MAN {stage}";
        }

        Debug.LogWarning($"<color=red>[StageHUDUI] HET GIO! GAME OVER THAT BAI ROUND {stage}! Lan phat: {failCount}/2</color>");
        RefreshUI();
    }

    private void HandleStagePassed(int stage)
    {
        customTitleNotice = ""; // Reset thông báo khi qua màn thành công
        isPanelOpen = true; // Auto Popup lai khi sang Man moi
        RefreshUI();
    }

    private void HandleMissionProgress(string id, int cur, int target) => RefreshUI();
    private void HandleMissionCompleted(string id) => RefreshUI();
    private void HandleMoneyChanged(double cash) => RefreshUI();
    private void HandleShaftPurchased(int count) => RefreshUI();
    private void HandleShaftUpgraded(int lvl) => RefreshUI();

    [Header("Toggle Button Position Config")]
    public bool autoPositionToggleButton = true;
    public Vector2 toggleButtonOpenPos = new Vector2(-440f, 720f);   
    public Vector2 toggleButtonClosedPos = new Vector2(-440f, 720f); 

    private float lastToggleTime = 0f; // Chống kích hoạt 2 lần cùng lúc trong 1 cú click

    public void ToggleHUD()
    {
        if (Time.unscaledTime - lastToggleTime < 0.2f) return;
        lastToggleTime = Time.unscaledTime;

        isPanelOpen = !isPanelOpen;
        
        if (hudPanelObject == null && panelImage != null) hudPanelObject = panelImage.gameObject;
        if (hudPanelObject == null)
        {
            Transform p = transform.Find("MissionPanel");
            if (p == null) p = transform.root.Find("Canvas (1)/MissionPanel");
            if (p == null) p = transform.root.Find("MissionPanel");
            if (p != null) hudPanelObject = p.gameObject;
        }

        if (hudPanelObject != null)
        {
            hudPanelObject.SetActive(isPanelOpen);
        }

        UpdateToggleButtonPosition();
        RefreshUI();
    }

    private void UpdateToggleButtonPosition()
    {
        RectTransform rect = null;
        if (toggleHUDButton != null) rect = toggleHUDButton.GetComponent<RectTransform>();
        else if (toggleButtonImage != null) rect = toggleButtonImage.rectTransform;

        if (rect != null)
        {
            // ÉP CỐ ĐỊNH CHÍNH XÁC X = -415, Y = 777 THEO YÊU CẦU CỦA NGUỜI DÙNG
            rect.anchoredPosition = new Vector2(-415f, 777f);
        }
    }

    /// <summary>
    /// Cap nhat giao dien HUD
    /// </summary>
    public void RefreshUI()
    {
        if (hudPanelObject == null && panelImage != null) hudPanelObject = panelImage.gameObject;
        if (hudPanelObject == null)
        {
            Transform p = transform.Find("MissionPanel");
            if (p == null) p = transform.root.Find("Canvas (1)/MissionPanel");
            if (p == null) p = transform.root.Find("MissionPanel");
            if (p != null) hudPanelObject = p.gameObject;
        }

        if (hudPanelObject != null)
        {
            hudPanelObject.SetActive(isPanelOpen);
        }
        UpdateToggleButtonPosition();

        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 1;
        string timeStr = StageManager.Instance != null ? StageManager.Instance.GetFormattedTimeRemaining() : "03:00";

        // 1. Stage & Time Text (Thời gian BỰ ở giữa, KHÔNG CÓ CHÚ THÍCH)
        if (stageText != null) stageText.text = $"MÀN {currentStage} / 3";
        if (timeText != null) timeText.text = timeStr; // Chỉ hiển thị con số 03:00 bự

        // 2. Mission List (Hiển thị tiêu đề Game Over đỏ rực rỡ nếu có)
        if (missionTitleText != null)
        {
            missionTitleText.text = !string.IsNullOrEmpty(customTitleNotice) 
                ? customTitleNotice 
                : $"📋 NHIỆM VỤ MÀN {currentStage}:";
        }

        if (MissionManager.Instance != null)
        {
            var missions = MissionManager.Instance.GetMissionsForStage(currentStage);

            // Cập nhật dạng văn bản đơn giản (nếu dùng Text)
            if (missionListText != null)
            {
                string listStr = "";
                foreach (var m in missions)
                {
                    string statusIcon = m.isCompleted ? "<color=green>[HOÀN THÀNH]</color>" : $"[{m.currentAmount}/{m.targetAmount}]";
                    listStr += $"• {m.title} {statusIcon}\n";
                }
                missionListText.text = listStr;
            }

            // Sinh UI Prefab linh hoạt (nếu gán Container & Prefab)
            if (missionContainer != null && missionItemPrefab != null)
            {
                // Tự động chỉnh khoảng cách (Spacing = 10px) giữa 3 thanh Bar.png cho tách rời vừa vặn
                var layoutGroup = missionContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = missionContainer.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                }
                if (layoutGroup != null)
                {
                    layoutGroup.spacing = 15f; // Khoảng hở 15px chuẩn đẹp giữa các thanh nhiệm vụ bự
                    layoutGroup.childAlignment = TextAnchor.UpperCenter;
                    layoutGroup.childControlWidth = true;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childForceExpandWidth = true;
                    layoutGroup.childForceExpandHeight = false;
                }

                foreach (Transform child in missionContainer)
                {
                    Destroy(child.gameObject);
                }

                foreach (var m in missions)
                {
                    GameObject itemObj = Instantiate(missionItemPrefab, missionContainer);
                    itemObj.SetActive(true);
                    // CỐ ĐỊNH CHIỀU CAO TỪNG THANH GỖ BAR.PNG BỰ GẤP ĐÔI (140PX)
                    var layoutElem = itemObj.GetComponent<UnityEngine.UI.LayoutElement>();
                    if (layoutElem == null) layoutElem = itemObj.AddComponent<UnityEngine.UI.LayoutElement>();
                    layoutElem.minHeight = 140f;
                    layoutElem.preferredHeight = 140f;

                    MissionItemUI itemUI = itemObj.GetComponent<MissionItemUI>();
                    if (itemUI == null)
                    {
                        itemUI = itemObj.GetComponentInChildren<MissionItemUI>();
                    }
                    if (itemUI == null)
                    {
                        itemUI = itemObj.AddComponent<MissionItemUI>();
                    }

                    if (itemUI != null)
                    {
                        itemUI.Setup(m);
                    }
                }

                // Ép Unity UI tính toán lại khoảng cách giãn dòng tức thì
                Canvas.ForceUpdateCanvases();
                RectTransform containerRect = missionContainer as RectTransform;
                if (containerRect != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
                }
            }
        }

        // 3. Pass Stage Cost & Button
        if (StageManager.Instance != null)
        {
            double gateCost = StageManager.Instance.GetStageGateCost();
            bool canPass = StageManager.Instance.CanPassStage();

            if (stageGateCostText != null) 
            {
                stageGateCostText.text = $"SANG MÀN MỚI ({CurrencyFormatter.FormatMoney(gateCost)})";
            }
            
            // Auto find nếu chưa kéo trong Inspector
            if (passStageButton == null && hudPanelObject != null)
            {
                Transform btnTr = hudPanelObject.transform.Find("PassStageButton");
                if (btnTr == null) btnTr = hudPanelObject.transform.Find("Button_NextStage");
                if (btnTr != null) passStageButton = btnTr.GetComponent<Button>();
            }
            if (passStageButtonImage == null && passStageButton != null)
            {
                passStageButtonImage = passStageButton.GetComponent<Image>();
            }

            // Ép Nút luôn hiển thị Active (không bị tàng hình / biến mất)
            if (passStageButton != null)
            {
                passStageButton.gameObject.SetActive(true);
                passStageButton.interactable = canPass;

                // Set Disabled Color có Alpha = 1.0 để Unity Button không làm tàng hình nút
                ColorBlock colors = passStageButton.colors;
                colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 1.0f);
                passStageButton.colors = colors;
            }

            if (passStageButtonImage != null)
            {
                passStageButtonImage.gameObject.SetActive(true);
                // Nút hiển thị màu xám đục 100% khi chưa qua được màn, và sáng đẹp khi đã đủ điều kiện qua màn
                passStageButtonImage.color = canPass ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1.0f);
            }
        }
    }

    private void OnPassStageClicked()
    {
        if (StageManager.Instance != null && StageManager.Instance.TryPassStage())
        {
            Debug.Log("[StageHUDUI] CHÚC MỪNG! BẠN ĐÃ QUA MÀN!");
        }
    }

    // ================================================================
    // RUNTIME ONGUI BACKUP: Hien thi THỜI GIAN BỰ Ở GIỮA
    // ================================================================
    private void OnGUI()
    {
        // Tắt hoàn toàn OnGUI khi đã có UI Canvas (hudPanelObject / panelImage) hoặc useRuntimeGUIIfNoUI = false
        if (!useRuntimeGUIIfNoUI || hudPanelObject != null || panelImage != null) return;

        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 1;
        string timeStr = StageManager.Instance != null ? StageManager.Instance.GetFormattedTimeRemaining() : "03:00";
        double gateCost = StageManager.Instance != null ? StageManager.Instance.GetStageGateCost() : 500;
        bool canPassStage = StageManager.Instance != null && StageManager.Instance.CanPassStage();

        // 1. Khoi tao Style cho Dong ho Dem nguoc BỰ o giua
        if (bigTimeStyle == null)
        {
            bigTimeStyle = new GUIStyle(GUI.skin.label);
            bigTimeStyle.fontSize = 26;
            bigTimeStyle.fontStyle = FontStyle.Bold;
            bigTimeStyle.alignment = TextAnchor.MiddleCenter;
            bigTimeStyle.normal.textColor = new Color(1f, 0.85f, 0.2f); // Vang sang
        }

        // 2. NÚT BẬT / TẮT HUD Ở GÓC TRÊN MÀN HÌNH
        string toggleLabel = isPanelOpen ? $"📋 MÀN {stage} ({timeStr}) [▲ ẨN HUD]" : $"📋 MÀN {stage} ({timeStr}) [▼ HIỆN NHIỆM VỤ]";
        if (GUI.Button(new Rect(10, 10, 260, 32), toggleLabel))
        {
            ToggleHUD();
        }

        // NẾU HUD ĐANG MỞ THÌ MỚI VẼ BẢNG CHI TIẾT
        if (isPanelOpen)
        {
            GUI.Box(new Rect(10, 47, 360, 220), $"=== MÀN {stage} ===");

            // HIỂN THỊ ĐỒNG HỒ ĐẾM NGƯỢC BỰ Ở GIỮA (KHÔNG CÓ DÒNG CHÚ THÍCH)
            GUI.Label(new Rect(20, 68, 340, 36), timeStr, bigTimeStyle);

            // Hien thi Danh sach Nhiem vu Man
            if (MissionManager.Instance != null)
            {
                var missions = MissionManager.Instance.GetMissionsForStage(stage);
                int yOffset = 108;
                foreach (var m in missions)
                {
                    string check = m.isCompleted ? "<color=green>[HOÀN THÀNH]</color>" : $"[{m.currentAmount}/{m.targetAmount}]";
                    GUI.Label(new Rect(30, yOffset, 330, 20), $"• {m.title} {check}");
                    yOffset += 20;
                }
            }

            // NÚT SANG MÀN MỚI
            GUI.enabled = canPassStage;
            if (GUI.Button(new Rect(20, 195, 330, 35), $"🚪 SANG MÀN MỚI ({CurrencyFormatter.FormatMoney(gateCost)})"))
            {
                OnPassStageClicked();
            }

            GUI.enabled = true;
        }
    }
}
