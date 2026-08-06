using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class Elevator : Facility
{
    public override FacilityType GetFacilityType() => FacilityType.Elevator;

    public enum ElevatorState
    {
        Idle,
        MovingDown,
        Loading,
        MovingUp,
        Unloading
    }

    [Header("Elevator Settings")]
    public float moveSpeed = 5f;
    public float loadTime = 1f; 
    public float unloadTime = 1f; 

    [Header("Positions & Targets")]
    public Transform startPos; // PHẢI KÉO 1 EMPTY GAMEOBJECT NẰM Ở TRÊN CÙNG VÀO ĐÂY
    public List<MineShaft> shafts; 

    [Header("UI")]
    public ProgressBar progressBar; // Thanh hiển thị thời gian load
    public TextMeshProUGUI droppedResourceText; // Hiển thị số tiền xả ra
    public ProgressBar capacityBar; // Thanh UI hiển thị khối lượng chứa
    public TextMeshProUGUI currentLoadText; // Hiển thị tiền trên thang máy (khối đi lên xuống)

    [Header("Data")]
    public double CurrentLoad = 0; 
    public double DroppedResource = 0; 
    
    public double BaseCapacity = 50; 
    
    public override double GetCapacity(int targetLevel)
    {
        double baseCap = BaseCapacity * System.Math.Pow(1.1f, targetLevel - 1);
        
        // --- CHUẨN GAME DESIGN: MILESTONE JUMPS ---
        // Cơ chế bùng nổ sức chứa tại các mốc Level chẵn để bắt kịp sản lượng của Hầm mới.
        double milestoneMult = 1.0;
        if (targetLevel >= 10) milestoneMult *= 2;
        if (targetLevel >= 25) milestoneMult *= 2;
        if (targetLevel >= 50) milestoneMult *= 3;
        if (targetLevel >= 100) milestoneMult *= 4;
        if (targetLevel >= 200) milestoneMult *= 5;
        if (targetLevel >= 300) milestoneMult *= 5;
        if (targetLevel >= 400) milestoneMult *= 10;
        if (targetLevel >= 500) milestoneMult *= 10;
        
        baseCap *= milestoneMult;
        // ------------------------------------------
        
        double prestigeMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.PrestigeMultiplier : 1.0;
        double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
        return baseCap * prestigeMultiplier * roundMultiplier;
    }

    public double Capacity => GetCapacity(Level);

    private ElevatorState currentState = ElevatorState.Idle;
    private int currentShaftIndex = 0;
    private float currentTimer = 0f;
    private MineShaft targetShaft = null; // Hầm mục tiêu tiếp theo

    public float ElevatorMoveSpeedBuff = 1f;
    public float ElevatorLoadSpeedBuff = 1f;

    [Header("Boss VFX")]
    public GameObject slowVFX; // Kéo thả VFX dư âm làm chậm vào đây

    protected override void Start()
    {
        base.Start(); 
        ScanForShafts(); // Tự động quét hầm ngay khi mở game
        UpdateElevatorUI(); // Cập nhật UI lúc mới vào game
    }

    private void UpdateElevatorUI()
    {
        if (droppedResourceText != null)
        {
            droppedResourceText.text = CurrencyFormatter.FormatMoney(DroppedResource);
        }
        
        // Cập nhật thanh hiển thị khối lượng của thang máy bằng ProgressBar tĩnh
        if (capacityBar != null)
        {
            capacityBar.SetProgress((float)(CurrentLoad / Capacity));
        }

        if (currentLoadText != null)
        {
            if (CurrentLoad > 0)
            {
                currentLoadText.gameObject.SetActive(true);
                currentLoadText.text = CurrencyFormatter.FormatMoney(CurrentLoad);
            }
            else
            {
                currentLoadText.gameObject.SetActive(false);
            }
        }
    }

    // Hàm tự động tìm và sắp xếp tất cả các hầm mỏ
    public void ScanForShafts()
    {
        // Tìm tất cả khối MineShaft trong game
        MineShaft[] foundShafts = FindObjectsOfType<MineShaft>();
        
        // Sắp xếp theo thứ tự trục Y giảm dần (hầm ở trên cao sẽ đứng trước hầm dưới sâu)
        shafts = foundShafts.OrderByDescending(s => s.transform.position.y).ToList();
        
        Debug.Log($"Thang máy tự động nhận diện {shafts.Count} hầm.");
    }

    protected override void Update()
    {
        base.Update();
        
        switch (currentState)
        {
            case ElevatorState.Idle:
                // Thang máy thông minh: Chỉ bắt đầu đi làm khi có ít nhất 1 hầm có tiền VÀ có quản lý
                if (currentManager != null && HasAnyMoneyInShafts())
                {
                    currentShaftIndex = 0;
                    FindNextTargetShaft(); 
                }
                break;

            case ElevatorState.MovingDown:
                HandleMovingDown();
                break;

            case ElevatorState.Loading:
                HandleLoading();
                break;

            case ElevatorState.MovingUp:
                HandleMovingUp();
                break;

            case ElevatorState.Unloading:
                HandleUnloading();
                break;
        }
    }
    
    // Kiểm tra xem toàn bộ khu mỏ có đồng nào không
    private bool HasAnyMoneyInShafts()
    {
        foreach(var shaft in shafts)
        {
            if (shaft != null && shaft.CurrentResource > 0) return true;
        }
        return false;
    }

    // Tìm hầm tiếp theo có tiền, bỏ qua các hầm rỗng
    private void FindNextTargetShaft()
    {
        targetShaft = null;
        
        while (currentShaftIndex < shafts.Count)
        {
            // Nếu hầm này có tiền, chọn làm mục tiêu và ngắt vòng lặp
            if (shafts[currentShaftIndex] != null && shafts[currentShaftIndex].CurrentResource > 0)
            {
                targetShaft = shafts[currentShaftIndex];
                break;
            }
            currentShaftIndex++; // Nếu không có tiền, bỏ qua hầm này
        }

        if (targetShaft != null)
        {
            ChangeState(ElevatorState.MovingDown);
        }
        else
        {
            // Hết hầm có tiền (hoặc đã quét qua hết) -> Đi lên
            ChangeState(ElevatorState.MovingUp);
        }
    }

    private void HandleMovingDown()
    {
        if (targetShaft == null)
        {
            ChangeState(ElevatorState.MovingUp);
            return;
        }

        Vector3 targetPos = new Vector3(transform.position.x, targetShaft.transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * ElevatorMoveSpeedBuff * Time.deltaTime);

        // Tới hầm mục tiêu
        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            ChangeState(ElevatorState.Loading);
        }
    }

    private void HandleLoading()
    {
        currentTimer += Time.deltaTime * ElevatorLoadSpeedBuff;
        
        if (currentTimer >= loadTime)
        {
            if (targetShaft != null)
            {
                if (IsSkillActive && currentManager != null && currentManager.SpecialFeature == SeniorSpecialFeature.SpecialFeature)
                {
                    ActivateSpecialBirds(targetShaft);
                }

                double spaceLeft = Capacity - CurrentLoad;
                double collected = targetShaft.TakeResource(spaceLeft);
                CurrentLoad += collected;
                
                UpdateElevatorUI(); // Cập nhật thanh độ đầy khi vừa ăn tiền
            }

            currentShaftIndex++; // Chuẩn bị xét hầm kế tiếp
            
            // Nếu đầy thùng thì đi lên luôn, khỏi tìm hầm khác
            if (CurrentLoad >= Capacity)
            {
                ChangeState(ElevatorState.MovingUp);
            }
            else
            {
                // Nếu chưa đầy, tiếp tục đi tìm hầm có tiền
                FindNextTargetShaft();
            }
        }
    }

    private void ActivateSpecialBirds(MineShaft shaft)
    {
        // Bỏ qua nếu hầm này đang được kích hoạt kỹ năng bởi Quản lý Cấp cao
        if (shaft.currentManager != null && shaft.currentManager.Rarity == ManagerRarity.Senior && shaft.currentManager.IsSkillActive())
        {
            return;
        }

        // 1. Heal Bird logic (Priority)
        bool needsHeal = shaft.IsSkill3Active;
        if (!needsHeal)
        {
            foreach (var miner in shaft.activeMiners)
            {
                WorkerHealth wh = miner != null ? miner.GetComponent<WorkerHealth>() : null;
                if (wh != null && wh.healthState == WorkerHealth.HealthState.Injured)
                {
                    needsHeal = true;
                    break;
                }
            }
        }

        if (needsHeal)
        {
            shaft.TriggerHealBird();
            return; // Only 1 bird at a time
        }

        // 2. Attack Bird logic
        MinionController targetMinion = null;
        MinionController[] minions = shaft.GetComponentsInChildren<MinionController>(true);
        foreach (var m in minions)
        {
            if (m.gameObject.activeInHierarchy && !m.IsDead)
            {
                targetMinion = m;
                break;
            }
        }

        if (targetMinion != null)
        {
            shaft.TriggerAttackBird(targetMinion);
        }
    }

    private void HandleMovingUp()
    {
        if (startPos == null) return;

        // Đi về vị trí StartPos
        Vector3 targetPos = new Vector3(transform.position.x, startPos.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * ElevatorMoveSpeedBuff * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            // Về tới đỉnh
            if (CurrentLoad > 0)
            {
                ChangeState(ElevatorState.Unloading);
            }
            else
            {
                // Đi lên tay không thì nghỉ luôn
                ChangeState(ElevatorState.Idle);
            }
        }
    }

    private void HandleUnloading()
    {
        currentTimer += Time.deltaTime * ElevatorLoadSpeedBuff;
        
        if (currentTimer >= unloadTime)
        {
            DroppedResource += CurrentLoad;
            CurrentLoad = 0;
            
            UpdateElevatorUI(); // Bơm tiền xong thì cập nhật UI liền
            ChangeState(ElevatorState.Idle);
        }
    }

    private void ChangeState(ElevatorState newState)
    {
        currentState = newState;
        currentTimer = 0f; 

        if (newState == ElevatorState.Loading && progressBar != null)
        {
            progressBar.StartLoading(loadTime / ElevatorLoadSpeedBuff);
        }
        else if (newState == ElevatorState.Unloading && progressBar != null)
        {
            progressBar.StartLoading(unloadTime / ElevatorLoadSpeedBuff);
        }
    }

    public double TakeResource(double amountToTake)
    {
        double taken = 0;
        if (amountToTake > DroppedResource)
        {
            taken = DroppedResource;
            DroppedResource = 0;
        }
        else
        {
            DroppedResource -= amountToTake;
            taken = amountToTake;
        }
        
        UpdateElevatorUI(); // Bị lấy tiền đi cũng phải cập nhật lại UI
        return taken;
    }

    // Đồng bộ công thức tốc độ với giao diện UI (Facility.cs) và áp dụng giới hạn Max Speed
    public override float GetSpeed(int targetLevel)
    {
        float baseS = Config != null ? Config.BaseSpeed : 5f;
        float speed = baseS + ((targetLevel - 1) * 0.2f);
        return Mathf.Min(speed, 15f); // Khóa tốc độ tối đa ở mức 15 để tránh lỗi xuyên tường
    }

    protected override void OnUpgraded()
    {
        moveSpeed = GetSpeed(Level);
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (currentState == ElevatorState.Idle && HasAnyMoneyInShafts())
        {
            currentShaftIndex = 0;
            FindNextTargetShaft();
        }
    }

    protected override void ApplyManagerBuff()
    {
        base.ApplyManagerBuff();
        if (currentManager == null) return;

        float buffMultiplier = 1f + (currentManager.BuffValue / 100f);
        
        switch (currentManager.BuffType)
        {
            case ManagerBuffType.MoveSpeed:
                ElevatorMoveSpeedBuff = buffMultiplier;
                break;
            case ManagerBuffType.MiningSpeed:
                ElevatorLoadSpeedBuff = buffMultiplier;
                break;
        }
    }

    protected override void RemoveManagerBuff()
    {
        base.RemoveManagerBuff();
        ElevatorMoveSpeedBuff = 1f;
        ElevatorLoadSpeedBuff = 1f;
    }

    // =====================================
    // LƯU TRỮ VÀ TẢI DỮ LIỆU (SAVE/LOAD)
    // =====================================
    public override FacilitySaveData SaveState()
    {
        FacilitySaveData data = base.SaveState();
        data.Index = 0; // Elevator không cần Index
        data.CurrentResource = this.DroppedResource; 
        return data;
    }

    public override void LoadState(FacilitySaveData data)
    {
        base.LoadState(data);
        if (data == null) return;
        this.DroppedResource = data.CurrentResource;
        UpdateElevatorUI();
    }

    public override void PopulateSaveData(SaveData data)
    {
        data.Elevator = this.SaveState();
    }

    public override void LoadFromSaveData(SaveData data)
    {
        if (data.Elevator != null)
        {
            this.LoadState(data.Elevator);
        }
    }
}
