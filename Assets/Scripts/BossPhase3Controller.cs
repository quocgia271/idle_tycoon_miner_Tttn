using UnityEngine;
using System.Collections.Generic;

public enum BossPhase3Skill
{
    SummonMinion,
    Barrier,
    Skill3_ShaftDoT,
    Skill4_GlobalDoT
}

[System.Serializable]
public class SkillWeight
{
    public BossPhase3Skill skill;
    [Tooltip("Trọng số ra chiêu. Số càng to xác suất ra càng cao.")]
    public float weight; 
    public float cooldownDelay = 0f; 
    [HideInInspector] public float currentCooldown = 0f;
}

public class BossPhase3Controller : MonoBehaviour, ISaveable
{
    public Animator bossAnim;
    public List<GameObject> attackVFXList;
    public GameObject[] minionPrefabs;
    
    [Header("Skill Settings")]
    public float phase3A_AttackInterval = 40f;
    public float phase3B_AttackInterval = 15f;
    private float timeBetweenAttacks = 40f;
    private bool isEnraged = false;
    public List<MineShaft> allShafts;
    public List<SkillWeight> skillWeights;

    [Header("Barrier Skill Settings")]
    public GameObject elevatorBarrier;
    public GameObject warehouseBarrier;
    private bool nextBarrierIsWarehouse = false; // Luân phiên giữa 2 màn chắn

    [Header("Skill 3 Settings")]
    public float skill3Duration = 8f;
    public float skill3DamagePerSec = 10f;

    [Header("Skill 4 Settings")]
    public List<GameObject> skill4CameraVFXs;
    public float skill4Duration = 3f;
    public float skill4DamagePerSec = 5f;
    public Color skill4DamageColor = Color.magenta;
    private Coroutine skill4Coroutine;
    public float CurrentSkill4Timer = 0f;

    private float attackTimer;

