using UnityEngine;
using System;

public class Gamemanager : MonoBehaviour
{
    public static Gamemanager Instance;
    public double IdleCash = 0;

    // Tạo một sự kiện (Event) báo hiệu khi tiền thay đổi
    public Action<double> OnCashChanged;

    void Awake()
    {
        // Setup Singleton
        if (Instance == null) 
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddCash(double amount)
    {
        IdleCash += amount;
        Debug.Log("IdleCash: " + IdleCash); // In ra console để dễ test
        
        // Phát sự kiện cho bất kỳ Script UI nào đang lắng nghe
        OnCashChanged?.Invoke(IdleCash);
    }
}
