using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LeaderboardItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public Image backgroundImage;
    
    public void Setup(int rank, string playerName, double score, bool isPlayer)
    {
        if (rankText != null) rankText.text = "#" + rank.ToString();
        if (nameText != null) nameText.text = playerName;
        
        // Sử dụng CurrencyFormatter có sẵn của game để format tiền (điểm)
        if (scoreText != null) scoreText.text = CurrencyFormatter.FormatMoney(score);

        // Highlight nếu đây là dòng của người chơi
        if (isPlayer)
        {
            if (backgroundImage != null) backgroundImage.color = new Color(0.2f, 0.8f, 0.2f, 0.6f); // Xanh lá mờ cho nền
            if (nameText != null) nameText.color = Color.yellow;
            if (scoreText != null) scoreText.color = Color.yellow;
            if (rankText != null) rankText.color = Color.yellow;
        }
    }
}
