using UnityEngine;
using System.Collections.Generic;

public class BossPhase2Controller : MonoBehaviour
{
    public Animator bossAnim;
    public List<GameObject> attackVFXList;
    
    [Header("Settings")]
    public float baseTimeBetweenAttacks = 60f; // Cân bằng APM: Tăng từ 30s lên 60s
    public float minTimeBetweenAttacks = 30f;  // Cân bằng APM: Tăng từ 10s lên 30s
    public Transform suckTargetPos; // Kéo object tâm điểm hút vào đây
    public List<MineShaft> allShafts; // Kéo toàn bộ 10 hầm mỏ vào đây theo thứ tự từ 1 đến 10

    [Header("Damage Settings")]
    [SerializeField] public float dotDamage = 4f; // Sát thương mỗi lần nhảy
    [SerializeField] public float dotInterval = 1f; // Thời gian nhảy sát thương (giây)
    
    private float attackTimer;

    private void Start()
    {
        if (Gamemanager.Instance != null && Gamemanager.Instance.CurrentRound != 2)
        {
            this.enabled = false; // Tắt script này vì không phải Round 2
            
            // Nếu là Round 1 thì ẩn luôn cả hình ảnh Boss đi (vì cả 2 Boss đều chưa xuất hiện)
            if (Gamemanager.Instance.CurrentRound == 1) 
            {
                Debug.Log($"[BossPhase2] Round 1 -> Ẩn Boss GameObject: {gameObject.name}");
                gameObject.SetActive(false); 
            }
            return;
        }

        Debug.Log($"<color=yellow>[BossPhase2] Round 2 -> BẬT BOSS THÀNH CÔNG! Object: {gameObject.name} đang hiển thị!</color>");

        // TẮT cơ chế Click trừ máu ở Round 2 (Để Boss thành Hiểm họa vĩnh cửu)
        EnemyClickReceiver clicker = GetComponent<EnemyClickReceiver>();
        if (clicker != null) clicker.enabled = false;

        attackTimer = baseTimeBetweenAttacks;
        if (bossAnim != null) bossAnim.Play("idle"); // Ép về idle lúc mới vào game
    }

    private void Update()
    {
        if (allShafts == null || allShafts.Count == 0) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            AttackRandomWorker();
            
            // Tính toán lại Cooldown dựa vào tổng cấp độ hầm
            int totalLevel = 0;
            foreach (var shaft in allShafts)
            {
                if (shaft != null && shaft.gameObject.activeInHierarchy) totalLevel += shaft.Level;
            }
            float newCooldown = baseTimeBetweenAttacks - (totalLevel / 100f);
            attackTimer = Mathf.Max(newCooldown, minTimeBetweenAttacks);
        }
    }

    private void AttackRandomWorker()
    {
        // 1. Lọc ra các hầm hợp lệ (đã mở khóa và có thợ mỏ đang làm việc)
        List<MineShaft> validShafts = new List<MineShaft>();
        foreach (var shaft in allShafts)
        {
            if (shaft == null || !shaft.gameObject.activeInHierarchy || shaft.activeMiners == null) continue;

            ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>(false);
            if (unlocker != null && unlocker.gameObject.activeInHierarchy) continue; // Bị khóa

            if (shaft.isInvincible) continue; // Bỏ qua hầm đang có khiên bất tử

            bool hasLivingMiner = false;
            foreach (var m in shaft.activeMiners)
            {
                if (m.healthState != Miner.HealthState.Injured && m.healthState != Miner.HealthState.Dead)
                {
                    hasLivingMiner = true;
                    break;
                }
            }

            if (hasLivingMiner) validShafts.Add(shaft);
        }

        // Nếu tất cả các hầm đều trống hoặc bị khóa -> Boss đứng chơi
        if (validShafts.Count == 0) return;

        // 2. Thuật toán chọn hầm ngẫu nhiên có trọng số (Weighted Random)
        // Ưu tiên cao nhất cho hầm đầu tiên trong danh sách validShafts, giảm dần về cuối.
        float totalWeight = 0;
        List<float> cumulativeWeights = new List<float>();

        for (int i = 0; i < validShafts.Count; i++)
        {
            // Điểm số thuận: Hầm càng sâu (i càng lớn) điểm ưu tiên càng cao.
            // Điều này là BẮT BUỘC trong lý thuyết Game Design: Boss phải tấn công vào nơi đẻ ra nhiều tiền nhất của người chơi.
            float baseScore = i + 1; 
            
            // Dùng số mũ 1.5 để tạo độ dốc (Ví dụ: hầm 10 tỉ lệ sẽ bị đánh cao gấp nhiều lần hầm 1, nhưng hầm 1 vẫn > 0)
            float weight = Mathf.Pow(baseScore, 1.5f); 
            
            totalWeight += weight;
            cumulativeWeights.Add(totalWeight);
        }

        // Đổ xí ngầu
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

        // 3. Chọn 1 thợ mỏ ngẫu nhiên trong hầm vừa được chọn
        List<Miner> validMiners = new List<Miner>();
        foreach (var m in chosenShaft.activeMiners)
        {
            if (m.healthState != Miner.HealthState.Injured && m.healthState != Miner.HealthState.Dead) 
                validMiners.Add(m);
        }
        
        if (validMiners.Count == 0) return;

        // 4. Kích hoạt ngẫu nhiên 1 Animation đánh
        bool useAttack2 = Random.value > 0.5f;
        if (bossAnim != null)
        {
            bossAnim.SetTrigger(useAttack2 ? "attack02" : "attack");
        }

        // 5. Tắt TẤT CẢ các VFX trước, sau đó mới chọn 1 cái để bật lên
        if (attackVFXList != null && attackVFXList.Count > 0)
        {
            List<GameObject> validVFXs = new List<GameObject>();
            foreach(var vfx in attackVFXList)
            {
                if (vfx != null) 
                {
                    vfx.SetActive(false);
                    validVFXs.Add(vfx); // Chỉ lấy những ô có chứa VFX (loại bỏ ô trống)
                }
            }

            if (validVFXs.Count > 0)
            {
                int rndIndex = Random.Range(0, validVFXs.Count);
                GameObject chosenVfx = validVFXs[rndIndex];
                chosenVfx.SetActive(true);

                // Ép các Particle System khởi động lại từ đầu (phòng ngừa lỗi Unity không tự Play khi bật tắt quá nhanh)
                ParticleSystem[] pss = chosenVfx.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in pss)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                }
            }
        }

        // 4. Chọn 1 thợ mỏ ngẫu nhiên để ám
        Miner target = validMiners[Random.Range(0, validMiners.Count)];
        target.ApplyHaunt(this);
    }
}
