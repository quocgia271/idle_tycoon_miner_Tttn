using UnityEngine;

public class RippleEffectController : MonoBehaviour
{
    [Header("Ripple Settings")]
    [Tooltip("The material using the URP_ScreenRipple shader")]
    public Material rippleMaterial;
    public float animationDuration = 1.0f;
    public float maxRadius = 1.5f;
    public float maxStrength = 0.1f;
    
    private float currentTimer = 0f;
    private bool isPlaying = false;
    private Material instancedMaterial;

    private void Awake()
    {
        // Tạo một instance của material để không làm thay đổi material gốc
        if (rippleMaterial != null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                instancedMaterial = new Material(rippleMaterial);
                renderer.material = instancedMaterial;
            }
        }
    }

    private void OnEnable()
    {
        PlayRipple();
    }

    public void PlayRipple()
    {
        if (instancedMaterial == null) return;
        
        currentTimer = 0f;
        isPlaying = true;
        
        // Reset properties
        instancedMaterial.SetFloat("_RippleRadius", 0f);
        instancedMaterial.SetFloat("_RippleStrength", maxStrength);
    }

    private void Update()
    {
        if (!isPlaying || instancedMaterial == null) return;

        currentTimer += Time.deltaTime;
        float progress = currentTimer / animationDuration;

        if (progress >= 1f)
        {
            isPlaying = false;
            instancedMaterial.SetFloat("_RippleStrength", 0f); // Tắt distortion
            // Tùy chọn: gameObject.SetActive(false); // Ẩn luôn object sau khi xong
        }
        else
        {
            // Tăng dần radius
            float currentRadius = Mathf.Lerp(0f, maxRadius, progress);
            instancedMaterial.SetFloat("_RippleRadius", currentRadius);
            
            // Giảm dần strength để tạo cảm giác lan tỏa tan biến
            float currentStrength = Mathf.Lerp(maxStrength, 0f, progress);
            instancedMaterial.SetFloat("_RippleStrength", currentStrength);
        }
    }
}
