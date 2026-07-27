using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class DamagePopup : MonoBehaviour
{
    public TMP_Text textMesh;
    public float floatDistance = 1.5f; // Khoảng cách bay lên
    
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
            popup.transform.SetParent(parent);
            popup.transform.position = position;
            popup.transform.localScale = Vector3.one; // Quan trọng: Reset scale khi lấy ra khỏi pool
            popup.transform.localRotation = Quaternion.identity;
            popup.gameObject.SetActive(true);
        }
        else
        {
            popup = Instantiate(prefab, position, Quaternion.identity, parent);
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

    public void Setup(float damageAmount, float scaleFactor = 1f)
    {
        // Dừng các hiệu ứng cũ nếu được lấy ra từ Pool
        transform.DOKill();
        if (textMesh != null) textMesh.DOKill();

        // Hiển thị số sát thương (ví dụ: -10)
        if (textMesh != null)
        {
            textMesh.text = "-" + damageAmount.ToString("F0");

            // Đặt mặc định text hiển thị rõ
            Color textColor = textMesh.color;
            textColor.a = 1f;
            textMesh.color = textColor;

            // Hiệu ứng mờ dần (Fade out)
            textMesh.DOFade(0f, 1f).SetEase(Ease.InExpo);
        }

        // Tỷ lệ khoảng cách để hầm không bị bay quá xa (scaleFactor)
        float scatterX = Random.Range(-0.3f, 0.3f) * scaleFactor;
        float scatterY = Random.Range(-0.2f, 0.2f) * scaleFactor;
        transform.position += new Vector3(scatterX, scatterY, 0);

        float distanceToFloat = floatDistance * scaleFactor;

        // Bay lên trên (từ tâm hiện tại) một khoảng distanceToFloat, sau đó đưa trở lại Pool thay vì Xóa
        transform.DOMoveY(transform.position.y + distanceToFloat, 1f).SetEase(Ease.OutCirc)
            .OnComplete(() => ReturnToPool());
    }
}
