using UnityEngine;
using TMPro;
using System.Collections;

public class WorkerUI : MonoBehaviour
{
    [Header("References")]
    public ProgressBar progressBar;
    public ProgressBar moraleBar;
    public TextMeshProUGUI moneyText;
    
    private WorkerHealth workerHealth;
    private Miner miner;
    private MineShaft currentShaft;

    private Vector3 initialTextLocalPos;
    private bool isFloatingText = false;

    private void Awake()
    {
        workerHealth = GetComponent<WorkerHealth>();
        miner = GetComponent<Miner>();
        
        if (workerHealth != null)
        {
            workerHealth.OnMoraleChanged += UpdateMoraleUI;
            workerHealth.OnCleansed += HandleCleansed;
        }
    }
    
    private void Start()
    {
        if (miner != null) currentShaft = miner.currentShaft;
        
        if (moneyText != null)
        {
            initialTextLocalPos = moneyText.transform.localPosition;
            moneyText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (workerHealth != null)
        {
            workerHealth.OnMoraleChanged -= UpdateMoraleUI;
            workerHealth.OnCleansed -= HandleCleansed;
        }
    }

    public void UpdateMoraleUI(float current, float max)
    {
        if (moraleBar != null)
            moraleBar.SetProgress(current / max);
    }
    
    private void HandleCleansed()
    {
        if (currentShaft == null && miner != null) currentShaft = miner.currentShaft;
        if (currentShaft != null && currentShaft.damagePopupPrefab != null)
        {
            DamagePopup popup = DamagePopup.Create(currentShaft.damagePopupPrefab, transform.position + Vector3.up * 0.8f, transform, DamagePopup.PopupSourceType.Mineshaft);
            popup.Setup(25f, 0.1f, Color.yellow, true);
        }
    }

    private void Update()
    {
        FlipUIIfNeeded();
    }

    private void FlipUIIfNeeded()
    {
        bool isFlipped = transform.rotation.eulerAngles.y > 90f;
        
        if (moraleBar != null)
            moraleBar.transform.localRotation = Quaternion.Euler(0, isFlipped ? 180f : 0f, 0);
            
        if (progressBar != null)
        {
            Vector3 pScale = progressBar.transform.localScale;
            pScale.x = Mathf.Abs(pScale.x);
            progressBar.transform.localScale = pScale;
            progressBar.transform.localRotation = Quaternion.Euler(0, isFlipped ? 180f : 0f, 0);
        }
            
        if (moneyText != null)
        {
            Vector3 tScale = moneyText.transform.localScale;
            tScale.x = Mathf.Abs(tScale.x);
            moneyText.transform.localScale = tScale;
            moneyText.transform.localRotation = Quaternion.Euler(0, isFlipped ? 180f : 0f, 0);
        }
    }
    
    public void StartLoadingProgress(float time)
    {
        if (progressBar != null) progressBar.StartLoading(time);
    }

    public void ShowMoneyText(double amount)
    {
        if (moneyText == null) return;
        moneyText.gameObject.SetActive(true);
        moneyText.text = CurrencyFormatter.FormatMoney(amount);
        
        if (isFloatingText)
        {
            StopCoroutine("ShowFloatingTextRoutine");
            ResetTextState();
        }
    }

    public void HideMoneyText()
    {
        if (moneyText != null && !isFloatingText) 
        {
            moneyText.gameObject.SetActive(false);
        }
    }

    public void TriggerFloatingText()
    {
        if (moneyText != null)
        {
            StartCoroutine("ShowFloatingTextRoutine");
        }
    }

    private void ResetTextState()
    {
        if (moneyText == null) return;
        isFloatingText = false;
        moneyText.transform.localPosition = initialTextLocalPos;
        Color c = moneyText.color;
        c.a = 1f;
        moneyText.color = c;
    }

    private IEnumerator ShowFloatingTextRoutine()
    {
        isFloatingText = true;
        Vector3 startWorldPos = moneyText.transform.position;
        Vector3 targetWorldPos = startWorldPos + new Vector3(0, 2f, 0);
        
        float duration = 1.0f;
        float time = 0;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            
            moneyText.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            
            Color c = moneyText.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            moneyText.color = c;
            
            yield return null;
        }
        
        moneyText.gameObject.SetActive(false);
        ResetTextState();
    }
}
