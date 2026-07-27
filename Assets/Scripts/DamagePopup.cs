using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class DamagePopup : MonoBehaviour
{
    public TMP_Text textMesh;
    public float floatDistance = 1.5f; // Khoảng cách bay lên
    public float sizeMultiplier = 1.2f; // Hệ số phóng to/thu nhỏ chữ
    
    // Object Pool tĩnh để dùng chung cho mọi nguồn (Boss, Hầm) nhằm tránh giật lag khi sinh chữ liên tục
    private static Queue<DamagePopup> pool = new Queue<DamagePopup>();

    public static DamagePopup Create(DamagePopup prefab, Vector3 position, Transform parent)
    {
        DamagePopup popup = null;
        while (pool.Count > 0 && popup == null)
        {
            popup = pool.Dequeue();
        }

        if (popup != null)
        {
            // Để đảm bảo kích thước luôn chuẩn xác như Prefab gốc bất kể đưa vào Canvas có scale bao nhiêu:
            // 1. Tạm thời tách khỏi parent cũ
            popup.transform.SetParent(null);
            // 2. Phục hồi đúng kích thước gốc của Prefab rồi nhân với hệ số phóng to
            popup.transform.localScale = prefab.transform.localScale * prefab.sizeMultiplier;
            popup.transform.localRotation = prefab.transform.localRotation;
            // 3. Gắn vào parent mới, Unity sẽ tự động tính toán lại localScale cho phù hợp để giữ nguyên size
            popup.transform.SetParent(parent, true);
            popup.transform.position = position;
            popup.gameObject.SetActive(true);
        }
        else
        {
            popup = Instantiate(prefab, position, Quaternion.identity, parent);
            // Phóng to ngay từ lần sinh ra đầu tiên
            popup.transform.localScale = prefab.transform.localScale * prefab.sizeMultiplier;
        }
        return popup;
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false);
        pool.Enqueue(this);
    }
    
    private void Awake()
    {
        // Ẩn text ngay khi vừa được sinh ra (tránh bị chớp nháy chữ mặc định)
        if (textMesh != null)
        {
            textMesh.text = "";
            Color c = textMesh.color;
            c.a = 0f;
            textMesh.color = c;
        }
    }

    public void Setup(float damageAmount, float scaleFactor, Color textColor)
    {
        // Dừng các hiệu ứng cũ nếu được lấy ra từ Pool
        transform.DOKill();
        if (textMesh != null) textMesh.DOKill();

        // Hiển thị số sát thương (ví dụ: -10)
        if (textMesh != null)
        {
            textMesh.text = "-" + damageAmount.ToString("F0");

            // Ép Alpha = 1 phòng trường hợp User quên kéo thanh Alpha trong Inspector của Color
            textColor.a = 1f;
            textMesh.color = textColor;

            // Hiệu ứng mờ dần (Fade out)
            textMesh.DOFade(0f, 1f).SetEase(Ease.InExpo);
        }

        // Tỷ lệ khoảng cách để hầm không bị bay quá xa (scaleFactor)
        // Nâng mức tản tối thiểu lên 0.8 để các chữ tách nhau ra rõ hơn
        float scatterMultiplier = Mathf.Max(scaleFactor, 0.8f);
        float scatterX = Random.Range(-0.3f, 0.3f) * scatterMultiplier;
        float scatterY = Random.Range(-0.2f, 0.2f) * scatterMultiplier;
        transform.position += new Vector3(scatterX, scatterY, 0);

        float distanceToFloat = floatDistance * scaleFactor;

        // Bay lên trên (từ tâm hiện tại) một khoảng distanceToFloat, sau đó đưa trở lại Pool thay vì Xóa
        transform.DOMoveY(transform.position.y + distanceToFloat, 1f).SetEase(Ease.OutCirc)
            .OnComplete(() => ReturnToPool());
    }
}
