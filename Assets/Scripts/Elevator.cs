using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class Elevator : Facility
{
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

    [Header("Data")]
    public double CurrentLoad = 0; 
    public double DroppedResource = 0; 
    
    public double BaseCapacity = 50; 
    public double Capacity => BaseCapacity * Level; 

    private ElevatorState currentState = ElevatorState.Idle;
    private int currentShaftIndex = 0;
    private float currentTimer = 0f;
    private MineShaft targetShaft = null; // Hầm mục tiêu tiếp theo

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

    private void Update()
    {
        switch (currentState)
        {
            case ElevatorState.Idle:
                // Thang máy thông minh: Chỉ bắt đầu đi làm khi có ít nhất 1 hầm có tiền
                if (HasAnyMoneyInShafts())
                {
                    currentShaftIndex = 0;
                    FindNextTargetShaft(); // Bắt đầu tìm hầm có tiền để đi tới
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
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // Tới hầm mục tiêu
        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            ChangeState(ElevatorState.Loading);
        }
    }

    private void HandleLoading()
    {
        currentTimer += Time.deltaTime;
        
        if (currentTimer >= loadTime)
        {
            if (targetShaft != null)
            {
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

    private void HandleMovingUp()
    {
        if (startPos == null) return;

        // Đi về vị trí StartPos
        Vector3 targetPos = new Vector3(transform.position.x, startPos.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

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
        currentTimer += Time.deltaTime;
        
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
            progressBar.StartLoading(loadTime);
        }
        else if (newState == ElevatorState.Unloading && progressBar != null)
        {
            progressBar.StartLoading(unloadTime);
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

    protected override void OnUpgraded()
    {
        moveSpeed += 0.2f;
    }
}
