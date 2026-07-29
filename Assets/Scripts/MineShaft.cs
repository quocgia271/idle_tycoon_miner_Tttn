using UnityEngine;
using TMPro; // Thêm thư viện này để dùng TextMeshPro
using System.Collections.Generic; // Thêm thư viện dùng List
using DG.Tweening; // Thư viện tạo hiệu ứng DOTween

// Kế thừa Facility thay vì MonoBehaviour để có sẵn tính năng Nâng cấp
public class MineShaft : Facility 
{
    public override FacilityType GetFacilityType() => FacilityType.MineShaft;

    [Header("Shaft Settings")]
    [Tooltip("Thứ tự của hầm (từ 1 đến 10). Dùng để tính toán Cost/Income theo độ sâu.")]
    public int ShaftIndex = 1;

    // Ghi đè để áp dụng Toán học độ sâu hầm
    public override double ScaledBaseCost 
    {
        get 
        {
            if (Config == null) return 0;
            double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
            return Config.BaseCost * System.Math.Pow(15, ShaftIndex - 1) * roundMultiplier;
        }
    }
    
    // Ghi đè hệ số nhân mặc định nếu cần
    public override double CostMultiplier => 1.14;

    public double CurrentResource = 0; 
    
    public double BaseResourcePerSecond = 10; 
    
    [Header("Miner Settings")]
    public int MaxMiners = 5;
    public float MaxMoveSpeed = 5f;
    public float MinDigTime = 0.5f;

    [Header("Miner Spawn Settings")]
    public Miner minerPrefab;
    public Transform minerStartPos;
    public Transform minerDigPos;
    public float spawnOffsetX = 0.5f;
    public List<Miner> activeMiners = new List<Miner>();

    [Header("Elevator Special Birds")]
    public GameObject attackBirdVFX;
    public GameObject healBirdVFX;
    public GameObject birdAttackProjectilePrefab;
    [Tooltip("Chỉnh vị trí nòng đạn của chim (ví dụ y=1 để đạn bắn từ miệng thay vì dưới chân)")]
    public Vector3 birdProjectileSpawnOffset = new Vector3(0, 0f, 0);

    [Header("Mineshaft Invincibility")]
    public bool isInvincible = false;
    public GameObject invincibilityVFX;

    [Header("Manager Settings")]
    // Các buff này sẽ nằm chung dưới thẻ Manager Buffs của lớp cha Facility
    public float MinerMoveSpeedBuff = 1f;
    public float MinerDigSpeedBuff = 1f;
    public float ProductivityBuff = 1f;

    // Năng suất của một thợ mỏ
    public double ResourcePerSecond => GetWorkerProductivity(Level); 

    public int GetMinersCount(int targetLevel)
    {
        // Mỗi 10 cấp thêm 1 thợ, tối đa là MaxMiners
        int count = 1 + (targetLevel / 10);
        return Mathf.Min(count, MaxMiners);
    }

    public float GetMinerMoveSpeed(int targetLevel)
    {
        float speed = Config != null ? Config.BaseSpeed + (targetLevel * 0.05f) : 2f;
        speed = Mathf.Min(speed, MaxMoveSpeed);
        return speed * MinerMoveSpeedBuff;
    }

    public float GetMinerDigTime(int targetLevel)
    {
        float digTime = 2f - (targetLevel * 0.01f);
        digTime = Mathf.Max(digTime, MinDigTime);
        return digTime / MinerDigSpeedBuff;
    }

    public double GetWorkerProductivity(int targetLevel)
    {
        // Thu nhập cơ bản tăng theo độ sâu hầm (Gấp 12 lần mỗi hầm)
        double scaledBaseIncome = BaseResourcePerSecond * System.Math.Pow(12, ShaftIndex - 1); 
        // 1.07 là hệ số nhân mũ mỗi cấp độ của hầm.
        double exponentialIncome = scaledBaseIncome * System.Math.Pow(1.07, targetLevel - 1);
        
        double prestigeMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.PrestigeMultiplier : 1.0;
        double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
        
        return exponentialIncome * ProductivityBuff * prestigeMultiplier * roundMultiplier;
    }

    [Header("Endurance Settings")]
    public float maxEndurance = 100f;
    public float currentEndurance = 100f;
    public UnityEngine.UI.Slider enduranceSlider;

