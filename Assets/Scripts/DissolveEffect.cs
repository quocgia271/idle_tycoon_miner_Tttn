using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class DissolveEffect : MonoBehaviour
{
    public enum DissolveMode
    {
        Hide,   // Tan biến (từ 3 về 0)
        Appear  // Hiện ra (từ 0 lên 3)
    }

    [Header("Target Components (Để trống tự động tìm)")]
    [Tooltip("Nếu bật, script sẽ lôi toàn bộ các bộ phận (tay, chân, đầu) bên trong object này ra để làm tan biến.")]
    public bool includeChildren = true;
    public SpriteRenderer[] targetSpriteRenderers;
    public Image[] targetUIImages;

    [Header("Dissolve Settings")]
    public Material[] dissolveMaterials;
    public float dissolveDuration = 1.5f;
    public string dissolvePropertyName = "Vector1_E974001A";
    
    [Tooltip("Mức để hiện rõ (Thường là 3 đối với pack này)")]
    public float fullyVisibleValue = 3f;
    [Tooltip("Mức để tàng hình (Thường là 0)")]
    public float fullyHiddenValue = 0f;

    [Header("Additional UI Handling")]
    [Tooltip("Những Object này (như Text, Thanh máu) sẽ tắt NGAY LẬP TỨC khi tan biến, và bật lên khi hiện ra.")]
    public GameObject[] objectsToHideInstantly;
    [Tooltip("Những Canvas Group này sẽ mờ dần (Fade) đồng bộ với hiệu ứng tan biến.")]
    public CanvasGroup[] canvasGroupsToFade;

    [Header("Action")]
    [Tooltip("Chọn hiệu ứng Tan biến (Hide) hay Hiện ra (Appear)")]
    public DissolveMode dissolveMode = DissolveMode.Hide;
    
    [Tooltip("Tự động chạy hiệu ứng khi object được bật (hoặc game bắt đầu)")]
    public bool playOnStart = false;

    // Lưu trữ lại Material gốc (mặc định) của các bộ phận để tái sử dụng khi reset
    private Dictionary<SpriteRenderer, Material> originalSrMats = new Dictionary<SpriteRenderer, Material>();
    private Dictionary<SpriteRenderer, Color> originalSrColors = new Dictionary<SpriteRenderer, Color>();
    private Dictionary<Image, Material> originalImgMats = new Dictionary<Image, Material>();
    private bool hasCachedOriginals = false;

    private void Start()
    {
        CacheTargetsAndMaterials();
        if (playOnStart)
        {
            PlayEffect(null);
        }
    }

    private void CacheTargetsAndMaterials()
    {
        if (hasCachedOriginals) return;

        // Tự động tìm tất cả component nếu chưa gán
        if ((targetSpriteRenderers == null || targetSpriteRenderers.Length == 0) && 
            (targetUIImages == null || targetUIImages.Length == 0))
        {
            if (includeChildren)
            {
                targetSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                targetUIImages = GetComponentsInChildren<Image>(true);
            }
            else
            {
                targetSpriteRenderers = GetComponents<SpriteRenderer>();
                targetUIImages = GetComponents<Image>();
            }
        }

        if (targetSpriteRenderers != null)
        {
            foreach (var sr in targetSpriteRenderers)
            {
                if (sr != null && !originalSrMats.ContainsKey(sr))
                {
                    originalSrMats[sr] = sr.material;
                    originalSrColors[sr] = sr.color;
                }
            }
        }

        if (targetUIImages != null)
        {
            foreach (var img in targetUIImages)
            {
                if (img != null && !originalImgMats.ContainsKey(img))
                {
                    originalImgMats[img] = img.material;
                }
            }
        }

        hasCachedOriginals = true;
    }

    public void PlayEffect(System.Action onComplete = null)
    {
        CacheTargetsAndMaterials();

        if (dissolveMaterials == null || dissolveMaterials.Length == 0)
        {
            Debug.LogWarning("Chưa gán Dissolve Materials cho " + gameObject.name);
            onComplete?.Invoke();
            return;
        }

        Material randomMat = dissolveMaterials[Random.Range(0, dissolveMaterials.Length)];
        
        // Xác định giá trị bắt đầu và kết thúc dựa trên chế độ (Hide hay Appear)
        float startValue = dissolveMode == DissolveMode.Hide ? fullyVisibleValue : fullyHiddenValue;
        float endValue = dissolveMode == DissolveMode.Hide ? fullyHiddenValue : fullyVisibleValue;

        // Xử lý ẩn/hiện ngay lập tức các UI không hỗ trợ tan biến (Text, v.v...)
        if (objectsToHideInstantly != null)
        {
            foreach (var obj in objectsToHideInstantly)
            {
                if (obj != null) obj.SetActive(dissolveMode == DissolveMode.Appear);
            }
        }

        // Xử lý làm mờ dần các CanvasGroup
        if (canvasGroupsToFade != null)
        {
            foreach (var cg in canvasGroupsToFade)
            {
                if (cg != null)
                {
                    float startAlpha = dissolveMode == DissolveMode.Hide ? 1f : 0f;
                    float endAlpha = dissolveMode == DissolveMode.Hide ? 0f : 1f;
                    cg.alpha = startAlpha;
                    cg.DOFade(endAlpha, dissolveDuration).SetUpdate(true);
                }
            }
        }

        bool hasAnyTarget = false;
        bool isCompleted = false;

        System.Action finishAction = () => 
        {
            if (isCompleted) return;
            isCompleted = true;

            // Nếu là chế độ "Hiện ra" (Appear), trả lại toàn bộ Material gốc (Default)
            if (dissolveMode == DissolveMode.Appear)
            {
                RestoreOriginalMaterials();
            }
            else
            {
                // Nếu là Hide, sau khi ẩn xong thì tắt gameobject để tối ưu
                HideInstantly();
            }

            onComplete?.Invoke();
        };

        // Áp dụng hiệu ứng cho TẤT CẢ SpriteRenderer
        if (targetSpriteRenderers != null)
        {
            foreach (var sr in targetSpriteRenderers)
            {
                if (sr != null && sr.sprite != null && sr.gameObject.activeInHierarchy)
                {
                    Material clonedMat = new Material(randomMat);
                    clonedMat.SetFloat(dissolvePropertyName, startValue);
                    
                    clonedMat.SetTexture("_MainTex", sr.sprite.texture);
                    if (clonedMat.HasProperty("_BaseMap")) clonedMat.SetTexture("_BaseMap", sr.sprite.texture);
                    if (clonedMat.HasProperty("_Color")) clonedMat.SetColor("_Color", sr.color);
                    if (clonedMat.HasProperty("_BaseColor")) clonedMat.SetColor("_BaseColor", sr.color);

                    sr.material = clonedMat;
                    sr.material.DOFloat(endValue, dissolvePropertyName, dissolveDuration)
                               .SetEase(Ease.Linear)
                               .SetUpdate(true)
                               .OnComplete(() => finishAction());
                    hasAnyTarget = true;
                }
            }
        }

        // Áp dụng hiệu ứng cho TẤT CẢ UI Image
        if (targetUIImages != null)
        {
            foreach (var img in targetUIImages)
            {
                if (img != null && img.gameObject.activeInHierarchy)
                {
                    Material clonedMat = new Material(randomMat);
                    clonedMat.SetFloat(dissolvePropertyName, startValue);
                    img.material = clonedMat;
                    
                    img.material.DOFloat(endValue, dissolvePropertyName, dissolveDuration)
                                .SetEase(Ease.Linear)
                                .SetUpdate(true)
                                .OnComplete(() => finishAction());
                    hasAnyTarget = true;
                }
            }
        }

        if (!hasAnyTarget)
        {
            onComplete?.Invoke();
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var kvp in originalSrMats)
        {
            if (kvp.Key != null) kvp.Key.material = kvp.Value;
        }
        foreach (var kvp in originalImgMats)
        {
            if (kvp.Key != null) kvp.Key.material = kvp.Value;
        }
    }

    [ContextMenu("Test Effect")]
    public void TestEffect()
    {
        if (Application.isPlaying)
        {
            PlayEffect(() => Debug.Log("Test hiệu ứng " + dissolveMode.ToString() + " hoàn tất cho toàn bộ object!"));
        }
        else
        {
            Debug.LogWarning("Vui lòng bấm Play (▶) để test!");
        }
    }

    public void HideInstantly()
    {
        CacheTargetsAndMaterials();

        if (targetSpriteRenderers != null)
        {
            foreach (var sr in targetSpriteRenderers)
            {
                if (sr != null) sr.gameObject.SetActive(false);
            }
        }
        if (targetUIImages != null)
        {
            foreach (var img in targetUIImages)
            {
                if (img != null) img.gameObject.SetActive(false);
            }
        }
    }

    public void ResetToVisible(bool isRepairMode = false)
    {
        CacheTargetsAndMaterials();

        // Bật lại các object và khôi phục material
        if (targetSpriteRenderers != null)
        {
            foreach (var sr in targetSpriteRenderers)
            {
                if (sr != null) 
                {
                    sr.gameObject.SetActive(true);
                    if (originalSrMats.TryGetValue(sr, out Material mat))
                    {
                        sr.material = mat;
                    }
                    
                    if (isRepairMode)
                    {
                        sr.color = new Color(0.2f, 0.2f, 0.2f, 0.7f); // Đen mờ khi hỏng hầm
                    }
                    else
                    {
                        if (originalSrColors.TryGetValue(sr, out Color originalColor))
                        {
                            sr.color = originalColor; // Khôi phục màu gốc khi mở khóa bình thường
                        }
                    }
                }
            }
        }
        if (targetUIImages != null)
        {
            foreach (var img in targetUIImages)
            {
                if (img != null) 
                {
                    img.gameObject.SetActive(true);
                    if (originalImgMats.TryGetValue(img, out Material mat))
                    {
                        img.material = mat;
                    }
                }
            }
        }
        
        // Bật lại các object ẩn tức thì
        if (objectsToHideInstantly != null)
        {
            foreach (var obj in objectsToHideInstantly)
            {
                if (obj != null) obj.SetActive(true); 
            }
        }
        
        // Reset Alpha cho các CanvasGroup
        if (canvasGroupsToFade != null)
        {
            foreach (var cg in canvasGroupsToFade)
            {
                if (cg != null) cg.alpha = 1f;
            }
        }
    }
}
