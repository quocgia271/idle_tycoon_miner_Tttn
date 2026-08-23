using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public partial class MineShaft : Facility 
{
    #region SETTINGS & FIELDS
    [Header("Elevator Special Birds")]
    public GameObject attackBirdVFX;
    public GameObject healBirdVFX;
    public GameObject birdAttackProjectilePrefab;
    [Tooltip("Chỉnh vị trí nòng đạn của chim (ví dụ y=1 để đạn bắn từ miệng thay vì dưới chân)")]
    public Vector3 birdProjectileSpawnOffset = new Vector3(0, 0f, 0);

    [Header("Mineshaft Invincibility")]
    public bool isInvincible = false;
    public GameObject invincibilityVFX;

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
    // public int repairCostDurationSec = 30; // Đã chuyển sang EconomyConfig
    public double minRepairCost = 100;

    [Header("Combat Magic Numbers")]
    public float healBirdAmount = 25f;
    public int bigFireClicks = 35;
    public int smallFireClicks = 15;

    public void AddEndurance(float amount)
    #endregion

    #region ENDURANCE & DAMAGE POPUP
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

    private void BreakShaft(bool isLoading = false)
    {
        isBroken = true;
        
        // CÂN BẰNG TOÁN HỌC MỚI (Dynamic Income Taxation):
        // Boss 3 đánh hỏng hầm mỗi ~120s. Để tạo ra Thuế 25%,
        // Phí sửa chữa = 120s * 25% = 30 giây tổng thu nhập của Hầm.
        float durationMultiplier = (Gamemanager.Instance != null && Gamemanager.Instance.economyConfig != null) ? Gamemanager.Instance.economyConfig.RepairCostDurationSec : 30f;
        repairCost = MathHelper.CalculateRepairCost(GetTotalExtractionPerSecond(Level), durationMultiplier);
        if (repairCost < minRepairCost) repairCost = minRepairCost;
        
        // Tắt hết lửa nhỏ, lửa to một cách mượt mà (chờ các hạt tàn lụi)
        normalBurnTimer = 0f;
        bigBurnTimer = 0f;
        StopVFXSmoothly(normalBurnVFX);
        StopVFXSmoothly(bigBurnVFX);
        
        // Tắt Skill 3 nếu đang chạy
        ForceStopSkill3();
        float waitTime = 0f;
        if (!isLoading)
        {
            waitTime = fallbackExplosionDuration;
            if (explosionVFX != null) 
            {
                explosionVFX.SetActive(true);
                ParticleSystem[] pSystems = explosionVFX.GetComponentsInChildren<ParticleSystem>();
                if (pSystems.Length > 0)
                {
                    float maxDuration = 0f;
                    foreach (var ps in pSystems)
                    {
                        float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                        if (duration > maxDuration) maxDuration = duration;
                    }
                    if (maxDuration > 0) waitTime = maxDuration;
                }

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
    #endregion

    #region VFX & UI UTILITIES
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
    private void ExtinguishFireClick()
    #endregion

    #region DRAGON BOSS - BURN LOGIC
    {
        fireClicksRemaining--;
        if (fireClicksRemaining <= 0)
        {
            // Tắt lửa
            normalBurnTimer = 0f;
            bigBurnTimer = 0f;
            StopVFXSmoothly(normalBurnVFX);
            StopVFXSmoothly(bigBurnVFX);
            
            // Hiện chữ báo hiệu dập lửa thành công
            SpawnDamagePopup(0, Color.green, true); 
        }
        else
        {
            // Có thể tạo rung nhẹ hầm để biết đã click trúng
            transform.DOKill(true);
            transform.DOPunchPosition(new Vector3(0.05f, 0, 0), 0.15f, 1, 0f);
        }
    }

    // ==========================================
    // LOGIC BỊ ĐỐT CHÁY BỞI RỒNG
    // ==========================================
    [Header("VFX Hầm")]
    public GameObject normalBurnVFX; // Lửa nhỏ cho đạn thường
    public GameObject bigBurnVFX;    // Lửa to cho đạn bự
    
    [Header("Skill 3 - Poison (Boss 3)")]
    public List<GameObject> skill3VFXs; 
    public List<Color> skill3DamageColors; 
    private Coroutine skill3Coroutine;
    private GameObject activeSkill3VFX;
    
    // Lưu trữ trạng thái Event Freeze cho Skill 3
    public float currentSkill3Timer = 0f;
    public float currentSkill3DPS = 0f;
    public int currentSkill3Index = -1;
    public bool IsSkill3Active => skill3Coroutine != null;

    [Header("Burn Damage Settings")]
    public float normalBurnDamagePerSec = 2f; // Không dùng nữa nhưng giữ lại để tương thích
    public float bigBurnDamagePerSec = 5f;    // Không dùng nữa nhưng giữ lại để tương thích
    
    [Header("Fire Clicks")]
    public int fireClicksRemaining = 0;

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
            
            bool wasAlreadyBigBurning = bigBurnTimer > 0;
            bigBurnTimer += duration;
            if (!wasAlreadyBigBurning)
            {
                PlayVFXSmoothly(bigBurnVFX);
            }
            fireClicksRemaining = bigFireClicks;
        }
        else
        {
            // Trúng đạn nhỏ: Cộng dồn thời gian lửa nhỏ (nếu bị trúng liên tục)
            bool wasAlreadyNormalBurning = normalBurnTimer > 0;
            normalBurnTimer += duration;
            if (!wasAlreadyNormalBurning && bigBurnTimer <= 0)
            {
                PlayVFXSmoothly(normalBurnVFX);
            }
            if (fireClicksRemaining < smallFireClicks) fireClicksRemaining = smallFireClicks;
        }

        // Bật hệ thống đếm ngược nếu nó chưa chạy
        if (burnCoroutine == null)
        {
            burnCoroutine = StartCoroutine(BurnTimerRoutine());
        }
    }

    private System.Collections.IEnumerator BurnTimerRoutine()
    {
        while (normalBurnTimer > 0 || bigBurnTimer > 0)
        {
            // Xử lý lửa to
            if (bigBurnTimer > 0)
            {
                bigBurnTimer -= Time.deltaTime;
                
                if (bigBurnTimer <= 0) 
                {
                    StopVFXSmoothly(bigBurnVFX);
                    fireClicksRemaining = 0;
                }
            }

            // Xử lý lửa nhỏ
            if (normalBurnTimer > 0)
            {
                normalBurnTimer -= Time.deltaTime;
                
                if (normalBurnTimer <= 0) 
                {
                    StopVFXSmoothly(normalBurnVFX);
                    fireClicksRemaining = 0;
                }
            }

            yield return null; // Chờ frame tiếp theo
        }
        
        burnCoroutine = null; // Khi cả 2 lửa đều tắt, reset coroutine
    }

    // ==========================================
    #endregion

    #region BOSS 3 - POISON SKILL
    // KỸ NĂNG 3: ĐỘC HẦM (BOSS PHASE 3)
    // ==========================================
    private Color currentSkill3Color = Color.white;

    public void TriggerSkill3VFX(float duration, float dps, int colorIndex = -1)
    {
        if (isBroken || isInvincible || skill3VFXs == null || skill3VFXs.Count == 0) return;
        
        if (skill3Coroutine != null) StopCoroutine(skill3Coroutine);
        skill3Coroutine = StartCoroutine(Skill3Routine(duration, dps, colorIndex));
    }

    private void ForceStopSkill3()
    {
        if (skill3Coroutine != null) StopCoroutine(skill3Coroutine);
        skill3Coroutine = null;
        currentSkill3Timer = 0f;

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

    private System.Collections.IEnumerator Skill3Routine(float duration, float dps, int colorIndex)
    {
        int rndIndex = colorIndex >= 0 ? colorIndex : Random.Range(0, skill3VFXs.Count);
        activeSkill3VFX = skill3VFXs[rndIndex];
        currentSkill3Index = rndIndex;
        currentSkill3DPS = dps;
        
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

        if (currentSkill3Timer <= 0) currentSkill3Timer = duration;
        float tickTimer = 1f;

        while (currentSkill3Timer > 0)
        {
            currentSkill3Timer -= Time.deltaTime;
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
    #endregion

    #region ELEVATOR BIRD ASSIST
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
        
        // Hồi 1 lượng sức bền nhỏ
        AddEndurance(healBirdAmount, Color.yellow);
        
        foreach (var miner in activeMiners)
        {
            WorkerHealth wh = miner != null ? miner.GetComponent<WorkerHealth>() : null;
            if (wh != null && wh.healthState == WorkerHealth.HealthState.Injured)
            {
                wh.Cleanse(); // CHỈ thanh tẩy (giải độc), KHÔNG buff lại tinh thần và KHÔNG cứu người chết
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
            GameObject proj = PoolManager.Instance != null 
                ? PoolManager.Instance.Spawn(birdAttackProjectilePrefab, spawnPos, Quaternion.identity) 
                : Instantiate(birdAttackProjectilePrefab, spawnPos, Quaternion.identity, attackBirdVFX.transform);
            
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
    #endregion

