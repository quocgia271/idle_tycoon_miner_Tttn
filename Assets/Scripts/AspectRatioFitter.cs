using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioFitter : MonoBehaviour
{
    [Header("Target Aspect Ratio")]
    public float targetAspectWidth = 9.0f;
    public float targetAspectHeight = 16.0f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        AdjustCameraViewport();
    }

#if UNITY_EDITOR
    void Update()
    {
        // Giúp preview realtime tỉ lệ khi kéo thả màn hình trong Editor
        AdjustCameraViewport();
    }
#endif

    void AdjustCameraViewport()
    {
        if (cam == null) return;

        float targetAspect = targetAspectWidth / targetAspectHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        // Nếu màn hình hiện tại dài hơn target aspect (ví dụ điện thoại 19.5:9 dài hơn 16:9)
        // => Cần thêm viền đen ở trên và dưới (Letterbox)
        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            cam.rect = rect;
        }
        else // Nếu màn hình hiện tại rộng hơn target aspect (ví dụ màn iPad 3:4 hoặc landscape)
        // => Cần thêm viền đen ở 2 bên trái phải (Pillarbox)
        {
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }

        // Truyền Rect này vào Shader toàn cầu để các Shader đọc màn hình (như Ripple) có thể tính toán lại UV chính xác
        Shader.SetGlobalVector("_GlobalCameraRect", new Vector4(cam.rect.x, cam.rect.y, cam.rect.width, cam.rect.height));
    }
}
