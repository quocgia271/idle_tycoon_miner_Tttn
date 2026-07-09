using UnityEngine;
using UnityEngine.UI;

public class SimpleSpriteAnimator : MonoBehaviour
{
    public enum LoopMode { Normal, PingPong }

    [Header("Animation Settings")]
    [Tooltip("Kéo thả lần lượt các ảnh (frame) của nhân vật vào đây")]
    public Sprite[] frames; 
    
    [Tooltip("Thời gian chuyển đổi giữa mỗi ảnh (Tính bằng giây, vd: 0.1)")]
    public float frameRate = 0.1f; 

    [Tooltip("Normal: Chạy 1-2-3-1-2-3 | PingPong: Chạy 1-2-3-2-1 (Rất hợp cho Idle thở tự nhiên)")]
    public LoopMode loopMode = LoopMode.PingPong;

    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    
    private int currentFrame = 0;
    private float timer = 0f;
    private int direction = 1;

    private void Start()
    {
        // Tự động tìm component hiển thị ảnh (hỗ trợ cả nhân vật ở ngoài World và nhân vật trên Canvas UI)
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();

        if (spriteRenderer == null && uiImage == null)
        {
            Debug.LogWarning("Không tìm thấy SpriteRenderer hay Image nào trên nhân vật này!");
        }
        
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Chưa có ảnh (frames) nào được gắn vào SimpleSpriteAnimator!");
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;

        // Nếu đã đến lúc chuyển frame
        if (timer >= frameRate)
        {
            timer -= frameRate; 
            
            if (loopMode == LoopMode.Normal)
            {
                currentFrame = (currentFrame + 1) % frames.Length;
            }
            else if (loopMode == LoopMode.PingPong)
            {
                currentFrame += direction;
                if (currentFrame >= frames.Length - 1)
                {
                    currentFrame = frames.Length - 1;
                    direction = -1; // Quay ngược lại
                }
                else if (currentFrame <= 0)
                {
                    currentFrame = 0;
                    direction = 1; // Đi xuôi lại
                }
            }

            // Cập nhật ảnh ra màn hình
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[currentFrame];
            }
            else if (uiImage != null)
            {
                uiImage.sprite = frames[currentFrame];
            }
        }
    }
}