    [Header("Damage Popup")]
    public DamagePopup damagePopupPrefab;
    [Tooltip("Vị trí sinh ra số sát thương. Nếu để trống sẽ lấy tâm của hầm.")]
    public Transform popupSpawnPoint;

    [Header("Broken State")]
    public bool isBroken = false;
    public GameObject explosionVFX; // VFX phát nổ chớp nhoáng lúc vừa sập
    public float fallbackExplosionDuration = 1.5f; // Dự phòng nếu VFX không có ParticleSystem
    public GameObject damagedStateVFX; // VFX khói lửa duy trì suốt lúc hỏng
    public double repairCost = 5000;

    public void AddEndurance(float amount)
    {
        AddEndurance(amount, Color.white);
    }

    public void AddEndurance(float amount, Color damageColor)
    {
        if (isInvincible && amount < 0) return; // Miễn nhiễm sát thương khi có shield
        if (isBroken && amount < 0) return; // Nếu đang vỡ thì không nhận thêm damage

        currentEndurance = Mathf.Clamp(currentEndurance + amount, 0, maxEndurance);
        UpdateEnduranceUI();

        if (amount < 0)
        {
            // Vì Lửa và Độc đã được sửa lại để giật sát thương 1 giây 1 lần
            // Nên ta có thể yên tâm cho bung số lên mỗi khi bị trừ máu
            SpawnDamagePopup(-amount, damageColor, false);
        }
        else if (amount > 0)
        {
            SpawnDamagePopup(amount, damageColor, true); // true = heal popup
        }

        if (currentEndurance <= 0f && !isBroken)
        {
            BreakShaft();
        }
    }

    public void SpawnDamagePopup(float amount, Color color, bool isHeal = false)
    {
        if (damagePopupPrefab != null)
        {
            DamagePopup popup;
            if (popupSpawnPoint != null)
            {
                // Sử dụng Object Pool riêng biệt cho Mineshaft
                popup = DamagePopup.Create(damagePopupPrefab, popupSpawnPoint.position, popupSpawnPoint, DamagePopup.PopupSourceType.Mineshaft);
            }
            else
            {
                popup = DamagePopup.Create(damagePopupPrefab, transform.position, null, DamagePopup.PopupSourceType.Mineshaft);
            }
            // Truyền tham số 0.1f (scaleFactor) để chữ ở hầm bay lên cực kỳ ngắn (bằng 1/10 của Boss)
            // Khoảng cách tản ra (scatter) siêu hẹp, bám sát Hầm
            popup.Setup(amount, 0.1f, color, isHeal);
        }
    }

    private void BreakShaft()
    {
        isBroken = true;
        
        // CÂN BẰNG TOÁN HỌC MỚI (Dynamic Income Taxation):
        // Boss 3 đánh hỏng hầm mỗi ~120s. Để tạo ra Thuế 25%,
        // Phí sửa chữa = 120s * 25% = 30 giây tổng thu nhập của Hầm.
        repairCost = GetTotalExtractionPerSecond(Level) * 30;
        if (repairCost < 100) repairCost = 100;
        
        // Tắt hết lửa nhỏ, lửa to một cách mượt mà (chờ các hạt tàn lụi)
        normalBurnTimer = 0f;
        bigBurnTimer = 0f;
        StopVFXSmoothly(normalBurnVFX);
        StopVFXSmoothly(bigBurnVFX);
        
        // Tắt Skill 3 nếu đang chạy
        ForceStopSkill3();

        // Bật VFX phát nổ và tự động tính toán thời gian của VFX
        float waitTime = fallbackExplosionDuration;
        if (explosionVFX != null) 
        {
            explosionVFX.SetActive(true);
            
            // Tìm tất cả ParticleSystem trong VFX nổ để lấy thời gian dài nhất
            ParticleSystem[] pSystems = explosionVFX.GetComponentsInChildren<ParticleSystem>();
            if (pSystems.Length > 0)
            {
                float maxDuration = 0f;
                foreach (var ps in pSystems)
                {
                    // Tổng thời gian = Thời lượng phát + Thời gian sống của hạt
                    float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                    if (duration > maxDuration) maxDuration = duration;
                }
                if (maxDuration > 0) waitTime = maxDuration;
            }
        }

        // Bắt đầu khóa hầm sớm hơn (chỉ chờ 1 nửa thời gian nổ) để không phải chờ quá lâu
        StartCoroutine(LockShaftRoutine(waitTime * 0.5f));
    }

