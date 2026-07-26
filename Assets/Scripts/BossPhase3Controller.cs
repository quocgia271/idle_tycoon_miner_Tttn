using UnityEngine;
using System.Collections.Generic;

public enum BossPhase3Skill
{
    SummonMinion,
    Barrier
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

public class BossPhase3Controller : MonoBehaviour
{
    public Animator bossAnim;
    public List<GameObject> attackVFXList;
    public GameObject[] minionPrefabs;
    
    [Header("Skill Settings")]
    public float timeBetweenAttacks = 5f;
    public List<MineShaft> allShafts;
    public List<SkillWeight> skillWeights;

    [Header("Barrier Skill Settings")]
    public GameObject elevatorBarrier;
    public GameObject warehouseBarrier;
    private bool nextBarrierIsWarehouse = false; // Luân phiên giữa 2 màn chắn

    private float attackTimer;

    private void OnEnable()
    {
        attackTimer = timeBetweenAttacks;
        if (bossAnim != null) bossAnim.Play("idle");
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

        nextBarrierIsWarehouse = !nextBarrierIsWarehouse;
    }

    private void ExecuteSummonMinion()
    {
        List<MineShaft> validShafts = GetValidShaftsForSummon();

        if (validShafts.Count == 0 || minionPrefabs == null || minionPrefabs.Length == 0) return;

        float totalWeight = 0;
        List<float> cumulativeWeights = new List<float>();

        for (int i = 0; i < validShafts.Count; i++)
        {
            float baseScore = validShafts.Count - i; 
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

        GameObject randomPrefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];
        GameObject newMinionObj = Instantiate(randomPrefab, chosenShaft.transform);
        
        MinionController chosenMinion = newMinionObj.GetComponent<MinionController>();
        if (chosenMinion != null)
        {
            chosenMinion.targetShaft = chosenShaft; 
        }
    }
}
