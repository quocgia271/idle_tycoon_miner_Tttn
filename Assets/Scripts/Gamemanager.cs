using UnityEngine;
using System;

public class Gamemanager : MonoBehaviour
{
    public static Gamemanager Instance;
    public double IdleCash = 0;
    public int PlayerLevel = 1; // Thêm thông số level cho người chơi

    public Action<double> OnCashChanged;
    public Action<int> OnLevelChanged; // Event khi level thay đổi

    void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddCash(double amount)
    {
        IdleCash += amount;
        OnCashChanged?.Invoke(IdleCash);
    }

    public void AddLevel(int amount)
    {
        PlayerLevel += amount;
        OnLevelChanged?.Invoke(PlayerLevel);
    }

    // Hàm trừ tiền an toàn: Trả về true nếu đủ tiền và mua thành công
    public bool DeductCash(double amount)
    {
        if (IdleCash >= amount)
        {
            IdleCash -= amount;
            OnCashChanged?.Invoke(IdleCash);
            return true;
        }
        return false;
    }
}
