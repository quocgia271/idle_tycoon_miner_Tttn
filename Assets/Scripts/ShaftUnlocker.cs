using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Thêm thư viện DOTween

public class ShaftUnlocker : MonoBehaviour
{
    [Header("Unlock Requirements")]
    public double requiredGold = 1000;
    public int requiredLevel = 2;
    public float buildTimeSeconds = 10f; // Thời gian xây dựng    [Header("UI References")]
    public Button lockButton;
    public Image lockImage; // Hình ổ khóa hoặc hình che phủ hầm
    public TextMeshProUGUI costText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI levelText;
    public GameObject coinIcon; // Thêm tham chiếu đến icon hình đồng xu

    [Header("VFX Settings")]
    public GameObject indicatorVFX; // VFX phát sáng dưới chân hầm

    [Header("Target Shaft")]
    public Transform shaftRoot; // Cục to nhất của hầm mỏ để rung lắc toàn bộ

    private bool isBuilding = false;
    private bool isRepairMode = false;
    private double repairCost = 0;
    private Tween loadingTween;
    private Vector3 originalPos; // Lưu vị trí gốc để rung mượt hơn

    private void Start()
    {
        // Khởi tạo UI (Chỉ khi không phải chế độ sửa chữa)
        if (!isRepairMode)
        {
            if (costText != null) costText.text = CurrencyFormatter.FormatMoney(requiredGold);
            if (timeText != null) timeText.text = FormatTime(buildTimeSeconds);
            if (levelText != null) levelText.text = $"Level: {requiredLevel}";
        }

        if (lockButton != null)
        {
            lockButton.onClick.AddListener(OnUnlockClicked);
        }

        if (shaftRoot != null)
        {
            originalPos = shaftRoot.localPosition;
        }

        // Bật VFX nếu đang ở chế độ sửa chữa (vì có thể Start chạy sau khi TriggerRepairMode gọi SetActive(true))
        if (indicatorVFX != null) indicatorVFX.SetActive(isRepairMode);

        // Lắng nghe sự kiện đổi tiền/level để cập nhật màu chữ
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged += OnCashChanged;
            Gamemanager.Instance.OnLevelChanged += OnLevelChanged;
            UpdateRequirementColors(); // Gọi 1 lần lúc đầu để set màu chuẩn
        }
    }

    private void OnDestroy()
    {
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged -= OnCashChanged;
            Gamemanager.Instance.OnLevelChanged -= OnLevelChanged;
        }
    }

    private void OnCashChanged(double cash) => UpdateRequirementColors();
    private void OnLevelChanged(int level) => UpdateRequirementColors();

    private void UpdateRequirementColors()
    {
        if (isBuilding || Gamemanager.Instance == null) return;

        if (costText != null)
        {
            if (isRepairMode)
                costText.color = Gamemanager.Instance.IdleCash >= repairCost ? Color.white : Color.red;
            else
                costText.color = Gamemanager.Instance.IdleCash >= requiredGold ? Color.white : Color.red;
        }

        if (levelText != null && !isRepairMode)
        {
            levelText.color = Gamemanager.Instance.PlayerLevel >= requiredLevel ? Color.white : Color.red;
        }
    }

    private void OnUnlockClicked()
    {
        if (isBuilding) return; // Đang xây rồi thì bỏ qua

        if (isRepairMode)
        {
            if (Gamemanager.Instance.DeductCash(repairCost))
            {
                Debug.Log("<color=green>Sửa chữa hầm thành công!</color>");
                isRepairMode = false;
                
                // Báo cho hầm mỏ biết là đã sửa xong
                MineShaft shaft = GetComponentInParent<MineShaft>();
                if (shaft != null) shaft.RepairShaft();

                StartCoroutine(FadeOutAndDisable());
            }
            else
            {
                Debug.Log("<color=red>Không đủ tiền sửa hầm!</color>");
                ShakeEffect();
            }
            return;
        }

        // Kiểm tra Level
        if (Gamemanager.Instance.PlayerLevel < requiredLevel)
        {
            Debug.Log($"<color=red>Chưa đủ Level! Yêu cầu Level {requiredLevel}.</color>");
            ShakeEffect();
            return;
        }

        // Kiểm tra và trừ Vàng
        if (Gamemanager.Instance.DeductCash(requiredGold))
        {
            Debug.Log("<color=green>Bắt đầu xây dựng hầm mỏ!</color>");
            StartCoroutine(BuildRoutine());
        }
        else
        {
            Debug.Log("<color=red>Không đủ Vàng để mở khóa!</color>");
            ShakeEffect();
        }
    }

    private void ShakeEffect()
    {
        Transform targetToShake = shaftRoot != null ? shaftRoot : transform;
        
        // Ngắt các hiệu ứng cũ để chống giật
        targetToShake.DOKill();
        
        // Reset về vị trí gốc trước khi rung để không bị trôi UI
        if (shaftRoot != null)
            targetToShake.localPosition = originalPos;

        // Giảm xuống mức cực nhỏ là 0.02f
        targetToShake.DOShakePosition(0.25f, new Vector3(0, 0.02f, 0), 15, 0f, false, true);
    }

    private IEnumerator BuildRoutine()
    {
        isBuilding = true;
        
        // Disable nút bấm khi đang xây
        if (lockButton != null) lockButton.interactable = false;

        // Tắt luôn hình đồng xu nếu có
        if (coinIcon != null) coinIcon.SetActive(false);

        // Tắt VFX báo hiệu đi vì đang tiến hành xây rồi
        if (indicatorVFX != null) indicatorVFX.SetActive(false);

        // Chỉ hiện chữ Đang xây dựng và Ẩn text level đi
        if (costText != null) 
        {
            costText.text = "Đang xây dựng..."; 
            // Tạo hiệu ứng chớp tắt mờ dần lặp lại liên tục (Loading effect)
            loadingTween = costText.DOFade(0.3f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        if (levelText != null) levelText.gameObject.SetActive(false); // Ẩn level đi

        float remainingTime = buildTimeSeconds;

        while (remainingTime > 0)
        {
            if (timeText != null)
            {
                timeText.text = FormatTime(remainingTime);
            }

            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        // Xây xong
        if (timeText != null) timeText.text = "00:00:00";

        // Dừng hiệu ứng loading
        if (loadingTween != null) loadingTween.Kill();

        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        // Mờ dần hình che phủ (giảm Alpha)
        if (lockImage != null)
        {
            float alpha = lockImage.color.a;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime * 2f; // Tốc độ mờ dần, có thể điều chỉnh
                Color c = lockImage.color;
                c.a = Mathf.Clamp01(alpha);
                lockImage.color = c;
                yield return null;
            }
        }
        
        isBuilding = false; // Reset lại biến để sau này nếu vỡ hầm còn bấm nút được
        
        if (indicatorVFX != null) indicatorVFX.SetActive(false); // Chắc chắn tắt VFX
        
        // Ẩn toàn bộ UI mở khóa (nút bấm, text...)
        gameObject.SetActive(false); 
    }

    public void TriggerRepairMode(double cost)
    {
        isRepairMode = true;
        isBuilding = false; // Chắc chắn rằng không bị kẹt trạng thái Đang xây dựng từ trước
        repairCost = cost;
        gameObject.SetActive(true);

        // Bật lại VFX báo hiệu hầm đang cần được sửa chữa
        if (indicatorVFX != null) indicatorVFX.SetActive(true);

        // Ẩn các Image icon phụ (như cuốc, xẻng trang trí...), giữ lại nền đen, nút bấm và icon tiền
        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            // Bỏ qua lớp nền che phủ
            if (lockImage != null && img.gameObject == lockImage.gameObject) continue;
            
            // Bỏ qua nút bấm và các thành phần con của nút bấm
            if (lockButton != null && img.transform.IsChildOf(lockButton.transform)) continue;
            
            // Bỏ qua icon đồng xu
            if (coinIcon != null && img.gameObject == coinIcon) continue;

            img.gameObject.SetActive(false);
        }

        if (lockButton != null) lockButton.interactable = true;
        if (coinIcon != null) coinIcon.SetActive(true);
        if (levelText != null) levelText.gameObject.SetActive(false);
        if (timeText != null) timeText.text = ""; // Ẩn time đi
        
        // Cài đặt độ mờ nền đen để không che mất khói lửa (độ mờ 60%)
        if (lockImage != null)
        {
            Color c = lockImage.color;
            c.a = 0.6f;
            lockImage.color = c;
        }

        if (costText != null)
        {
            costText.text = $"Hầm hư hỏng nặng\nSửa chữa: {CurrencyFormatter.FormatMoney(repairCost)}";
            costText.color = Color.white; // Sẽ được update lại màu ở UpdateRequirementColors
            if (loadingTween != null) loadingTween.Kill();
            
            // Fade in cho đẹp
            CanvasGroup cg = costText.GetComponent<CanvasGroup>();
            if (cg == null) cg = costText.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.5f);
        }

        UpdateRequirementColors();
    }

    // Hàm phụ trợ định dạng thời gian giây sang HH:MM:SS
    private string FormatTime(float timeInSeconds)
    {
        int hours = Mathf.FloorToInt(timeInSeconds / 3600f);
        int minutes = Mathf.FloorToInt((timeInSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);

        if (hours > 0)
            return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        else
            return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
