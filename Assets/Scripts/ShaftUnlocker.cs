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

    [Header("Target Shaft")]
    public Transform shaftRoot; // Cục to nhất của hầm mỏ để rung lắc toàn bộ

    private bool isBuilding = false;
    private Tween loadingTween;
    private Vector3 originalPos; // Lưu vị trí gốc để rung mượt hơn

    private void Start()
    {
        // Khởi tạo UI
        if (costText != null) costText.text = CurrencyFormatter.FormatMoney(requiredGold);
        if (timeText != null) timeText.text = FormatTime(buildTimeSeconds);
        if (levelText != null) levelText.text = $"Level: {requiredLevel}";

        if (lockButton != null)
        {
            lockButton.onClick.AddListener(OnUnlockClicked);
        }

        if (shaftRoot != null)
        {
            originalPos = shaftRoot.localPosition;
        }

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
            costText.color = Gamemanager.Instance.IdleCash >= requiredGold ? Color.white : Color.red;
        }

        if (levelText != null)
        {
            levelText.color = Gamemanager.Instance.PlayerLevel >= requiredLevel ? Color.white : Color.red;
        }
    }

    private void OnUnlockClicked()
    {
        if (isBuilding) return; // Đang xây rồi thì bỏ qua

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

        // Mờ dần hình che phủ (giảm Alpha)
        if (lockImage != null)
        {
            float alpha = lockImage.color.a;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime; // Tốc độ mờ dần, có thể điều chỉnh
                Color c = lockImage.color;
                c.a = Mathf.Clamp01(alpha);
                lockImage.color = c;
                yield return null;
            }
        }

        // Dừng hiệu ứng loading
        if (loadingTween != null) loadingTween.Kill();

        // Ẩn toàn bộ UI mở khóa (nút bấm, text...)
        gameObject.SetActive(false); 
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
