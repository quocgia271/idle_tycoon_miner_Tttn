using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class DamagePopup : MonoBehaviour
{
    public TMP_Text textMesh;
    public float floatDistance = 1.5f; // Khoảng cách bay lên
    public float sizeMultiplier = 1.2f; // Hệ số phóng to/thu nhỏ chữ
    
    public enum PopupSourceType
    {
        Mineshaft,
        Boss,
        Dragon,
        Minion
    }

    // Object Pool tĩnh với Dictionary để quản lý riêng biệt từng loại
    private static Dictionary<PopupSourceType, Queue<DamagePopup>> pools = new Dictionary<PopupSourceType, Queue<DamagePopup>>()
    {
        { PopupSourceType.Mineshaft, new Queue<DamagePopup>() },
        { PopupSourceType.Boss, new Queue<DamagePopup>() },
        { PopupSourceType.Dragon, new Queue<DamagePopup>() },
        { PopupSourceType.Minion, new Queue<DamagePopup>() }
    };
    
    [HideInInspector] public PopupSourceType popupSourceType = PopupSourceType.Mineshaft;

    private static Transform popupContainer;

    public static DamagePopup Create(DamagePopup prefab, Vector3 position, Transform parent, PopupSourceType sourceType, float customScale = -1f)
    {
        DamagePopup popup = null;
        Queue<DamagePopup> activePool = pools[sourceType];

        while (activePool.Count > 0 && popup == null)
        {
            popup = activePool.Dequeue();
        }

        // CHỮ UI BẮT BUỘC PHẢI NẰM TRONG CANVAS MỚI HIỂN THỊ ĐƯỢC!
        // Thay vì gắn vào Minion (bị méo chữ), ta tìm Canvas chứa Minion đó và gắn thẳng chữ vào Canvas gốc.
        Transform targetParent = null;
        if (parent != null)
        {
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null) targetParent = canvas.transform;
        }

        // Nếu không tìm thấy Canvas (trường hợp game dùng 3D Text), dùng Container tĩnh
        if (targetParent == null)
        {
            if (popupContainer == null) popupContainer = new GameObject("DamagePopupContainer").transform;
            targetParent = popupContainer;
        }

        if (popup != null)
        {
            // Gắn vào Canvas gốc. Dùng 'false' để Unity KHÔNG tự động thay đổi localScale của chữ
            popup.transform.SetParent(targetParent, false);
            
            // Theo yêu cầu: khi tái sử dụng từ Pool (click liên tục), KHÔNG được đổi scale
            popup.transform.localRotation = prefab.transform.localRotation;
            
            // Đặt lại vị trí xuất hiện (phải đặt sau khi SetParent)
            popup.transform.position = position;
            popup.gameObject.SetActive(true);
        }
        else
        {
            // Nếu Pool cạn, tạo mới và gắn thẳng vào targetParent
            popup = Instantiate(prefab, position, Quaternion.identity, targetParent);
            float appliedScale = (customScale > 0) ? customScale : prefab.sizeMultiplier;
            popup.transform.localScale = prefab.transform.localScale * appliedScale;
            popup.popupSourceType = sourceType;
        }
        return popup;
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false);
        pools[popupSourceType].Enqueue(this);
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

    public void Setup(float amount, float scaleFactor, Color textColor, bool isHeal = false)
    {
        // Dừng các hiệu ứng cũ nếu được lấy ra từ Pool
        transform.DOKill();
        if (textMesh != null) textMesh.DOKill();

        // Hiển thị số (ví dụ: +25 hoặc -10)
        if (textMesh != null)
        {
            if (isHeal)
            {
                textMesh.text = "+" + amount.ToString("F0");
            }
            else
            {
                textMesh.text = "-" + amount.ToString("F0");
            }

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
