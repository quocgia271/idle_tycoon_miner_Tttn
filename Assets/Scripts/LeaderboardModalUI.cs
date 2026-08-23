using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class PlayerData
{
    public string Name;
    public double Score;
    public bool IsPlayer;
}

public class LeaderboardModalUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject itemPrefab;
    public Transform contentParent;

    [Header("Bot Generation Config")]
    public int totalBots = 49; 
    private readonly string[] botNames = { "Player", "Guest", "User", "Miner", "Digger", "Explorer", "Tycoon", "Worker", "Gamer", "Builder", "Collector", "Hunter" };

    // Mốc thời gian vũ trụ bắt đầu (dùng để tính thời gian trôi qua cho bot cày tiền)
    private readonly DateTime epochStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private void OnEnable()
    {
        GenerateMockLeaderboard();
    }

    public void GenerateMockLeaderboard()
    {
        // 1. Xóa danh sách UI cũ
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Lấy thông tin người chơi
        double currentCash = 0;
        int currentRound = 1;
        double roundMultiplier = 1;
        double prestigeMultiplier = 1; // Thêm biến lưu hệ số Prestige

        if (Gamemanager.Instance != null)
        {
            currentCash = Gamemanager.Instance.LifetimeCash;
            currentRound = Gamemanager.Instance.CurrentRound;
            roundMultiplier = Gamemanager.Instance.RoundMultiplier;
            
            // Lấy hệ số Prestige hiện tại của người chơi
            // (Đã thấy thuộc tính này trong file MineShaft.cs)
            prestigeMultiplier = Gamemanager.Instance.PrestigeMultiplier; 
        }
        
        // --- LOGIC: LƯU GIỮ KỶ LỤC DOANH THU CAO NHẤT (ALL-TIME HIGH SCORE) ---
        // Giúp người chơi không bị rớt hạng khi qua Round mới (tiền bị reset)
        string savedMaxStr = PlayerPrefs.GetString("PlayerMaxScore", "");
        double maxPlayerScore = 0;
        if (!string.IsNullOrEmpty(savedMaxStr)) double.TryParse(savedMaxStr, out maxPlayerScore);
        
        // Nếu tiền hiện tại cao hơn kỷ lục cũ, cập nhật kỷ lục mới
        if (currentCash > maxPlayerScore)
        {
            maxPlayerScore = currentCash;
            PlayerPrefs.SetString("PlayerMaxScore", maxPlayerScore.ToString("R"));
        }
        
        // Điểm mang đi thi đấu trên bảng xếp hạng là Điểm Kỷ Lục
        double leaderboardPlayerScore = maxPlayerScore;

        List<PlayerData> playersList = new List<PlayerData>
        {
            new PlayerData { Name = "You", Score = leaderboardPlayerScore, IsPlayer = true }
        };

        // YÊU CẦU 1: BOT XUYÊN SUỐT MÃI MÃI, KHÔNG ĐỔI TÊN
        // Cố định Seed vĩnh viễn (ví dụ 12345), bỏ '+ currentRound' đi. 
        // Đảm bảo 49 con bot được sinh ra sẽ giữ nguyên danh tính suốt 3 vòng.
        UnityEngine.Random.InitState(12345);

        double prestigeRequirement = (Gamemanager.Instance != null && Gamemanager.Instance.GlobalConfig != null ? Gamemanager.Instance.GlobalConfig.BasePrestigeRequirement : 1000000) * roundMultiplier; 

        // TÍNH TOÁN THỜI GIAN NGƯỜI CHƠI ĐÃ OFFLINE (TÍNH TỪ LẦN MỞ BẢNG XẾP HẠNG TRƯỚC)
        string lastOpenStr = PlayerPrefs.GetString("LastLeaderboardOpen", "");
        DateTime lastOpenTime = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(lastOpenStr))
        {
            DateTime.TryParse(lastOpenStr, out lastOpenTime);
        }
        
        double hoursSinceLastOpen = (DateTime.UtcNow - lastOpenTime).TotalHours;
        // Chặn cheat time: Giới hạn tối đa vắng mặt 30 ngày (720 giờ) cho mỗi lần mở
        if (hoursSinceLastOpen < 0 || hoursSinceLastOpen > 720) hoursSinceLastOpen = 0; 
        
        PlayerPrefs.SetString("LastLeaderboardOpen", DateTime.UtcNow.ToString("O"));

        for (int i = 0; i < totalBots; i++)
        {
            // Tên bot sẽ vĩnh viễn giống nhau nhờ khóa Seed ở trên
            string botName = botNames[UnityEngine.Random.Range(0, botNames.Length)] + "_" + UnityEngine.Random.Range(10, 999);
            
            // Sức mạnh của bot (Random từ 0.1 đến 2.0)
            float botSkill = UnityEngine.Random.Range(0.1f, 2.0f);
            
            // YÊU CẦU 2: LƯU LẠI QUÁ TRÌNH CỦA BOT BẰNG PLAYERPREFS (GIỐNG JSON)
            string botKey = "BotScore_" + botName;
            string savedScoreStr = PlayerPrefs.GetString(botKey, "");
            double currentBotScore = 0;
            
            if (string.IsNullOrEmpty(savedScoreStr) || !double.TryParse(savedScoreStr, out currentBotScore) || currentBotScore <= 0)
            {
                // Lần đầu tiên game chạy (hoặc khi file save bị lỗi điểm 0): 
                // Cấp vốn khởi nghiệp cho bot đảm bảo từ 1% đến 150% của mốc qua màn
                // Nghĩa là bét nhất bot cũng có 10.000 vàng, đảm bảo người chơi 0 vàng luôn chắc suất Hạng 50.
                double botBaseScore = prestigeRequirement * UnityEngine.Random.Range(0.01f, 1.5f);
                double botChaseScore = leaderboardPlayerScore * UnityEngine.Random.Range(0.1f, 1.2f);
                
                currentBotScore = botBaseScore + botChaseScore;
            }
            else
            {
                // YÊU CẦU 3: TRỪNG PHẠT THÊ THẢM NẾU NGƯỜI CHƠI LƯỜI BIẾNG
                // Cơ chế "Rubber-banding" (Dây thun bám đuổi): Nếu điểm người chơi vượt quá xa mốc qua màn (vd: hack tiền),
                // Bot sẽ tự động lấy điểm của người chơi làm mục tiêu mới để bám đuổi.
                double botTargetScore = Math.Max(prestigeRequirement, leaderboardPlayerScore);
                
                // Tốc độ cày của bot: Đuổi kịp mục tiêu trong vòng 20 giờ (nhân với hệ số kỹ năng và hệ số Prestige)
                double botIncomePerHour = (botTargetScore / 20f) * botSkill * prestigeMultiplier;
                
                // Bot cày 100% thời gian thực (ví dụ 48 giờ) trong khi người chơi chỉ nhận 4 giờ offline
                currentBotScore += (botIncomePerHour * hoursSinceLastOpen);
            }
            
            // Lưu lại điểm mới của bot dưới dạng string để tránh mất độ chính xác của double
            PlayerPrefs.SetString(botKey, currentBotScore.ToString("R"));

            playersList.Add(new PlayerData { Name = botName, Score = currentBotScore, IsPlayer = false });
        }

        // Trả lại Random State
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        PlayerPrefs.Save();

        // Sắp xếp danh sách giảm dần theo điểm
        playersList = playersList.OrderByDescending(p => p.Score).ToList();

        // Khởi tạo UI
        for (int i = 0; i < playersList.Count; i++)
        {
            GameObject go = Instantiate(itemPrefab, contentParent);
            LeaderboardItemUI itemUI = go.GetComponent<LeaderboardItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(i + 1, playersList[i].Name, playersList[i].Score, playersList[i].IsPlayer);
            }
        }
    }
}
