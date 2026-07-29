using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class Gamemanager : MonoBehaviour
{
    public static Gamemanager Instance;
    public double IdleCash = 0;
    public double LifetimeCash = 0; // Tổng tiền đã kiếm được trong vòng chơi hiện tại
    public int PlayerLevel = 1; // Thêm thông số level cho người chơi
    
    [Header("Game Progression")]
    public double PrestigeMultiplier = 1.0; // Hệ số nhân tiền khi chuyển sinh
    public int CurrentRound = 1; // Vòng chơi hiện tại (1, 2, 3)
    public double RoundMultiplier => Math.Pow(1000000, CurrentRound - 1); // Hệ số Dịch chuyển theo vòng

    public Action<double> OnCashChanged;
    public Action<int> OnLevelChanged; // Event khi level thay đổi

    [Header("Admin / Testing")]
    public double TestCashAmount = 1000000;

    [ContextMenu("Add Test Cash")]
    public void AddTestCash()
    {
        AddCash(TestCashAmount);
        Debug.Log($"[Admin] Added {TestCashAmount} cash. Current cash: {IdleCash}");
    }

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ GameManager khi load lại scene để không mất Round và Multiplier
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Cấp vốn khởi nghiệp Vàng (Golden Starting Cash) ngay khi game mới bắt đầu
        if (IdleCash == 0)
        {
            IdleCash = 150 * RoundMultiplier;
            OnCashChanged?.Invoke(IdleCash);
        }
    }

    public double CalculateNextPrestigeMultiplier()
    {
        double baseRequirement = 1000000 * RoundMultiplier;

        // Yêu cầu kiếm được ít nhất 1 Triệu (nhân với RoundMultiplier) mới có thể chuyển sinh
        if (LifetimeCash < baseRequirement) return PrestigeMultiplier;
        
        // Công thức: 1.0 + căn bậc hai của (Tổng tiền / baseRequirement)
        double newMultiplier = 1.0 + Math.Pow(LifetimeCash / baseRequirement, 0.5);
        
        // Không cho phép hệ số bị giảm
        return Math.Max(PrestigeMultiplier, newMultiplier);
    }

    [ContextMenu("Prestige (Chuyển Sinh)")]
    public void Prestige()
    {
        double baseRequirement = 1000000 * RoundMultiplier;
        if (LifetimeCash < baseRequirement)
        {
            Debug.Log($"<color=red>Chưa đủ điều kiện chuyển sinh! (Cần kiếm tổng cộng {CurrencyFormatter.FormatMoney(baseRequirement)})</color>");
            return;
        }

        PrestigeMultiplier = CalculateNextPrestigeMultiplier();
        
        // Cấp vốn khởi nghiệp Vàng (Giữ nguyên LifetimeCash để cộng dồn cho lần chuyển sinh sau)
        IdleCash = 150 * RoundMultiplier;
        OnCashChanged?.Invoke(IdleCash);
        
        // Cực kỳ quan trọng: Reset toàn bộ Quản lý khi Chuyển sinh (Theo chuẩn thiết kế Idle Game)
        if (ManagerController.Instance != null)
        {
            ManagerController.Instance.ResetManagers();
        }
        
        // Load lại cảnh hiện tại (giữ nguyên CurrentRound)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        Debug.Log($"<color=green>Đã Chuyển sinh! Hệ số tiền thưởng mới: x{PrestigeMultiplier}</color>");
    }

    [ContextMenu("Qua Màn (Next Round)")]
    public void ProceedToNextRound()
    {
        StartCoroutine(TransitionToNextRoundRoutine());
    }

    private IEnumerator TransitionToNextRoundRoutine()
    {
        // Tạo màn hình đen để fade
        Canvas fadeCanvas = new GameObject("FadeCanvas").AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;
        
        Image fadeImage = new GameObject("FadeImage").AddComponent<Image>();
        fadeImage.transform.SetParent(fadeCanvas.transform, false);
        fadeImage.rectTransform.anchorMin = Vector2.zero;
        fadeImage.rectTransform.anchorMax = Vector2.one;
        fadeImage.rectTransform.offsetMin = Vector2.zero;
        fadeImage.rectTransform.offsetMax = Vector2.zero;
        fadeImage.color = new Color(0, 0, 0, 0);

        // Giữ Canvas không bị hủy khi load scene
        DontDestroyOnLoad(fadeCanvas.gameObject);

        // Fade Out (Màn hình tối dần)
        Tween fadeOut = fadeImage.DOFade(1f, 1f);
        yield return fadeOut.WaitForCompletion();

        // Tăng Round và Reset với vốn khởi nghiệp
        CurrentRound++;
        IdleCash = 150 * RoundMultiplier;
        LifetimeCash = 0; 
        PrestigeMultiplier = 1.0; 
        OnCashChanged?.Invoke(IdleCash);
        
        // Cực kỳ quan trọng: Xóa toàn bộ Quản lý cũ khi qua màn mới (Tránh lỗi State Desync)
        if (ManagerController.Instance != null)
        {
            ManagerController.Instance.ResetManagers();
        }
        
        // Load Scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log($"<color=cyan>Đã qua Round {CurrentRound}! Chúc may mắn với thử thách mới!</color>");

        // Đợi 0.5s để người chơi chuẩn bị
        yield return new WaitForSeconds(0.5f);

        // Fade In (Màn hình sáng dần)
        Tween fadeIn = fadeImage.DOFade(0f, 1f);
        yield return fadeIn.WaitForCompletion();

        // Hủy Canvas sau khi Fade xong
        Destroy(fadeCanvas.gameObject);
    }

    public void AddCash(double amount)
    {
        IdleCash += amount;
        LifetimeCash += amount; // Ghi nhận vào tổng tiền để tính Prestige
        OnCashChanged?.Invoke(IdleCash);
    }

    public void AddLevel(int amount)
    {
        PlayerLevel += amount;
        OnLevelChanged?.Invoke(PlayerLevel);
    }

    // Hàm trừ tiền an toàn: Trả về true nếu đủ tiền và mua thành công
    public bool DeductCash(double amount)
    {
        if (IdleCash >= amount)
        {
            IdleCash -= amount;
            OnCashChanged?.Invoke(IdleCash);
            return true;
        }
        return false;
    }
}
