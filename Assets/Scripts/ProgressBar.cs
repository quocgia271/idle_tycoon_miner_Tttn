using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [Header("UI References")]
    public Image fillImage; // Dùng nếu bạn xài Image (Filled)
    public Slider slider;   // Dùng nếu bạn xài Slider

    private float duration;
    private float currentTime;
    private bool isFilling = false;

    private void Start()
    {
        // Tự động ẩn đi khi game vừa bắt đầu
        gameObject.SetActive(false); 
    }

    // Bất kỳ script nào cũng có thể gọi hàm này để chạy loading bar
    public void StartLoading(float timeToFill)
    {
        // Tránh lỗi chia cho 0 nếu lỡ truyền số <= 0 vào
        if (timeToFill <= 0f) timeToFill = 0.01f;
        
        duration = timeToFill;
        currentTime = 0f;
        
        if (fillImage != null) fillImage.fillAmount = 0f;
        if (slider != null) 
        {
            slider.maxValue = duration;
            slider.value = 0f;
        }
        
        gameObject.SetActive(true);
        isFilling = true;
    }

    // Hàm gọi thủ công nếu muốn ép dừng loading giữa chừng
    public void StopLoading()
    {
        isFilling = false;
        gameObject.SetActive(false);
    }

    // Cập nhật thanh đầy theo phần trăm tĩnh (dùng cho sức chứa, không chạy theo thời gian)
    public void SetProgress(float percentage)
    {
        isFilling = false; // Tắt bộ đếm thời gian đi
        gameObject.SetActive(true); // Đảm bảo thanh này luôn hiện
        
        if (fillImage != null) fillImage.fillAmount = percentage;
        if (slider != null) 
        {
            slider.maxValue = 1f;
            slider.value = percentage;
        }
    }

    private void Update()
    {
        if (isFilling)
        {
            currentTime += Time.deltaTime;
            
            if (fillImage != null) fillImage.fillAmount = currentTime / duration;
            if (slider != null) slider.value = currentTime;

            // Tự động ẩn đi khi thanh đã chạy đầy
            if (currentTime >= duration)
            {
                isFilling = false;
                gameObject.SetActive(false); 
            }
        }
    }
}