    private void Start()
    {
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnRoundChanged += HandleRoundChanged;
            HandleRoundChanged(Gamemanager.Instance.CurrentRound);
        }
    }

    private void OnDestroy()
    {
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnRoundChanged -= HandleRoundChanged;
        }
    }

    private void HandleRoundChanged(int round)
    {
        if (round != 3)
        {
            if (round == 1 && gameObject.activeInHierarchy) 
            {
                Debug.Log($"[BossPhase3] Round {round} -> Ẩn Boss GameObject: {gameObject.name}");
                StartCoroutine(HideBossRoutine()); 
            }
            this.enabled = false;
            return;
        }

        Debug.Log($"<color=magenta>[BossPhase3] Round 3 -> BẬT BOSS THÀNH CÔNG! Object: {gameObject.name} đang hiển thị!</color>");
        
        gameObject.SetActive(true);
        this.enabled = true;
        
        timeBetweenAttacks = phase3A_AttackInterval;
        attackTimer = timeBetweenAttacks;
        if (bossAnim != null) bossAnim.Play("idle");
    }

    private System.Collections.IEnumerator HideBossRoutine()
    {
        yield return new WaitForEndOfFrame();
        if (Gamemanager.Instance != null && Gamemanager.Instance.CurrentRound == 1)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (allShafts == null || allShafts.Count == 0) return;

        foreach (var sw in skillWeights)
        {
            if (sw.currentCooldown > 0) sw.currentCooldown -= Time.deltaTime;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            ChooseAndExecuteSkill();
            attackTimer = timeBetweenAttacks;
        }
    }

    public void Enrage()
    {
        isEnraged = true;
        timeBetweenAttacks = phase3B_AttackInterval;
        attackTimer = 0f;
        Debug.Log("<color=red>[Boss 3] KHIÊN ĐÃ VỠ! BOSS 3 NỔI ĐIÊN!</color>");
    }

    public void LoadEnrage()
    {
        isEnraged = true;
        timeBetweenAttacks = phase3B_AttackInterval;
        attackTimer = timeBetweenAttacks; // Không đánh ngay khi load
    }

    public void IncreaseBarrierAggro()
    {
        if (!isEnraged) return; // Kỹ năng Barrier chỉ có ở Phase 3B

        // NẾU BOSS ĐANG BẤT TỬ (ĐANG Ở TRONG MÀN CHẮN), BỎ QUA AGGRO
        // Điều này ngăn chặn việc Boss ném khiên liên tục đè lên nhau gây bất tử vĩnh viễn
        BossHealth hp = GetComponent<BossHealth>();
        if (hp != null && hp.IsInvincible) return;

        foreach (var sw in skillWeights)
        {
            if (sw.skill == BossPhase3Skill.Barrier)
            {
                sw.weight = 150f; // Tăng siêu trọng số lên 150 để phản ứng cực gắt
                break;
            }
        }
        
        // Ép lần tung Barrier tiếp theo chắc chắn rơi vào Nhà Kho
        nextBarrierIsWarehouse = true; 

        // Rút ngắn Cooldown: Boss tức giận nên sẽ phản đòn nhanh hơn bình thường!
        if (attackTimer > 5f)
        {
            attackTimer = 5f; 
        }
    }

    private void ChooseAndExecuteSkill()
    {
        float totalWeight = 0;
        List<SkillWeight> availableSkills = new List<SkillWeight>();

        foreach (var sw in skillWeights)
        {
            bool isAvailable = IsSkillAvailable(sw.skill);
            Debug.Log($"[BOSS LOGIC] Kiểm tra kỹ năng: {sw.skill} | Cooldown: {sw.currentCooldown} | IsAvailable: {isAvailable}");
            
            if (sw.currentCooldown <= 0 && sw.weight > 0 && isAvailable)
            {
                availableSkills.Add(sw);
                totalWeight += sw.weight;
            }
        }

        // Nếu không có chiêu nào xài được (VD hầm khóa hết) -> Đợi 1 giây rồi check lại, không set lại cooldown 5s
        if (availableSkills.Count == 0) 
        {
            Debug.LogWarning("[BOSS LOGIC] KHÔNG CÓ KỸ NĂNG NÀO XÀI ĐƯỢC LÚC NÀY! Chờ 1 giây...");
            attackTimer = 1f; 
            return;
        }

        float randomVal = Random.Range(0, totalWeight);
        float cumulative = 0f;
        BossPhase3Skill chosenSkill = availableSkills[0].skill;
        SkillWeight chosenSw = availableSkills[0];

        foreach (var sw in availableSkills)
        {
            cumulative += sw.weight;
            if (randomVal <= cumulative)
            {
                chosenSkill = sw.skill;
                chosenSw = sw;
                break;
            }
        }

        Debug.Log($"[BOSS LOGIC] Tổng Weight = {totalWeight} | Quay random ra số = {randomVal} => CHỌN KỸ NĂNG: {chosenSkill}");

        // Thực thi kỹ năng được chọn
        chosenSw.currentCooldown = chosenSw.cooldownDelay; 
        attackTimer = timeBetweenAttacks; // Reset lại timer tấn công
        
        PlayAttackAnimation();
        PlayRandomVFX();
        ExecuteSkill(chosenSkill);
    }

    // Hàm kiểm tra xem Skill đó có THỰC SỰ xài được trong tình huống hiện tại không
    private bool IsSkillAvailable(BossPhase3Skill skill)
    {
        // Phase 3A: Khóa Skill 2 và 4
        if (!isEnraged && (skill == BossPhase3Skill.Barrier || skill == BossPhase3Skill.Skill4_GlobalDoT))
        {
            return false;
        }

        if (skill == BossPhase3Skill.SummonMinion)
        {
            return GetValidShaftsForSummon().Count > 0;
        }
        else if (skill == BossPhase3Skill.Barrier)
        {
            if (nextBarrierIsWarehouse) 
            {
                // Nếu chuẩn bị gọi Warehouse Barrier mà cái cũ vẫn còn đang sống -> Không cho gọi
                if (warehouseBarrier != null && warehouseBarrier.activeInHierarchy) return false;
                return true; 
            }
            else 
            {
                // Nếu chuẩn bị gọi Elevator Barrier mà cái cũ vẫn còn đang sống -> Không cho gọi
                if (elevatorBarrier != null && elevatorBarrier.activeInHierarchy) return false;
                return GetActiveShafts().Count > 0; 
            }
        }
        else if (skill == BossPhase3Skill.Skill3_ShaftDoT)
        {
            return GetValidShaftsForSkill3().Count > 0;
        }
        else if (skill == BossPhase3Skill.Skill4_GlobalDoT)
        {
            return skill4Coroutine == null && GetActiveShafts().Count > 0;
        }
        return true;
    }

    private void ExecuteSkill(BossPhase3Skill skill)
    {
        switch (skill)
        {
            case BossPhase3Skill.SummonMinion:
                ExecuteSummonMinion();
                break;
            case BossPhase3Skill.Barrier:
                ExecuteBarrier();
                break;
            case BossPhase3Skill.Skill3_ShaftDoT:
                ExecuteSkill3();
                break;
            case BossPhase3Skill.Skill4_GlobalDoT:
                ExecuteSkill4();
                break;
        }
    }

    private void PlayAttackAnimation()
    {
        if (bossAnim != null)
        {
            bool useAttack2 = Random.value > 0.5f;
            bossAnim.SetTrigger(useAttack2 ? "attack02" : "attack");
        }
    }

    private void PlayRandomVFX()
    {
        if (attackVFXList != null && attackVFXList.Count > 0)
        {
            List<GameObject> validVFXs = new List<GameObject>();
            foreach(var vfx in attackVFXList)
            {
                if (vfx != null) 
                {
                    vfx.SetActive(false);
                    validVFXs.Add(vfx);
                }
            }

            if (validVFXs.Count > 0)
            {
                int rndIndex = Random.Range(0, validVFXs.Count);
                GameObject chosenVfx = validVFXs[rndIndex];
                chosenVfx.SetActive(true);

                ParticleSystem[] pss = chosenVfx.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in pss)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                }
            }
        }
    }

    // Tách riêng hàm lấy hầm đang mở để tái sử dụng
    private List<MineShaft> GetActiveShafts()
    {
        List<MineShaft> activeShafts = new List<MineShaft>();
        foreach (var shaft in allShafts)
        {
            if (shaft != null && shaft.gameObject.activeInHierarchy && !shaft.isBroken)
            {
                if (shaft.isInvincible) continue; // Bỏ qua hầm đang có khiên bất tử
                
                ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>(true);
                if (unlocker == null || !unlocker.gameObject.activeInHierarchy)
                {
                    activeShafts.Add(shaft);
                }
            }
        }
        return activeShafts;
    }

    // Tách riêng hàm lấy hầm trống để gọi quái
    private List<MineShaft> GetValidShaftsForSummon()
    {
        List<MineShaft> validShafts = new List<MineShaft>();
        List<MineShaft> activeShafts = GetActiveShafts();

        foreach (var shaft in activeShafts)
        {
            bool hasActiveMinion = false;
            MinionController[] minionsInShaft = shaft.GetComponentsInChildren<MinionController>(true);
            foreach (var m in minionsInShaft)
            {
                if (m.gameObject.activeInHierarchy)
                {
                    hasActiveMinion = true;
                    break;
                }
            }

            if (!hasActiveMinion) validShafts.Add(shaft);
        }
        return validShafts;
    }

    private List<MineShaft> GetValidShaftsForSkill3()
    {
        List<MineShaft> validShafts = new List<MineShaft>();
        List<MineShaft> activeShafts = GetActiveShafts();

        foreach (var shaft in activeShafts)
        {
            if (!shaft.IsSkill3Active && shaft.fireClicksRemaining <= 0)
            {
                validShafts.Add(shaft);
            }
        }
        return validShafts;
    }

    private MineShaft ChooseShaftWithPriority(List<MineShaft> validShafts)
    {
        if (validShafts.Count == 0) return null;

        float totalWeight = 0;
        List<float> cumulativeWeights = new List<float>();

        for (int i = 0; i < validShafts.Count; i++)
        {
            // Ưu tiên các hầm có chỉ số cao (Hầm sâu nhất)
            float baseScore = i + 1; 
            float weight = Mathf.Pow(baseScore, 1.5f); 
            
            totalWeight += weight;
            cumulativeWeights.Add(totalWeight);
        }

        float randomVal = Random.Range(0, totalWeight);
        MineShaft chosenShaft = validShafts[0];

        for (int i = 0; i < validShafts.Count; i++)
        {
            if (randomVal <= cumulativeWeights[i])
            {
                chosenShaft = validShafts[i];
                break;
            }
        }

        return chosenShaft;
    }

    private void ExecuteBarrier()
    {
        if (nextBarrierIsWarehouse)
        {
            if (warehouseBarrier != null)
            {
                warehouseBarrier.SetActive(false); // Reset
                warehouseBarrier.SetActive(true);
            }
        }
        else
        {
            if (elevatorBarrier != null)
            {
                List<MineShaft> activeShafts = GetActiveShafts();
                if (activeShafts.Count > 0)
                {
                    MineShaft targetShaft = activeShafts[Random.Range(0, activeShafts.Count)];
                    
                    Vector3 currentPos = elevatorBarrier.transform.position;
                    currentPos.y = targetShaft.transform.position.y;
                    elevatorBarrier.transform.position = currentPos;

                    elevatorBarrier.SetActive(false); // Reset
                    elevatorBarrier.SetActive(true);
                }
            }
        }

        // RESET AGGRO SAU KHI ĐÃ TRẢ THÙ XONG
        foreach (var sw in skillWeights)
        {
            if (sw.skill == BossPhase3Skill.Barrier)
            {
                sw.weight = 25f; // Trả về trọng số mặc định ban đầu
                break;
            }
        }

        nextBarrierIsWarehouse = !nextBarrierIsWarehouse;
    }

    private void ExecuteSummonMinion()
    {
        List<MineShaft> validShafts = GetValidShaftsForSummon();
        if (validShafts.Count == 0 || minionPrefabs == null || minionPrefabs.Length == 0) return;

        MineShaft chosenShaft = ChooseShaftWithPriority(validShafts);

        GameObject randomPrefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
        GameObject newMinionObj = Instantiate(randomPrefab, chosenShaft.transform);
        
        MinionController chosenMinion = newMinionObj.GetComponent<MinionController>();
        if (chosenMinion != null)
        {
            chosenMinion.targetShaft = chosenShaft; 
        }
    }

    private void ExecuteSkill3()
    {
        List<MineShaft> validShafts = GetValidShaftsForSkill3();
        if (validShafts.Count > 0)
        {
            MineShaft targetShaft = ChooseShaftWithPriority(validShafts);
            if (targetShaft != null)
            {
                // Cân bằng Game (DoT Standard): Độc/Cháy trong game RPG/Idle chuẩn thường gây 5% Max HP mỗi giây.
                // Tổng 5 giây = 25% máu, cho người chơi đủ thời gian phản ứng và không bị chết sốc khi dính đạn của Rồng.
                float scaledDamage = targetShaft.maxEndurance * 0.05f;
                targetShaft.TriggerSkill3VFX(skill3Duration, scaledDamage);
            }
        }
    }

    private void ExecuteSkill4()
    {
        if (skill4Coroutine != null) StopCoroutine(skill4Coroutine);
        skill4Coroutine = StartCoroutine(Skill4Routine());
    }

    private System.Collections.IEnumerator Skill4Routine()
    {
        GameObject chosenVFX = null;

        if (skill4CameraVFXs != null && skill4CameraVFXs.Count > 0)
        {
            chosenVFX = skill4CameraVFXs[Random.Range(0, skill4CameraVFXs.Count)];
            if (chosenVFX != null)
            {
                chosenVFX.SetActive(true);
                
                // Nếu có ParticleSystem thì Play nó
                ParticleSystem[] pss = chosenVFX.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in pss)
                {
                    ps.Play(true);
                }
            }
        }

        if (CurrentSkill4Timer <= 0) CurrentSkill4Timer = skill4Duration;
        float tickTimer = 1f;
        while (CurrentSkill4Timer > 0)
        {
            CurrentSkill4Timer -= Time.deltaTime;
            tickTimer -= Time.deltaTime;
            
            if (tickTimer <= 0f)
            {
                // Gây sát thương toàn bộ hầm 1 lần mỗi giây
                List<MineShaft> activeShafts = GetActiveShafts();
                foreach (var shaft in activeShafts)
                {
                    // 5% max máu mỗi giây
                    float scaledDamage = shaft.maxEndurance * 0.05f;
                    shaft.AddEndurance(-scaledDamage, skill4DamageColor);
                }
                tickTimer = 1f;
            }

            yield return null;
        }

        if (chosenVFX != null)
        {
            float maxLifetime = 0f;
            
            // Tắt VFX mượt mà nếu là ParticleSystem
            ParticleSystem[] pss = chosenVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in pss)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (ps.main.startLifetime.constantMax > maxLifetime)
                {
                    maxLifetime = ps.main.startLifetime.constantMax;
                }
            }
            
            if (maxLifetime > 0)
            {
                yield return new WaitForSeconds(maxLifetime);
            }
            
            chosenVFX.SetActive(false);
        }
        skill4Coroutine = null;
        CurrentSkill4Timer = 0f;
    }

    public void RestoreMinion(MineShaft targetShaft, float loadedHealth)
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;
        GameObject randomPrefab = minionPrefabs[UnityEngine.Random.Range(0, minionPrefabs.Length)];
        GameObject newMinionObj = Instantiate(randomPrefab, targetShaft.transform);
        
        MinionController chosenMinion = newMinionObj.GetComponent<MinionController>();
        if (chosenMinion != null)
        {
            chosenMinion.targetShaft = targetShaft; 
            chosenMinion.LoadHealth(loadedHealth);
        }
    }

    public void PopulateSaveData(SaveData data)
    {
        if (Gamemanager.Instance == null || Gamemanager.Instance.CurrentRound != 3) return;

        data.BossData.Skill4Timer = this.CurrentSkill4Timer;

        data.BossData.ElevatorBarrierTimer = 0f;
        data.BossData.WarehouseBarrierTimer = 0f;

        if (elevatorBarrier != null && elevatorBarrier.activeInHierarchy)
        {
            BarrierController bc = elevatorBarrier.GetComponent<BarrierController>();
            if (bc != null && bc.currentTimer > 0)
            {
                data.BossData.ElevatorBarrierTimer = bc.currentTimer;
            }
        }
        
        if (warehouseBarrier != null && warehouseBarrier.activeInHierarchy)
        {
            BarrierController bc = warehouseBarrier.GetComponent<BarrierController>();
            if (bc != null && bc.currentTimer > 0)
            {
                data.BossData.WarehouseBarrierTimer = bc.currentTimer;
            }
        }

        RoundTransitionBarrier barrier = FindObjectOfType<RoundTransitionBarrier>(true);
        if (barrier != null) 
        {
            data.IsRound3BarrierBroken = !barrier.gameObject.activeInHierarchy;
        }
    }

    public void LoadFromSaveData(SaveData data)
    {
        if (Gamemanager.Instance == null || Gamemanager.Instance.CurrentRound != 3) return;

        // Phục hồi Minions
        MineShaft[] shafts = FindObjectsOfType<MineShaft>();
        foreach (var shaft in shafts)
        {
            var savedData = data.MineShafts.Find(s => s.Index == shaft.ShaftIndex);
            if (savedData != null && savedData.MinionHealth > 0)
            {
                RestoreMinion(shaft, savedData.MinionHealth);
            }
        }

        RoundTransitionBarrier barrier = FindObjectOfType<RoundTransitionBarrier>(true);
        if (barrier != null && data.IsRound3BarrierBroken)
        {
            barrier.gameObject.SetActive(false);
        }
        
        if (data.IsRound3BarrierBroken)
        {
            LoadEnrage();
            if (data.BossData != null && data.BossData.Skill4Timer > 0)
            {
                CurrentSkill4Timer = data.BossData.Skill4Timer;
                ExecuteSkill(BossPhase3Skill.Skill4_GlobalDoT); 
            }
            
            if (data.BossData != null)
            {
                if (data.BossData.ElevatorBarrierTimer > 0 && elevatorBarrier != null)
                {
                    BarrierController bc = elevatorBarrier.GetComponent<BarrierController>();
                    if (bc != null) bc.currentTimer = data.BossData.ElevatorBarrierTimer;
                    elevatorBarrier.SetActive(true);
                }
                if (data.BossData.WarehouseBarrierTimer > 0 && warehouseBarrier != null)
                {
                    BarrierController bc = warehouseBarrier.GetComponent<BarrierController>();
                    if (bc != null) bc.currentTimer = data.BossData.WarehouseBarrierTimer;
                    warehouseBarrier.SetActive(true);
                }
            }
        }
    }
}
