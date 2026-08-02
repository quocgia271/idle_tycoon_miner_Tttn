using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MissionItemUI quản lý hiển thị cho 1 hàng nhiệm vụ đơn (sử dụng ảnh Bar.png)
/// Tìm kiếm đệ quy tất cả các con/cháu để cập nhật dữ liệu chuẩn 100%
/// </summary>
public class MissionItemUI : MonoBehaviour
{
    [Header("UI References (Kéo đúng ô trong Inspector của Prefab)")]
    public Image backgroundImage;        // Gán ảnh Bar.png
    public Image typeIconImage;          // Icon loại nhiệm vụ ở ô tròn bên trái
    
    [Header("Text Tên Nhiệm Vụ (Khung giữa)")]
    public TextMeshProUGUI titleText;    // Tên nhiệm vụ (TMP)
    public Text legacyTitleText;          // Tên nhiệm vụ (Legacy UI Text)

    [Header("Text Tiến Độ (Phía bên phải khung giữa)")]
    public TextMeshProUGUI progressText; // Tiến độ (TMP)
    public Text legacyProgressText;      // Tiến độ (Legacy UI Text)

    [Header("Dấu Tích Hoàn Thành (Ô tre bên phải)")]
    public Image checkmarkImage;        // Dấu tích dạng Image
    public TextMeshProUGUI checkmarkTMP;// Dấu tích dạng TextTMP ✅
    public Text legacyCheckmarkText;     // Dấu tích dạng Text Legacy ✅

    [Header("Progress Bar (Optional)")]
    public Slider progressBar;           // Thanh tiến độ (nếu có)

    public void Setup(MissionData data, Sprite typeIcon = null)
    {
        if (data == null) return;

        string titleStr = data.title;
        string progressStr = data.isCompleted ? "<color=green>HOÀN THÀNH</color>" : $"[{data.currentAmount}/{data.targetAmount}]";

        // 0. TRIỆT TIÊU 100% Ô TRẮNG ĐỤC VÀ HIỂN THỊ THANH GỖ BAR.PNG RỰC RỠ:
        // A. Tắt ô vuông trắng đục rỗng mặc định nằm ở gốc root Prefab
        Image rootImage = GetComponent<Image>();
        if (rootImage != null && rootImage.sprite == null)
        {
            rootImage.enabled = false; 
        }

        // B. Ép hiển thị mượt mà 100% cho thanh gỗ Bar.png ở child "Bar" hoặc backgroundImage
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.color = Color.white;
        }
        Transform barChild = transform.Find("Bar");
        if (barChild == null) barChild = transform.Find("Background");

        if (barChild != null)
        {
            Image barImg = barChild.GetComponent<Image>();
            if (barImg != null)
            {
                barImg.enabled = true;
                barImg.color = Color.white;
                barImg.preserveAspect = false; // TẮT BẮT BUỘC PRESERVE ASPECT ĐỂ CHO PHÉP KÉO BỰ DÀY GẤP ĐÔI!
            }

            // ÉP CHIỀU CAO THẠNH GỖ BAR.PNG BỰ GẤP ĐÔI (140PX)
            RectTransform barRect = barChild.GetComponent<RectTransform>();
            if (barRect != null)
            {
                barRect.anchorMin = new Vector2(0f, 0f);
                barRect.anchorMax = new Vector2(1f, 1f);
                barRect.offsetMin = Vector2.zero;
                barRect.offsetMax = Vector2.zero;
                barRect.sizeDelta = new Vector2(0f, 140f);
            }
        }

        // 1. CẬP NHẬT TÊN NHIỆM VỤ (Tự động co giãn chữ để KHÔNG BAO GIỜ TRÀN KHUNG)
        if (titleText != null) 
        { 
            titleText.text = titleStr; 
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 10;
            titleText.fontSizeMax = 18;
        }
        if (legacyTitleText != null) 
        { 
            legacyTitleText.text = titleStr; 
            legacyTitleText.resizeTextForBestFit = true;
            legacyTitleText.resizeTextMinSize = 9;
            legacyTitleText.resizeTextMaxSize = 18;
        }
        if (progressText != null) progressText.text = progressStr;
        if (legacyProgressText != null) legacyProgressText.text = progressStr;

        // 2. TỰ ĐỘNG LỌC VÀ TÌM KIẾM ĐỆ QUY TẤT CẢ OBJECT CON/CHÁU
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t == this.transform) continue;

            string nameLower = t.name.ToLower();

            // A. CẬP NHẬT TÊN NHIỆM VỤ (Ví dụ: TitleText, Title, TenNhiemVu)
            if (nameLower.Contains("title") || nameLower.Contains("ten") || nameLower.Contains("nhiemvu"))
            {
                var tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = titleStr;

                var txt = t.GetComponent<Text>();
                if (txt != null) txt.text = titleStr;
            }
            // B. CẬP NHẬT TIẾN ĐỘ (Ví dụ: ProgressText, Progress, TienDo)
            else if (nameLower.Contains("progress") || nameLower.Contains("tiendo"))
            {
                var tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = progressStr;

                var txt = t.GetComponent<Text>();
                if (txt != null) txt.text = progressStr;
            }
            // C. CẬP NHẬT ICON BÊN TRÁI
            else if (nameLower.Contains("icon") || nameLower.Contains("typeicon"))
            {
                var img = t.GetComponent<Image>();
                if (img != null)
                {
                    if (typeIcon != null)
                    {
                        img.sprite = typeIcon;
                        img.gameObject.SetActive(true);
                    }
                    else
                    {
                        img.gameObject.SetActive(false); // Ẩn ô trắng đục
                    }
                }
            }
            // D. CẬP NHẬT DẤU TÍCH BÊN PHẢI
            else if (nameLower.Contains("check") || nameLower.Contains("tich"))
            {
                t.gameObject.SetActive(data.isCompleted);
            }
        }

        // 3. PROGRESS BAR
        if (progressBar != null)
        {
            progressBar.maxValue = data.targetAmount;
            progressBar.value = data.currentAmount;
        }

        // 4. ICON BÊN TRÁI (Nếu đã gán)
        if (typeIconImage != null)
        {
            if (typeIcon != null)
            {
                typeIconImage.sprite = typeIcon;
                typeIconImage.gameObject.SetActive(true);
            }
            else
            {
                typeIconImage.gameObject.SetActive(false);
            }
        }

        // 5. CHECKMARK (Nếu đã gán)
        if (checkmarkImage != null) checkmarkImage.gameObject.SetActive(data.isCompleted);
        if (checkmarkTMP != null) checkmarkTMP.gameObject.SetActive(data.isCompleted);
        if (legacyCheckmarkText != null) legacyCheckmarkText.gameObject.SetActive(data.isCompleted);
    }
}
