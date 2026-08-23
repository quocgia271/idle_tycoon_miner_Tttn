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

    [Header("Economy Configuration")]
    public EconomyConfig economyConfig;

    [Header("UI References")]
    public GameObject transitionPrefab;

    [Header("Transition Settings")]
    public float transitionFadeOutTime = 1.0f;
    public float transitionWaitTime = 0.5f;
    public float transitionFadeInTime = 1.0f;

    [Header("Game Progression")]
    public GlobalGameConfigSO GlobalConfig;
    public double PrestigeMultiplier = 1.0; // Hệ số nhân tiền khi chuyển sinh
    public int CurrentRound = 1; // Vòng chơi hiện tại (1, 2, 3)
    public double RoundMultiplier => MathHelper.CalculateRoundMultiplier(GlobalConfig != null ? GlobalConfig.RoundDifficultyMultiplier : 1000000, CurrentRound); // Hệ số Dịch chuyển theo vòng

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
            double startingCash = (GlobalConfig != null ? GlobalConfig.InitialStartingCash : 150) * RoundMultiplier;
            IdleCash = startingCash;
            OnCashChanged?.Invoke(IdleCash);
        }
    }

    public double CalculateNextPrestigeMultiplier()
    {
        double baseRequirement = (GlobalConfig != null ? GlobalConfig.BasePrestigeRequirement : 1000000) * RoundMultiplier;

        // Yêu cầu kiếm đủ baseRequirement mới có thể chuyển sinh
        if (LifetimeCash < baseRequirement) return PrestigeMultiplier;
        
        // Công thức: 1.0 + căn bậc hai của (Tổng tiền / baseRequirement)
        double newMultiplier = MathHelper.CalculatePrestigeMultiplier(LifetimeCash, baseRequirement);
        
        // Không cho phép hệ số bị giảm
        return Math.Max(PrestigeMultiplier, newMultiplier);
    }

    [ContextMenu("Prestige (Chuyển Sinh)")]
    public void Prestige()
    {
        double baseRequirement = (GlobalConfig != null ? GlobalConfig.BasePrestigeRequirement : 1000000) * RoundMultiplier;
        bool isDeadGame = DeadGameChecker.Instance != null && DeadGameChecker.Instance.IsDeadGame;
        
        if (LifetimeCash < baseRequirement && !isDeadGame)
        {
            Debug.Log($"<color=red>Chưa đủ điều kiện chuyển sinh! (Cần kiếm tổng cộng {CurrencyFormatter.FormatMoney(baseRequirement)})</color>");
            return;
        }

        double nextMultiplier = CalculateNextPrestigeMultiplier();
        if (nextMultiplier <= PrestigeMultiplier && !isDeadGame)
        {
            Debug.Log($"<color=red>Chưa đủ điều kiện chuyển sinh! (Cần kiếm thêm tiền để tăng hệ số, hệ số mới phải lớn hơn {PrestigeMultiplier:F2}x)</color>");
            return;
        }

        if (isDeadGame)
        {
            Debug.Log("<color=orange>DEAD GAME DETECTED! Hệ thống cho phép Đặc cách Chuyển sinh sớm (Fail-Forward)!</color>");
        }

        PrestigeMultiplier = CalculateNextPrestigeMultiplier();
        
        // Cấp vốn khởi nghiệp Vàng (Giữ nguyên LifetimeCash để cộng dồn cho lần chuyển sinh sau)
        double startingCash = (GlobalConfig != null ? GlobalConfig.InitialStartingCash : 150) * RoundMultiplier;
        IdleCash = startingCash;
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
        yield return PlayFakeLoadingTransition(() => {
            MainMenuController.skipMenuInstantly = true;
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            return asyncLoad;
        });

        if (SaveManager.Instance != null) SaveManager.isTransitioning = false;
        Debug.Log($"<color=green>Đã Chuyển sinh! Hệ số tiền thưởng mới: x{PrestigeMultiplier}</color>");
    }

    [ContextMenu("Qua Màn (Next Round)")]
    public void ProceedToNextRound()
    {
        if (SaveManager.Instance != null) SaveManager.isTransitioning = true;
        StartCoroutine(TransitionToNextRoundRoutine());
    }

    private IEnumerator TransitionToNextRoundRoutine()
    {
        yield return PlayFakeLoadingTransition(() => {
            CurrentRound++;
            double startingCash = (GlobalConfig != null ? GlobalConfig.InitialStartingCash : 150) * RoundMultiplier;
            IdleCash = startingCash;
            LifetimeCash = 0; 
            PrestigeMultiplier = 1.0; 
            
            if (SaveManager.Instance != null) SaveManager.Instance.SaveResetState(); 
            
            MainMenuController.skipMenuInstantly = true; 
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            return asyncLoad;
        });

        OnCashChanged?.Invoke(IdleCash);
        OnRoundChanged?.Invoke(CurrentRound);
        if (ManagerController.Instance != null) ManagerController.Instance.ResetManagers();

        if (SaveManager.Instance != null) SaveManager.isTransitioning = false;
        Debug.Log($"<color=cyan>Đã qua Round {CurrentRound}! Chúc may mắn với thử thách mới!</color>");
    }

    private IEnumerator PlayFakeLoadingTransition(Func<AsyncOperation> onMidpoint)
    {
        GameObject fadeObj = new GameObject("TransitionCanvas");
        DontDestroyOnLoad(fadeObj);

        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 9999;
        canvas.sortingLayerName = "Camera";
        
        UnityEngine.UI.CanvasScaler scaler = fadeObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(540, 960);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeObj.transform, false);
        Image fadeImage = imageObj.AddComponent<Image>();
        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        // Dùng tọa độ chuẩn của CanvasScaler (540x960) nên 1500f là chắc chắn phủ kín mọi góc
        rt.sizeDelta = new Vector2(1500f, 1500f);
        rt.anchoredPosition = Vector2.zero;
        fadeImage.color = Color.black; 

        bool useShader = false;
        Shader circleShader = Shader.Find("UI/CircleReveal");
        if (circleShader != null)
        {
            useShader = true;
            Material mat = new Material(circleShader);
            mat.SetFloat("_Radius", 1.5f);
            fadeImage.material = mat;
            Tween fadeOut = fadeImage.material.DOFloat(0f, "_Radius", transitionFadeOutTime).SetEase(Ease.InOutSine).SetUpdate(true);
            yield return fadeOut.WaitForCompletion();
        }
        else
        {
            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;
            Tween fadeOut = fadeImage.DOFade(1f, transitionFadeOutTime).SetUpdate(true);
            yield return fadeOut.WaitForCompletion();
        }

AsyncOperation asyncLoad = onMidpoint?.Invoke();
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone) yield return null;
        }

        if (canvas != null) canvas.worldCamera = Camera.main;

        yield return new WaitForSecondsRealtime(transitionWaitTime);

if (useShader)
        {
            Tween fadeIn = fadeImage.material.DOFloat(1.5f, "_Radius", transitionFadeInTime).SetEase(Ease.InOutSine).SetUpdate(true);
            yield return fadeIn.WaitForCompletion();
            Destroy(fadeImage.material);
        }
        else
        {
            Tween fadeIn = fadeImage.DOFade(0f, transitionFadeInTime).SetUpdate(true);
            yield return fadeIn.WaitForCompletion();
        }

        Destroy(fadeObj);
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
