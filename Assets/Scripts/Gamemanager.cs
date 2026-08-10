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
    public bool isBroken { get; private set; } // Trạng thái hầm mỏ

    // AI Check đã được tách sang file DeadGameChecker.cs để tuân thủ SOLID

    [Header("UI References")]
    public GameObject transitionPrefab;

    [Header("Game Progression")]
    public GlobalGameConfigSO GlobalConfig;
    public double PrestigeMultiplier = 1.0; // Hệ số nhân tiền khi chuyển sinh
    public int CurrentRound = 1; // Vòng chơi hiện tại (1, 2, 3)
    public double RoundMultiplier => Math.Pow(GlobalConfig != null ? GlobalConfig.RoundDifficultyMultiplier : 1000000, CurrentRound - 1); // Hệ số Dịch chuyển theo vòng

    public Action<double> OnCashChanged;
    public Action<int> OnLevelChanged; // Event khi level thay đổi
    public Action<int> OnRoundChanged; // Event khi qua màn (Round) hoặc nạp Save

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
        // Đăng ký sự kiện nạp game từ SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnGameLoaded += HandleGameLoaded;
            
            // Nếu SaveManager đã load xong trước cả khi GameManager Start, gọi luôn
            if (SaveManager.Instance.CurrentSaveData != null)
            {
                HandleGameLoaded();
            }
        }
        else
        {
            InitializeNewRoundCash();
        }
    }

    private void HandleGameLoaded()
    {
        var data = SaveManager.Instance.CurrentSaveData;
        
        // Chỉ nạp dữ liệu tiền nếu đây không phải là một file Save trống mới tạo
        if (data != null && data.LastSaveTimeUnixSeconds > 0)
        {
            IdleCash = data.IdleCash;
            LifetimeCash = data.LifetimeCash;
            PlayerLevel = data.PlayerLevel == 0 ? 1 : data.PlayerLevel;
            PrestigeMultiplier = data.PrestigeMultiplier < 1.0 ? 1.0 : data.PrestigeMultiplier;
            CurrentRound = data.CurrentRound == 0 ? 1 : data.CurrentRound;
            
            OnCashChanged?.Invoke(IdleCash);
            OnLevelChanged?.Invoke(PlayerLevel);
            OnRoundChanged?.Invoke(CurrentRound);
            Debug.Log("[GameManager] Loaded state from SaveManager.");
        }
        else
        {
            // Nếu không có Save hoặc Save trống, cấp tiền khởi nghiệp
            InitializeNewRoundCash();
        }
    }

    private void InitializeNewRoundCash()
    {
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
        bool isDeadGame = DeadGameChecker.Instance != null && DeadGameChecker.Instance.IsDeadGame;
        
        if (LifetimeCash < baseRequirement && !isDeadGame)
        {
            Debug.Log($"<color=red>Chưa đủ điều kiện chuyển sinh! (Cần kiếm tổng cộng {CurrencyFormatter.FormatMoney(baseRequirement)})</color>");
            return;
        }

        if (isDeadGame)
        {
            Debug.Log("<color=orange>DEAD GAME DETECTED! Hệ thống cho phép Đặc cách Chuyển sinh sớm (Fail-Forward)!</color>");
        }

        PrestigeMultiplier = CalculateNextPrestigeMultiplier();
        
        // Cấp vốn khởi nghiệp Vàng (Giữ nguyên LifetimeCash để cộng dồn cho lần chuyển sinh sau)
        IdleCash = 150 * RoundMultiplier;
        OnCashChanged?.Invoke(IdleCash);
        
        // Cực kỳ quan trọng: Reset toàn bộ Quản lý khi Chuyển sinh
        if (ManagerController.Instance != null)
        {
            ManagerController.Instance.ResetManagers();
        }
        
        if (SaveManager.Instance != null)
        {
            SaveManager.isTransitioning = true;
            SaveManager.Instance.SaveResetState(); // Ghi file ngay lập tức
        }
        
        StartCoroutine(PrestigeTransitionRoutine());
    }

    private IEnumerator PrestigeTransitionRoutine()
    {
        // Đánh dấu để MainMenuController không tự động bật Menu hoặc Fake Loading lên
        MainMenuController.skipMenuInstantly = true;

        // Load lại cảnh hiện tại đồng bộ
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        // Chờ 1.5 giây để toàn bộ scene mới, các script Start/Awake chạy xong hoàn toàn
        yield return new WaitForSeconds(1.5f);
        
        if (SaveManager.Instance != null)
        {
            SaveManager.isTransitioning = false;
        }
        
        Debug.Log($"<color=green>Đã Chuyển sinh! Hệ số tiền thưởng mới: x{PrestigeMultiplier}</color>");
    }

    [ContextMenu("Qua Màn (Next Round)")]
    public void ProceedToNextRound()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.isTransitioning = true; // Khóa Save NGAY LẬP TỨC để chống lỗi tắt cái rụp
        }
        StartCoroutine(TransitionToNextRoundRoutine());
    }

    private IEnumerator TransitionToNextRoundRoutine()
    {
        if (transitionPrefab == null)
        {
            Debug.LogError("Transition Prefab is missing! Please assign it in the GameManager Inspector.");
            yield break;
        }

        // Tạo màn hình đen từ Prefab
        GameObject fadeObj = Instantiate(transitionPrefab);
        DontDestroyOnLoad(fadeObj);

        Image fadeImage = fadeObj.GetComponentInChildren<Image>();
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;
        }

        // ==========================================
        // 1. TÍNH TOÁN VÀ LƯU DATA NGAY LẬP TỨC 
        // ==========================================
        // Tính trước các thông số của Round mới
        CurrentRound++;
        IdleCash = 150 * RoundMultiplier;
        LifetimeCash = 0; 
        PrestigeMultiplier = 1.0; 
        
        // Tạo 1 file save sạch tinh ngay lập tức (Ghi đè file cũ)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveResetState(); 
        }
        // Kể từ khoảnh khắc này, nếu người chơi có tắt app thì dữ liệu trong máy ĐÃ LÀ ROUND MỚI!

        // ==========================================
        // 2. HIỆU ỨNG HÌNH ẢNH (FADE OUT)
        // ==========================================
        Tween fadeOut = fadeImage.DOFade(1f, 1f);
        yield return fadeOut.WaitForCompletion();

        // 3. Sau khi màn hình đã đen, mới bắt đầu cập nhật UI và reset Manager
        OnCashChanged?.Invoke(IdleCash);
        OnRoundChanged?.Invoke(CurrentRound);
        
        if (ManagerController.Instance != null)
        {
            ManagerController.Instance.ResetManagers();
        }

        // ==========================================
        // 4. CHUYỂN SCENE
        // ==========================================
        MainMenuController.skipMenuInstantly = true; // Bỏ qua Menu và Fake Loading
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
        Destroy(fadeObj);

        // MỞ KHÓA SAVE LẠI SAU KHI NGƯỜI CHƠI THỰC SỰ BẮT ĐẦU CHƠI
        if (SaveManager.Instance != null)
        {
            SaveManager.isTransitioning = false;
        }
    }

    public void AddCash(double amount)
    {
        IdleCash += amount;
        LifetimeCash += amount; // Ghi nhận vào tổng tiền để tính Prestige
        OnCashChanged?.Invoke(IdleCash);
        
        if (SaveManager.Instance != null) SaveManager.Instance.MarkAsDirty();
    }

    public void AddLevel(int amount)
    {
        PlayerLevel += amount;
        OnLevelChanged?.Invoke(PlayerLevel);
        
        if (SaveManager.Instance != null) SaveManager.Instance.MarkAsDirty();
    }

    // Hàm trừ tiền an toàn: Trả về true nếu đủ tiền và mua thành công
    public bool DeductCash(double amount)
    {
        if (IdleCash >= amount)
        {
            IdleCash -= amount;
            OnCashChanged?.Invoke(IdleCash);
            if (SaveManager.Instance != null) SaveManager.Instance.MarkAsDirty();
            return true;
        }
        return false;
    }
}
