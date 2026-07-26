using UnityEngine;
using System.Collections;

public class DragonBossController : MonoBehaviour
{
    public Animator dragonAnim;
    
    [Header("Attack Settings")]
    public float timeBetweenAttacks = 5f;
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
        originalPos = transform.position;
        attackTimer = timeBetweenAttacks;

        PlayAnim(idleAnimName); 
        if (chargeVFX != null) chargeVFX.SetActive(false);
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

        // Bắn xong chiêu cuối, đợi 2 giây để hả hê rồi bay khỏi màn hình
        yield return new WaitForSeconds(2f);
        StartCoroutine(FlyOffScreenRoutine());
    }

    public void ShootFireballEvent()
    {
        StartCoroutine(ExecuteShootPattern());
    }

    private IEnumerator ExecuteShootPattern()
    {
        if (mouthPosition == null) yield break;

        switch (currentAttack)
        {
            case AttackType.Big:
                if (chargeVFX != null) chargeVFX.SetActive(false);
                if (dragonAnim != null) dragonAnim.speed = 1f;
                if (bigFireballPrefab != null) Instantiate(bigFireballPrefab, mouthPosition.position, Quaternion.identity);
                break;

            case AttackType.Single:
                float randZ = Random.Range(-spreadAngle, spreadAngle);
                if (normalFireballPrefab != null) Instantiate(normalFireballPrefab, mouthPosition.position, Quaternion.Euler(0, 0, randZ));
                break;

            case AttackType.Rapid:
                for (int i = 0; i < 5; i++)
                {
                    float rZ = Random.Range(-spreadAngle, spreadAngle);
                    if (normalFireballPrefab != null) Instantiate(normalFireballPrefab, mouthPosition.position, Quaternion.Euler(0, 0, rZ));
                    yield return new WaitForSeconds(0.15f);
                }
                break;

            case AttackType.Spread:
                if (normalFireballPrefab != null)
                {
                    Instantiate(normalFireballPrefab, mouthPosition.position, Quaternion.Euler(0, 0, spreadAngle)); 
                    Instantiate(normalFireballPrefab, mouthPosition.position, Quaternion.identity);                 
                    Instantiate(normalFireballPrefab, mouthPosition.position, Quaternion.Euler(0, 0, -spreadAngle));
                }
                break;
        }
    }

    // ==========================================
    // LOGIC BAY
    // ==========================================
    private IEnumerator FlyOffScreenRoutine()
    {
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
        yield return new WaitForSeconds(timeOffScreen);

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
        isBusy = false;
    }
}
