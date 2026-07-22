using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [Header("Camera Constraints")]
    [Tooltip("Vị trí thấp nhất camera có thể kéo tới (Ví dụ: Đáy mỏ)")]
    public float minY = -20f; 
    [Tooltip("Vị trí cao nhất camera có thể kéo tới (Ví dụ: Khu vực nhà kho)")]
    public float maxY = 5.3f;  

    [Header("Settings")]
    [Tooltip("Tốc độ kéo màn hình")]
    public float panSpeed = 1f; 
    [Tooltip("Khoảng cách TỐI ĐA camera có thể văng ra ngoài giới hạn (Chỉ được phép kéo lố 1 tí)")]
    public float maxOverscrollDistance = 1.0f;
    [Tooltip("Thời gian nảy về vị trí an toàn (giây)")]
    public float bounceDuration = 0.3f;

    private Vector3 _startMousePos;
    private float _startCameraY;
    private Camera _mainCamera;
    private bool _isDragging = false;

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = GetComponent<Camera>();
        }
    }

    private void LateUpdate()
    {
        PanCamera();
    }

    private void PanCamera()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Hủy hiệu ứng nảy nếu người chơi chạm vào lại
            transform.DOKill();
            
            _startMousePos = Input.mousePosition;
            _startCameraY = transform.position.y;
            _isDragging = true;
        }

        if (Input.GetMouseButton(0) && _isDragging)
        {
            // Tính toán khoảng cách chuột di chuyển trên màn hình (theo trục Y)
            float mouseDeltaY = Input.mousePosition.y - _startMousePos.y;
            
            // Chuyển đổi sang khoảng cách thế giới (World Space)
            float worldHeight = _mainCamera.orthographicSize * 2f;
            float worldDeltaY = (mouseDeltaY / Screen.height) * worldHeight;

            // Camera di chuyển ngược chiều tay kéo
            float targetY = _startCameraY - worldDeltaY * panSpeed;

            // Áp dụng công thức giới hạn (Hard Limit Overscroll)
            if (targetY < minY)
            {
                // Chỉ cho phép kéo lố khi chạm đáy (Min Y)
                float excess = minY - targetY;
                float actualExcess = maxOverscrollDistance * (1f - 1f / ((excess / maxOverscrollDistance) + 1f));
                targetY = minY - actualExcess;
            }
            else if (targetY > maxY)
            {
                // KHÔNG cho phép kéo lố ở mặt đất (Max Y) - Chặn cứng lại luôn
                targetY = maxY;
                
                // Đồng thời update lại _startCameraY và _startMousePos để khi người chơi 
                // đổi hướng vuốt xuống lại thì camera nhận liền mà không bị khựng
                _startCameraY = targetY;
                _startMousePos = Input.mousePosition;
            }

            Vector3 newPos = transform.position;
            newPos.y = targetY;
            transform.position = newPos;
        }

        // Khi thả tay ra, dùng DOTween nảy về vị trí cực trị
        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            // Vì maxY đã bị chặn cứng, ta chỉ cần lo việc nảy về cho minY
            if (transform.position.y < minY)
            {
                transform.DOMoveY(minY, bounceDuration).SetEase(Ease.OutQuad);
            }
            else if (transform.position.y > maxY)
            {
                // Đề phòng trường hợp camera vô tình ở ngoài, snap về lập tức
                Vector3 fixPos = transform.position;
                fixPos.y = maxY;
                transform.position = fixPos;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Chỉnh màu xanh lá cho 2 đường giới hạn ngang
        Gizmos.color = Color.green;

        float camX = transform.position.x;
        float camZ = transform.position.z;

        // Chiều dài của đường kẻ ngang để dễ nhìn trong Scene (có thể chỉnh số 5f to lên nếu muốn dài hơn)
        float lineLength = 5f; 

        // Các điểm cho đường giới hạn dưới (Min Y)
        Vector3 minPosLeft = new Vector3(camX - lineLength, minY, camZ);
        Vector3 minPosRight = new Vector3(camX + lineLength, minY, camZ);
        
        // Các điểm cho đường giới hạn trên (Max Y)
        Vector3 maxPosLeft = new Vector3(camX - lineLength, maxY, camZ);
        Vector3 maxPosRight = new Vector3(camX + lineLength, maxY, camZ);

        // Vẽ đường ngang dưới cùng và trên cùng
        Gizmos.DrawLine(minPosLeft, minPosRight);
        Gizmos.DrawLine(maxPosLeft, maxPosRight);

        // Chỉnh màu vàng cho đường nối dọc
        Gizmos.color = Color.yellow;

        // Vẽ một đường thẳng đứng nối giữa Min Y và Max Y ngay tâm trục X của camera
        Vector3 minCenter = new Vector3(camX, minY, camZ);
        Vector3 maxCenter = new Vector3(camX, maxY, camZ);
        Gizmos.DrawLine(minCenter, maxCenter);
    }
}