    private System.Collections.IEnumerator LockShaftRoutine(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        // Tắt VFX nổ
        if (explosionVFX != null) explosionVFX.SetActive(false);

        // Bật VFX trạng thái hư hỏng tàn tạ
        if (damagedStateVFX != null) damagedStateVFX.SetActive(true);

        // Hiện bảng báo sửa chữa (ShaftUnlocker)
        ShaftUnlocker unlocker = GetComponentInChildren<ShaftUnlocker>(true);
        if (unlocker != null)
        {
            unlocker.TriggerRepairMode(repairCost);
        }
    }

    public void RepairShaft()
    {
        isBroken = false;
        currentEndurance = maxEndurance; // Hồi đầy máu
        UpdateEnduranceUI();
        
        if (damagedStateVFX != null) 
        {
            StopVFXSmoothly(damagedStateVFX); // Tắt mượt mà thay vì tắt rụp
        }
        if (explosionVFX != null) explosionVFX.SetActive(false);
    }

    private void StopVFXSmoothly(GameObject vfxObject)
    {
        if (vfxObject == null || !vfxObject.activeSelf) return;
        if (stopVFXCoroutines.ContainsKey(vfxObject) && stopVFXCoroutines[vfxObject] != null)
        {
            StopCoroutine(stopVFXCoroutines[vfxObject]);
        }
        stopVFXCoroutines[vfxObject] = StartCoroutine(StopVFXRoutine(vfxObject));
    }

    private System.Collections.IEnumerator StopVFXRoutine(GameObject vfxObject)
    {
        ParticleSystem[] pSystems = vfxObject.GetComponentsInChildren<ParticleSystem>();
        float maxLifetime = 0f;
        
        if (pSystems.Length > 0)
        {
            foreach (var ps in pSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (ps.main.startLifetime.constantMax > maxLifetime)
                {
                    maxLifetime = ps.main.startLifetime.constantMax;
                }
            }
            // Chờ cho các hạt rơi/bay nốt rồi mới tắt hẳn object
            yield return new WaitForSeconds(maxLifetime);
        }
        
        if (vfxObject != null)
        {
            vfxObject.SetActive(false);
            if (stopVFXCoroutines.ContainsKey(vfxObject)) stopVFXCoroutines[vfxObject] = null;
        }
    }

    private Dictionary<GameObject, Coroutine> stopVFXCoroutines = new Dictionary<GameObject, Coroutine>();

