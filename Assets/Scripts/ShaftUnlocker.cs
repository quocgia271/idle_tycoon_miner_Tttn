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

    [Header("Unlock Dissolve Effect")]
    public DissolveEffect unlockDissolveEffect; // Hiệu ứng tan biến khi mở khóa
    [Tooltip("Các UI Canvas bị ẩn đi khi đang khóa (để tránh đè lên Sprite Renderer) và sẽ hiện lại khi mở khóa xong.")]
    public GameObject[] canvasElementsToHideWhileLocked;

    [Header("Round Sprites (Đổi ảnh khóa theo Round)")]
    public SpriteRenderer lockSpriteRenderer; // Kéo thả SpriteRenderer của cục đất khóa vào đây
    public Sprite[] roundLockSprites; // index 0 = round 1, index 1 = round 2...

    private bool isBuilding = false;
    private bool isRepairMode = false;
    private double repairCost = 0;
    private Tween loadingTween;
    private Vector3 originalPos; // Lưu vị trí gốc để rung mượt hơn

    private void Start()
    {
        // Tính toán giá tiền lần đầu
        UpdateUnlockCost();

        // Ẩn các thành phần Canvas UI nếu có cấu hình (tránh đè lên Sprite Renderer khóa)
        HideCanvasElementsInstantly();

        // Khởi tạo UI (Chỉ khi không phải chế độ sửa chữa)
        if (!isRepairMode)
        {
            if (timeText != null) timeText.text = FormatTime(buildTimeSeconds);
            
            // Tạm thời ẩn yêu cầu Level theo yêu cầu của Hào
            if (levelText != null) levelText.gameObject.SetActive(false); 
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
            Gamemanager.Instance.OnRoundChanged += OnRoundChanged;
            
            UpdateLockSprite(Gamemanager.Instance.CurrentRound);
        }
    }

    private void OnDestroy()
    {
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged -= OnCashChanged;
            Gamemanager.Instance.OnLevelChanged -= OnLevelChanged;
            Gamemanager.Instance.OnRoundChanged -= OnRoundChanged;
        }
    }

    private void OnCashChanged(double cash) => UpdateRequirementColors();
    private void OnLevelChanged(int level) => UpdateRequirementColors();
    private void OnRoundChanged(int round)
    {
        UpdateUnlockCost();
        UpdateLockSprite(round);
    }

    private void UpdateLockSprite(int round)
    {
        if (lockSpriteRenderer != null && roundLockSprites != null && roundLockSprites.Length > 0)
        {
            int index = round - 1;
            if (index >= 0 && index < roundLockSprites.Length)
            {
                lockSpriteRenderer.sprite = roundLockSprites[index];
            }
        }
    }

    public void UpdateUnlockCost()
    {
        MineShaft parentShaft = GetComponent<MineShaft>(); 
        if (parentShaft == null) parentShaft = GetComponentInParent<MineShaft>();

        if (parentShaft != null)
        {
            double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
            if (Gamemanager.Instance != null && Gamemanager.Instance.GlobalConfig != null)
            {
                var config = Gamemanager.Instance.GlobalConfig;
                requiredGold = config.ShaftUnlockBaseCost * System.Math.Pow(config.ShaftDepthMultiplier, parentShaft.ShaftIndex - 1) * roundMultiplier;
            }
            else
            {
                requiredGold = 50 * System.Math.Pow(15, parentShaft.ShaftIndex - 1) * roundMultiplier;
            }
            requiredLevel = parentShaft.ShaftIndex;
            
            if (!isRepairMode && costText != null)
            {
                costText.text = CurrencyFormatter.FormatMoney(requiredGold);
            }
            UpdateRequirementColors();
        }
    }

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

    public bool IsBuilding => isBuilding;
    public bool isPurchased = false;

    private void OnUnlockClicked()
    {
        if (isBuilding) return; // Đang xây rồi thì bỏ qua

        if (isRepairMode)
        {
            if (Gamemanager.Instance.DeductCash(repairCost))
            {
                isPurchased = true;
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

        // TẠM THỜI TẮT YÊU CẦU LEVEL THEO YÊU CẦU CỦA USER
        /*
        if (Gamemanager.Instance.PlayerLevel < requiredLevel)
        {
            Debug.Log($"<color=red>Chưa đủ Level! Yêu cầu Level {requiredLevel}.</color>");
            ShakeEffect();
            return;
        }
        */

        // Kiểm tra và trừ Vàng
        if (Gamemanager.Instance.DeductCash(requiredGold))
        {
            isPurchased = true;
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
            costText.color = Color.white; // Ép thành màu trắng
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
        // Chạy hiệu ứng tan biến nếu có
        if (unlockDissolveEffect != null)
        {
            Debug.Log("<color=cyan>Đang chạy hiệu ứng Dissolve Hide cho hầm mỏ...</color>");
            bool effectDone = false;
            unlockDissolveEffect.PlayEffect(() => { 
                effectDone = true; 
                Debug.Log("<color=cyan>Hiệu ứng Dissolve hoàn tất!</color>");
            });
            yield return new WaitUntil(() => effectDone);
        }
        // Nếu không có dissolve effect, dùng cách mờ dần cũ
        else if (lockImage != null)
        {
            Debug.LogWarning("<color=orange>Chưa gán Unlock Dissolve Effect! Đang dùng chế độ mờ dần của UI cũ.</color>");
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
        
        // Bật lại các Canvas UI của hầm mỏ và Fade In sau khi ổ khóa tan biến xong
        FadeCanvasElements(true);

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

        // Khôi phục lại trạng thái hiển thị của hiệu ứng Dissolve để đảm bảo khi sửa chữa sẽ có thể Dissolve Hide lại
        if (unlockDissolveEffect != null)
        {
            unlockDissolveEffect.ResetToVisible(true); // Truyền true để nó đổi sprite renderer thành màu đen mờ
        }

        // Khi hầm bị vỡ, ẩn từ từ các Canvas UI đằng sau đi
        FadeCanvasElements(false);

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

    private void HideCanvasElementsInstantly()
    {
        if (canvasElementsToHideWhileLocked == null) return;
        foreach (var obj in canvasElementsToHideWhileLocked)
        {
            if (obj != null) 
            {
                CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                if (cg == null) cg = obj.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                obj.SetActive(false);
            }
        }
    }

    private void FadeCanvasElements(bool show)
    {
        if (canvasElementsToHideWhileLocked == null) return;
        
        // Khi hiện lên (sau khi mở khóa) thì cho fade cực nhanh (0.25s) cho mượt và snappy
        // Khi ẩn đi (bị hỏng hầm) thì fade chậm lại theo hiệu ứng
        float fadeDuration = show ? 0.25f : ((unlockDissolveEffect != null) ? unlockDissolveEffect.dissolveDuration : 1f);

        foreach (var obj in canvasElementsToHideWhileLocked)
        {
            if (obj == null) continue;
            
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();
            
            cg.DOKill();
            
            if (show)
            {
                obj.SetActive(true);
                cg.alpha = 0f;
                cg.DOFade(1f, fadeDuration).SetEase(Ease.Linear);
            }
            else
            {
                cg.DOFade(0f, fadeDuration).SetEase(Ease.Linear).OnComplete(() => {
                    obj.SetActive(false);
                });
            }
        }
    }

    public void HideLockInstantly()
    {
        if (unlockDissolveEffect != null)
        {
            unlockDissolveEffect.HideInstantly();
        }
        else if (lockImage != null)
        {
            lockImage.gameObject.SetActive(false);
        }

        ShowCanvasElementsInstantly();
    }

    public void ShowCanvasElementsInstantly()
    {
        if (canvasElementsToHideWhileLocked == null) return;
        foreach (var obj in canvasElementsToHideWhileLocked)
        {
            if (obj != null) 
            {
                obj.SetActive(true);
                CanvasGroup cg = obj.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }
    }
}
