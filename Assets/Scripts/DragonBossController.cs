using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DragonBossController : MonoBehaviour
{
    public Animator dragonAnim;
    
    [Header("Attack Settings")]
    public float phase3A_AttackInterval = 30f;
    public float phase3B_AttackInterval = 10f;
    private float timeBetweenAttacks = 30f;
    private bool isEnraged = false;

    public float spreadAngle = 8f; 
    public Transform mouthPosition; 
    
    [Header("Phase Settings")]
    public int wavesBeforeBigAttack = 5; // Số đợt đánh thường trước khi tung chiêu cuối
    public float timeOffScreen = 5f;     // Thời gian rồng biến mất khỏi màn hình sau khi dùng chiêu cuối
    
    [Header("Prefabs & VFX")]
    public GameObject normalFireballPrefab; 
    public GameObject bigFireballPrefab;    
    public GameObject chargeVFX;            

    [Header("Animation Names")]
    public string idleAnimName = "idledragon";
    public string attackAnimName = "SpecialAttackDragon";
    public string climbAnimName = "climbdragon";

    [Header("Movement Settings")]
    public float flySpeed = 8f;

    private float attackTimer;
    private int waveCount = 0; 
    
    private enum AttackType { Single, Rapid, Spread, Big }
    private AttackType currentAttack;
    
    private bool isBusy = false;
    private Vector3 originalPos;

    private void Start()
    {
        if (Gamemanager.Instance != null && Gamemanager.Instance.CurrentRound != 3)
        {
            gameObject.SetActive(false);
            return;
        }

        originalPos = transform.position;
        timeBetweenAttacks = phase3A_AttackInterval;
        attackTimer = timeBetweenAttacks;

        PlayAnim(idleAnimName); 
        if (chargeVFX != null) chargeVFX.SetActive(false);

        // Nếu bắt đầu game mà chưa có hầm nào mở, rồng sẽ bay đi trốn luôn
        if (!HasAnyUnlockedMineshaft())
        {
            StartCoroutine(FlyOffScreenRoutine(true));
        }
    }

    private bool HasAnyUnlockedMineshaft()
    {
        MineShaft[] allShafts = FindObjectsOfType<MineShaft>();
        foreach (var shaft in allShafts)
        {
            ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>(true);
            // Nếu không có unlocker hoặc unlocker bị ẩn (tức là đã mở khóa xong)
            if (unlocker == null || !unlocker.gameObject.activeInHierarchy)
            {
                return true;
            }
        }
        return false;
    }

    private void PlayAnim(string stateName)
    {
        if (dragonAnim != null)
        {
            dragonAnim.CrossFade(stateName, 0.2f);
        }
    }

    private void Update()
    {
        if (isBusy) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            DecideNextMajorAction();
            attackTimer = timeBetweenAttacks;
            return;
        }
    }

    public void Enrage()
    {
        isEnraged = true;
        timeBetweenAttacks = phase3B_AttackInterval;
        attackTimer = 0f;
        Debug.Log("<color=red>[Dragon] RAWRRR! RỒNG ĐÃ NỔI ĐIÊN!</color>");
    }

    private void DecideNextMajorAction()
    {
        waveCount++;

        // Nếu đạt đủ số đợt đánh thường -> Khạc đạn bự
        if (waveCount >= wavesBeforeBigAttack)
        {
            StartCoroutine(ChargeAndShootBigFireball());
        }
        else
        {
            StartCoroutine(AttackRoutine());
        }
    }

    // ==========================================
    // LOGIC TẤN CÔNG
    // ==========================================
    private IEnumerator AttackRoutine()
    {
        isBusy = true;
        
        int attackPattern = Random.Range(0, 3); 
        switch (attackPattern)
        {
            case 0: currentAttack = AttackType.Single; break;
            case 1: currentAttack = AttackType.Rapid; break;
            case 2: currentAttack = AttackType.Spread; break;
        }
        
        PlayAnim(attackAnimName);
        
        yield return new WaitForSeconds(3f);
        PlayAnim(idleAnimName);
        isBusy = false;
    }

    private IEnumerator ChargeAndShootBigFireball()
    {
        isBusy = true;
        currentAttack = AttackType.Big;

        if (chargeVFX != null) chargeVFX.SetActive(true);
        if (dragonAnim != null) dragonAnim.speed = 0.25f;

        PlayAnim(attackAnimName);

        yield return new WaitForSeconds(5f);

        if (chargeVFX != null) chargeVFX.SetActive(false);
        if (dragonAnim != null) dragonAnim.speed = 1f;
        
        PlayAnim(idleAnimName); // Ngăn chặn animation attack bị loop và xả đạn lần 2

        // Bắn xong chiêu cuối, đợi 2 giây để hả hê rồi bay khỏi màn hình
        yield return new WaitForSeconds(2f);
        StartCoroutine(FlyOffScreenRoutine(false));
    }

    public void ShootFireballEvent()
    {
        StartCoroutine(ExecuteShootPattern());
    }

    private List<MineShaft> GetBaseValidShafts()
    {
        List<MineShaft> activeShafts = new List<MineShaft>();
        MineShaft[] allShafts = FindObjectsOfType<MineShaft>();
        
        foreach (var shaft in allShafts)
        {
            if (shaft != null && shaft.gameObject.activeInHierarchy && !shaft.isBroken)
            {
                if (shaft.isInvincible) continue; // Bỏ qua hầm đang có khiên bất tử
                if (shaft.IsSkill3Active) continue; // SMART TARGETING: Bỏ qua hầm đang bị Boss 3 thả độc
                
                ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>(true);
                if (unlocker == null || !unlocker.gameObject.activeInHierarchy)
                {
                    activeShafts.Add(shaft);
                }
            }
        }
        return activeShafts;
    }

    private MineShaft GetTargetWithWeightedPriority()
    {
        List<MineShaft> activeShafts = GetBaseValidShafts();
        if (activeShafts.Count == 0) return null;

        // Sắp xếp hầm theo Y giảm dần (Y càng thấp -> hầm càng sâu -> index càng lớn)
        activeShafts.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        float totalWeight = 0;
        List<float> weights = new List<float>();

        for (int i = 0; i < activeShafts.Count; i++)
        {
            float baseScore = i + 1; 
            float weight = Mathf.Pow(baseScore, 1.5f); 
            weights.Add(weight);
            totalWeight += weight;
        }

        float randomVal = Random.Range(0, totalWeight);
        float currentSum = 0;

        for (int i = 0; i < activeShafts.Count; i++)
        {
            currentSum += weights[i];
            if (randomVal <= currentSum)
            {
                return activeShafts[i];
            }
        }
        return activeShafts[activeShafts.Count - 1];
    }

    private List<MineShaft> GetSpreadTargetsGaussian()
    {
        List<MineShaft> activeShafts = GetBaseValidShafts();
        if (activeShafts.Count <= 3) return activeShafts;

        activeShafts.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        List<MineShaft> result = new List<MineShaft>();
        int third = activeShafts.Count / 3;

        // Lấy 1 hầm ngẫu nhiên ở 1/3 trên
        result.Add(activeShafts[Random.Range(0, third)]);
        // Lấy 1 hầm ngẫu nhiên ở 1/3 giữa
        result.Add(activeShafts[Random.Range(third, 2 * third)]);
        // Lấy 1 hầm ngẫu nhiên ở 1/3 dưới cùng
        result.Add(activeShafts[Random.Range(2 * third, activeShafts.Count)]);

        return result;
    }

    private IEnumerator ExecuteShootPattern()
    {
        if (mouthPosition == null) yield break;

        switch (currentAttack)
        {
            case AttackType.Big:
                if (chargeVFX != null) chargeVFX.SetActive(false);
                if (dragonAnim != null) dragonAnim.speed = 1f;
                
                MineShaft bigTarget = GetTargetWithWeightedPriority();
                if (bigTarget != null && bigFireballPrefab != null)
                {
                    GameObject fb = Instantiate(bigFireballPrefab, mouthPosition.position, Quaternion.identity);
                    DragonFireball df = fb.GetComponent<DragonFireball>();
                    if (df != null) df.targetShaft = bigTarget.transform;
                }
                break;

            case AttackType.Single:
                MineShaft singleTarget = GetTargetWithWeightedPriority();
                if (singleTarget != null && normalFireballPrefab != null)
                {
                    GameObject fb = Instantiate(normalFireballPrefab, mouthPosition.position, Quaternion.identity);
                    DragonFireball df = fb.GetComponent<DragonFireball>();
                    if (df != null) df.targetShaft = singleTarget.transform;
                }
                break;

            case AttackType.Rapid:
                MineShaft rapidTarget = GetTargetWithWeightedPriority();
                if (rapidTarget != null && normalFireballPrefab != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        GameObject fb = Instantiate(normalFireballPrefab, mouthPosition.position, Quaternion.identity);
                        DragonFireball df = fb.GetComponent<DragonFireball>();
                        if (df != null) df.targetShaft = rapidTarget.transform;
                        yield return new WaitForSeconds(0.15f);
                    }
                }
                break;

            case AttackType.Spread:
                List<MineShaft> spreadTargets = GetSpreadTargetsGaussian();
                if (normalFireballPrefab != null)
                {
                    foreach (var target in spreadTargets)
                    {
                        GameObject fb = Instantiate(normalFireballPrefab, mouthPosition.position, Quaternion.identity);
                        DragonFireball df = fb.GetComponent<DragonFireball>();
                        if (df != null) df.targetShaft = target.transform;
                    }
                }
                break;
        }
    }

    // ==========================================
    // LOGIC BAY
    // ==========================================
    private IEnumerator FlyOffScreenRoutine(bool isFleeing = false)
    {
        isBusy = true;
        // 0. Tắt Collider để người chơi không thể click chém rồng lúc nó đang cất cánh bay
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        PlayAnim(climbAnimName);

        // 1. Bay ra khỏi mép màn hình bên phải
        Vector3 rightOffScreen = new Vector3(originalPos.x + 15f, originalPos.y + 2f, originalPos.z);
        while (Vector3.Distance(transform.position, rightOffScreen) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, rightOffScreen, flySpeed * Time.deltaTime);
            yield return null;
        }

        // 2. Dịch chuyển tức thời ra mép trái (tàng hình trong lúc đợi)
        Vector3 leftOffScreen = new Vector3(originalPos.x - 15f, originalPos.y + 2f, originalPos.z);
        transform.position = leftOffScreen;

        // 3. Đợi một khoảng thời gian (Off Screen)
        if (!isFleeing)
        {
            yield return new WaitForSeconds(timeOffScreen);
        }

        // Chờ đến khi có ít nhất 1 hầm được mở khóa thì mới bay về
        while (!HasAnyUnlockedMineshaft())
        {
            yield return new WaitForSeconds(1f);
        }

        // 4. Từ từ bay về lại đúng vị trí chiến đấu ban đầu
        while (Vector3.Distance(transform.position, originalPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPos, flySpeed * Time.deltaTime);
            yield return null;
        }

        // Reset bộ đếm để bắt đầu vòng tuần hoàn mới
        waveCount = 0;

        // 5. Đã đáp xuống an toàn -> Bật lại Collider cho người chơi chém tiếp
        if (col != null) col.enabled = true;

        PlayAnim(idleAnimName);
        attackTimer = timeBetweenAttacks; // Tránh việc vừa bay về đã khạc lửa luôn
        isBusy = false;
    }
}