    public void PlayVFXSmoothly(GameObject vfxObject)
    {
        if (vfxObject == null) return;
        
        if (stopVFXCoroutines.ContainsKey(vfxObject) && stopVFXCoroutines[vfxObject] != null)
        {
            StopCoroutine(stopVFXCoroutines[vfxObject]);
            stopVFXCoroutines[vfxObject] = null;
        }

        vfxObject.SetActive(true);
        ParticleSystem[] pSystems = vfxObject.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in pSystems)
        {
            // Reset hoàn toàn hạt cũ trước khi Play lại để tránh bị kẹt trạng thái Stop
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    public void UpdateEnduranceUI()
    {
        if (enduranceSlider != null)
        {
            enduranceSlider.DOKill();
            enduranceSlider.DOValue(currentEndurance / maxEndurance, 0.5f).SetEase(Ease.OutCubic);
        }
    }

    public double GetTotalExtractionPerSecond(int targetLevel)
    {
        // Tổng lượng đào = Năng suất 1 thợ * Số lượng thợ
        return GetWorkerProductivity(targetLevel) * GetMinersCount(targetLevel);
    }

    [Header("UI")]
    public TextMeshProUGUI shaftCashText; // Hiển thị số tiền/tài nguyên hiện tại của hầm

    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của lớp cha (Facility) để update text Level

        // Tìm thợ mỏ có sẵn trong cảnh (con của hầm) và thêm vào danh sách nếu chưa có
        Miner[] existingMiners = GetComponentsInChildren<Miner>();
        foreach (Miner m in existingMiners)
        {
            if (!activeMiners.Contains(m))
            {
                m.currentShaft = this;
                activeMiners.Add(m);
            }
        }

        UpdateUI();
        UpdateEnduranceUI();
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void ActivateManagerSkill()
    {
        // Khóa không cho bật kỹ năng (kể cả bất tử) nếu hầm đang trong trạng thái bị vỡ/hỏng
        if (isBroken) return;
        
        base.ActivateManagerSkill();
    }

    protected override void ApplyManagerBuff()
    {
        base.ApplyManagerBuff();
        if (currentManager == null) return;
        
        // --- SENIOR SPECIAL FEATURE (Invincibility) ---
        if (currentManager.SpecialFeature == SeniorSpecialFeature.SpecialFeature)
        {
            isInvincible = true;
            if (invincibilityVFX != null) invincibilityVFX.SetActive(true);
            
            // Hồi đầy máu ngay lập tức
            currentEndurance = maxEndurance;
            UpdateEnduranceUI();
            SpawnDamagePopup(100f, Color.yellow, true);
            
            // Xóa hiệu ứng lửa của Rồng
            normalBurnTimer = 0f;
            bigBurnTimer = 0f;
            StopVFXSmoothly(normalBurnVFX);
            StopVFXSmoothly(bigBurnVFX);
            
            // Hồi sinh và giải độc toàn bộ thợ mỏ trong hầm
            if (activeMiners != null)
            {
                foreach (var miner in activeMiners)
                {
                    if (miner != null)
                    {
                        miner.Revive();
                    }
                }
            }
            
            // Xóa DOT skill 3 của Boss
            ForceStopSkill3();

            // Tiêu diệt Minion đang có trong hầm
            MinionController[] minions = GetComponentsInChildren<MinionController>(true);
            foreach (var minion in minions)
            {
                if (minion.gameObject.activeInHierarchy)
                {
                    minion.TakeDamage(9999f); // Tiêu diệt minion
                }
            }
        }
        
        float buffMultiplier = 1f + (currentManager.BuffValue / 100f);
        float costDiscount = 1f - (currentManager.BuffValue / 100f);

        switch (currentManager.BuffType)
        {
            case ManagerBuffType.MoveSpeed:
                MinerMoveSpeedBuff = buffMultiplier;
                break;
            case ManagerBuffType.MiningSpeed:
                MinerDigSpeedBuff = buffMultiplier;
                break;
        }
    }

    protected override void RemoveManagerBuff()
    {
        base.RemoveManagerBuff();
        isInvincible = false;
        if (invincibilityVFX != null) invincibilityVFX.SetActive(false);

        MinerMoveSpeedBuff = 1f;
        MinerDigSpeedBuff = 1f;
        UpgradeCostDiscount = 1f;
        UpdateUpgradeUI();
    }

    // Hàm này được gọi bởi con thợ mỏ sau khi nó đào xong
    public void AddResource(double amount)
    {
        CurrentResource += amount;
        UpdateUI();
    }

    public double TakeResource(double amountToTake)
    {
        double taken = 0;
        if (amountToTake > CurrentResource)
        {
            taken = CurrentResource;
            CurrentResource = 0;
        }
        else
        {
            CurrentResource -= amountToTake;
            taken = amountToTake;
        }
        
        UpdateUI(); // Cập nhật lại UI sau khi thang máy lấy đi
        return taken;
    }

    private void UpdateUI()
    {
        if (shaftCashText != null)
        {
            // Sử dụng CurrencyFormatter để hiển thị số mượt hơn (K, M, B)
            shaftCashText.text = CurrencyFormatter.FormatMoney(CurrentResource);
        }
    }

    // Logic xử lý thêm (nếu có) khi Hầm mỏ được nâng cấp
    protected override void OnUpgraded()
    {
        // Vì ResourcePerSecond tính trực tiếp từ Level, nên nó tự động tăng.
        // Cập nhật số lượng thợ mỏ nếu đạt đủ level
        CheckAndSpawnMiners();
    }

    private void CheckAndSpawnMiners()
    {
        if (minerPrefab == null || minerStartPos == null || minerDigPos == null) return;

        int targetCount = GetMinersCount(Level);

        // Vì lúc Start() chúng ta đã tự tìm và add người đầu tiên vào activeMiners
        // Nên bây giờ activeMiners.Count đã phản ánh đúng số lượng thực tế
        while (activeMiners.Count < targetCount)
        {
            SpawnSingleMiner();
        }
    }

    private void SpawnSingleMiner()
    {
        if (minerPrefab == null || minerStartPos == null || minerDigPos == null) return;
        
        // Đánh số thứ tự bắt đầu từ 1 để nó lùi về sau lưng con gốc
        int index = activeMiners.Count + 1; 
        
        // Tính toán vị trí lùi về sau (bên trái) theo X
        Vector3 spawnPos = minerStartPos.position - new Vector3(spawnOffsetX * index, 0, 0);

        Miner newMiner = Instantiate(minerPrefab, spawnPos, Quaternion.identity, transform);
        newMiner.currentShaft = this;
        
        // Gán lại startPos ảo cho thợ mỏ này bằng một object rỗng tạo ra tại chỗ
        GameObject tempStart = new GameObject($"StartPos_Miner_{index}");
        tempStart.transform.position = spawnPos;
        tempStart.transform.SetParent(transform);

        newMiner.startPos = tempStart.transform;
        newMiner.digPos = minerDigPos;

        activeMiners.Add(newMiner);
    }

    public void BuyBackMiner(double cost)
    {
        int targetCount = GetMinersCount(Level);
        if (activeMiners.Count < targetCount)
        {
            if (Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= cost)
            {
                Gamemanager.Instance.IdleCash -= cost;
                SpawnSingleMiner();
            }
            else
            {
                Debug.LogWarning("Không đủ tiền mua lại thợ mỏ!");
            }
        }
        else
        {
            Debug.LogWarning("Hầm đã đầy đủ nhân viên, không cần mua lại.");
        }
    }

    public override (string curVal, string nextVal) GetStatDisplay(int statIndex, int currentLevel, int nextLevel)
    {
        string curVal = "0";
        string nextVal = "0";
        
        switch (statIndex)
        {
            case 0: // Tổng khai thác
                curVal = GetTotalExtractionPerSecond(currentLevel).ToString("F1") + "/s";
                nextVal = GetTotalExtractionPerSecond(nextLevel).ToString("F1") + "/s";
                break;
            case 1: // Số thợ mỏ
                curVal = GetMinersCount(currentLevel).ToString();
                nextVal = GetMinersCount(nextLevel).ToString();
                break;
            case 2: // Tốc độ di chuyển
                curVal = GetMinerMoveSpeed(currentLevel).ToString("F2");
                nextVal = GetMinerMoveSpeed(nextLevel).ToString("F2");
                break;
            case 3: // Tốc độ khai thác (thời gian cuốc đất)
                curVal = GetMinerDigTime(currentLevel).ToString("F2") + "s";
                nextVal = GetMinerDigTime(nextLevel).ToString("F2") + "s";
                break;
            case 4: // Năng suất 1 thợ mỏ
                curVal = GetWorkerProductivity(currentLevel).ToString("F1") + "/s";
                nextVal = GetWorkerProductivity(nextLevel).ToString("F1") + "/s";
                break;
        }

        return (curVal, nextVal);
    }

    // ==========================================
    // LOGIC BỊ ĐỐT CHÁY BỞI RỒNG
    // ==========================================
    [Header("VFX Hầm")]
    public GameObject normalBurnVFX; // Lửa nhỏ cho đạn thường
    public GameObject bigBurnVFX;    // Lửa to cho đạn bự
    
    [Header("Skill 3 Settings")]
    public List<GameObject> skill3VFXs;
    public List<Color> skill3DamageColors;
    private Coroutine skill3Coroutine;
    private GameObject activeSkill3VFX;
    public bool IsSkill3Active => skill3Coroutine != null;

    [Header("Burn Damage Settings")]
    public float normalBurnDamagePerSec = 2f; // Sát thương lửa nhỏ mỗi giây
    public float bigBurnDamagePerSec = 5f;    // Sát thương lửa to mỗi giây

    private float normalBurnTimer = 0f;
    private float bigBurnTimer = 0f;
    private Coroutine burnCoroutine;

    public void TriggerBurnVFX(float duration, bool isBig)
    {
        if (isBroken || isInvincible) return; // Không dính hiệu ứng cháy nếu đang vỡ hoặc có khiên bất tử

        if (isBig)
        {
            // Trúng đạn to: Tắt mượt mà lửa nhỏ, cộng dồn thời gian lửa to
            normalBurnTimer = 0f; 
            StopVFXSmoothly(normalBurnVFX);
            bigBurnTimer += duration;
            PlayVFXSmoothly(bigBurnVFX);
        }
        else
        {
            // Trúng đạn nhỏ: Cộng dồn thời gian lửa nhỏ (nếu bị trúng liên tục)
            normalBurnTimer += duration;
            PlayVFXSmoothly(normalBurnVFX);
        }

        // Bật hệ thống đếm ngược nếu nó chưa chạy
        if (burnCoroutine == null)
        {
            burnCoroutine = StartCoroutine(BurnTimerRoutine());
        }
    }

    private System.Collections.IEnumerator BurnTimerRoutine()
    {
        float tickTimer = 1f;
        while (normalBurnTimer > 0 || bigBurnTimer > 0)
        {
            tickTimer -= Time.deltaTime;

            // Xử lý lửa to
            if (bigBurnTimer > 0)
            {
                bigBurnTimer -= Time.deltaTime;
                if (tickTimer <= 0f) AddEndurance(-bigBurnDamagePerSec, Color.red);
                
                if (bigBurnTimer <= 0) 
                    StopVFXSmoothly(bigBurnVFX);
            }

            // Xử lý lửa nhỏ
            if (normalBurnTimer > 0)
            {
                normalBurnTimer -= Time.deltaTime;
                if (tickTimer <= 0f) AddEndurance(-normalBurnDamagePerSec, Color.red);
                
                if (normalBurnTimer <= 0) 
                    StopVFXSmoothly(normalBurnVFX);
            }

            if (tickTimer <= 0f) tickTimer = 1f;

            yield return null; // Chờ frame tiếp theo
        }
        
        burnCoroutine = null; // Khi cả 2 lửa đều tắt, reset coroutine
    }

    // ==========================================
    // SKILL 3: ĐỘC/SÁT THƯƠNG NGẪU NHIÊN LÊN HẦM
    // ==========================================
    private Color currentSkill3Color = Color.white;

    public void TriggerSkill3VFX(float duration, float dps)
    {
        if (isBroken || isInvincible || skill3VFXs == null || skill3VFXs.Count == 0) return;
        
        if (skill3Coroutine != null) StopCoroutine(skill3Coroutine);
        skill3Coroutine = StartCoroutine(Skill3Routine(duration, dps));
    }

    private void ForceStopSkill3()
    {
        if (skill3Coroutine != null) StopCoroutine(skill3Coroutine);
        skill3Coroutine = null;

        if (activeSkill3VFX != null)
        {
            StartCoroutine(FadeOutSkill3Routine(activeSkill3VFX));
            activeSkill3VFX = null;
        }
    }

    private System.Collections.IEnumerator FadeOutSkill3Routine(GameObject vfxObject)
    {
        SpriteRenderer[] sprites = vfxObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            sr.DOFade(0f, 0.5f);
        }

        ParticleSystem[] pSystems = vfxObject.GetComponentsInChildren<ParticleSystem>();
        float maxLifetime = 0f;
        foreach (var ps in pSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (ps.main.startLifetime.constantMax > maxLifetime)
            {
                maxLifetime = ps.main.startLifetime.constantMax;
            }
        }

        float waitTime = Mathf.Max(0.5f, maxLifetime);
        yield return new WaitForSeconds(waitTime);

        vfxObject.SetActive(false);
    }

