using UnityEngine;
using TMPro;

public class UpgradeStatItemUI : MonoBehaviour
{
    public TextMeshProUGUI StatNameText;
    public TextMeshProUGUI CurrentValueText;
    public TextMeshProUGUI NextValueText;

    // Hàm này gọi để cập nhật thông tin của từng dòng (slot)
    public void Setup(string statName, string currentValue, string nextValue)
    {
        if (StatNameText != null) StatNameText.text = statName;
        if (CurrentValueText != null) CurrentValueText.text = currentValue;
        if (NextValueText != null) NextValueText.text = nextValue;
    }
}