    private System.Collections.IEnumerator Skill3Routine(float duration, float dps)
    {
        int rndIndex = Random.Range(0, skill3VFXs.Count);
        activeSkill3VFX = skill3VFXs[rndIndex];
        
        if (skill3DamageColors != null && rndIndex < skill3DamageColors.Count)
        {
            currentSkill3Color = skill3DamageColors[rndIndex];
        }
        else
        {
            currentSkill3Color = Color.white;
        }

        if (activeSkill3VFX == null) yield break;

        activeSkill3VFX.SetActive(true);

        // Fade in
        SpriteRenderer[] sprites = activeSkill3VFX.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
            sr.DOFade(1f, 0.5f);
        }

        ParticleSystem[] pSystems = activeSkill3VFX.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in pSystems)
        {
            ps.Play(true);
        }

        float timer = duration;
        float tickTimer = 1f;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            tickTimer -= Time.deltaTime;

            if (tickTimer <= 0f)
            {
                AddEndurance(-dps, currentSkill3Color);
                tickTimer = 1f;
            }

            yield return null;
        }

        // Fade out
        foreach (var sr in sprites)
        {
            sr.DOFade(0f, 0.5f);
        }

        float maxLifetime = 0f;
        foreach (var ps in pSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (ps.main.startLifetime.constantMax > maxLifetime)
            {
                maxLifetime = ps.main.startLifetime.constantMax;
            }
        }

        // Chờ fade out xong
        float waitTime = Mathf.Max(0.5f, maxLifetime);
        yield return new WaitForSeconds(waitTime);

        if (activeSkill3VFX != null)
        {
            activeSkill3VFX.SetActive(false);
            activeSkill3VFX = null;
        }
        skill3Coroutine = null;
    }

    // ==========================================
    // BIRD LOGIC (Gọi từ Elevator)
    // ==========================================
    [Header("Cleanse VFX")]
    public GameObject cleanseVFX; // Hiệu ứng thanh tẩy lan tỏa

    public void TriggerHealBird()
    {
        if (healBirdVFX == null) return;
        StartCoroutine(HealBirdRoutine());
    }

    private System.Collections.IEnumerator HealBirdRoutine()
    {
        healBirdVFX.SetActive(true);
        if (cleanseVFX != null) cleanseVFX.SetActive(true);

        yield return new WaitForSeconds(2f);
        
        // Thanh tẩy
        ForceStopSkill3();
        
        // Hồi 1 lượng sức bền nhỏ (25)
        AddEndurance(25f, Color.yellow);
        
        foreach (var miner in activeMiners)
        {
            if (miner != null && miner.healthState == Miner.HealthState.Injured)
            {
                miner.Cleanse(); // CHỈ thanh tẩy (giải độc), KHÔNG buff lại tinh thần và KHÔNG cứu người chết
            }
        }
        
        // Tắt con chim từ từ
        SpriteRenderer[] srs = healBirdVFX.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs) sr.DOFade(0f, 0.5f);
        
        yield return new WaitForSeconds(0.5f);
        healBirdVFX.SetActive(false);
        foreach (var sr in srs) { Color c = sr.color; c.a = 1f; sr.color = c; }
        
        // Giữ VFX thanh tẩy thêm một lúc cho đẹp rồi mới tắt
        yield return new WaitForSeconds(2f);
        if (cleanseVFX != null) cleanseVFX.SetActive(false);
    }

    private Coroutine attackBirdCoroutine = null;

    public void TriggerAttackBird(MinionController targetMinion)
    {
        if (attackBirdVFX == null || birdAttackProjectilePrefab == null || targetMinion == null || targetMinion.IsDead) return;
        
        if (attackBirdCoroutine != null) StopCoroutine(attackBirdCoroutine);
        attackBirdCoroutine = StartCoroutine(AttackBirdRoutine(targetMinion));
    }

    private System.Collections.IEnumerator AttackBirdRoutine(MinionController targetMinion)
    {
        attackBirdVFX.SetActive(true);
        
        // Khôi phục Alpha ngay lập tức trong trường hợp chim đang mờ dần (Fade out) ở lượt gọi trước
        SpriteRenderer[] srs = attackBirdVFX.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
        {
            sr.DOKill(); // Dừng hiệu ứng DOFade cũ
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        
        yield return new WaitForSeconds(0.5f); // Đợi chim hiện ra

        if (targetMinion != null && targetMinion.gameObject.activeInHierarchy && !targetMinion.IsDead)
        {
            // Tính toán vị trí nòng đạn dựa trên offset
            Vector3 spawnPos = attackBirdVFX.transform.position + birdProjectileSpawnOffset;
            
            // Bắn đạn: Spawn làm child của con chim để lấy đúng tỷ lệ (Scale) của chim
            GameObject proj = Instantiate(birdAttackProjectilePrefab, spawnPos, Quaternion.identity, attackBirdVFX.transform);
            
            // Sau khi nhận scale, lập tức nhả parent ra để đạn bay độc lập
            // (Nếu không nhả parent, khi chim biến mất đạn sẽ bị biến mất theo)
            proj.transform.SetParent(null, true);
            
            BirdProjectile bp = proj.GetComponent<BirdProjectile>();
            if (bp == null) bp = proj.AddComponent<BirdProjectile>();
            bp.Setup(targetMinion.transform, targetMinion);
        }

        yield return new WaitForSeconds(1f); // Đợi đạn bay
        
        SpriteRenderer[] fadeSrs = attackBirdVFX.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in fadeSrs) sr.DOFade(0f, 0.5f);
        
        yield return new WaitForSeconds(0.5f);
        attackBirdVFX.SetActive(false);
        foreach (var sr in fadeSrs) { Color c = sr.color; c.a = 1f; sr.color = c; }
        
        attackBirdCoroutine = null;
    }
}
