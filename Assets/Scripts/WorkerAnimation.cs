using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

public class WorkerAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator anim;
    public SpriteRenderer spriteRenderer;
    public List<GameObject> hurtVFXList;
    public GameObject deathVFX;

    private WorkerHealth workerHealth;
    private Miner miner;
    private Vector3 initialScale;
    private Vector3 originalDeathVFXPos;
    private Vector3 originalDeathVFXScale;

    private void Awake()
    {
        workerHealth = GetComponent<WorkerHealth>();
        miner = GetComponent<Miner>();
        if (workerHealth != null)
        {
            workerHealth.OnInjured += HandleInjured;
            workerHealth.OnDied += HandleDied;
            workerHealth.OnRevived += HandleRevived;
            workerHealth.OnCleansed += HandleCleansed;
        }
    }

    private void Start()
    {
        initialScale = transform.localScale;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();

        if (deathVFX != null) 
        {
            originalDeathVFXPos = deathVFX.transform.localPosition;
            originalDeathVFXScale = deathVFX.transform.localScale;
            if (workerHealth == null || workerHealth.healthState != WorkerHealth.HealthState.Dead)
            {
                deathVFX.SetActive(false);
            }
        }
        
        if (anim != null) anim.SetTrigger("idle");
    }

    private void OnDestroy()
    {
        if (workerHealth != null)
        {
            workerHealth.OnInjured -= HandleInjured;
            workerHealth.OnDied -= HandleDied;
            workerHealth.OnRevived -= HandleRevived;
            workerHealth.OnCleansed -= HandleCleansed;
        }
    }
    
    public void PlayAnimTrigger(string triggerName)
    {
        if (anim != null && anim.gameObject.activeInHierarchy && anim.enabled) 
            anim.SetTrigger(triggerName);
    }

    private void HandleInjured(BossPhase2Controller boss)
    {
        if (hurtVFXList != null && hurtVFXList.Count > 0)
        {
            foreach (var vfx in hurtVFXList) if (vfx != null) vfx.SetActive(false);
            int rndIndex = Random.Range(0, hurtVFXList.Count);
            if (hurtVFXList[rndIndex] != null) hurtVFXList[rndIndex].SetActive(true);
        }
        if (spriteRenderer != null)
            spriteRenderer.DOColor(Color.red, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }

    private void HandleDied(bool isOffline)
    {
        if (anim != null) anim.enabled = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.color = Color.white;
        }

        if (isOffline)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            
            transform.SetParent(null, true);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            
            if (deathVFX != null) deathVFX.SetActive(false);
            
            if (MoraleModalUI.Instance != null && MoraleModalUI.Instance.gameObject.activeInHierarchy)
            {
                MoraleModalUI.Instance.RefreshList();
            }
            return;
        }

        BossPhase2Controller currentBoss = FindObjectOfType<BossPhase2Controller>();

        if (currentBoss != null && currentBoss.suckTargetPos != null && currentBoss.gameObject.activeInHierarchy)
        {
            transform.SetParent(null, true);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Vector3 targetPos = new Vector3(currentBoss.suckTargetPos.position.x, currentBoss.suckTargetPos.position.y, transform.position.z);
            Sequence scaleSeq = DOTween.Sequence();
            scaleSeq.SetTarget(transform);
            scaleSeq.Append(transform.DOScale(initialScale * 1.3f, 0.9f).SetEase(Ease.InOutSine)); 
            scaleSeq.Append(transform.DOScale(Vector3.zero, 0.9f).SetEase(Ease.InOutSine)); 

            Vector3 startPos = transform.position;
            Vector3 midPoint = startPos + (targetPos - startPos) / 2f;
            midPoint.y += 1.5f;
            midPoint.x += Random.Range(-1f, 1f);

            Vector3[] path = new Vector3[] { midPoint, targetPos };

            transform.DOPath(path, 1.8f, PathType.CatmullRom).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                if (spriteRenderer != null) spriteRenderer.enabled = false;
                if (deathVFX != null)
                {
                    deathVFX.transform.SetParent(null, true);
                    deathVFX.transform.position = targetPos;
                    deathVFX.transform.localScale = originalDeathVFXScale;
                    deathVFX.SetActive(false);
                    deathVFX.SetActive(true);
                }
            });
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            
            transform.SetParent(null, true);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
        
        if (MoraleModalUI.Instance != null && MoraleModalUI.Instance.gameObject.activeInHierarchy)
        {
            MoraleModalUI.Instance.RefreshList();
        }
    }

    private void HandleRevived()
    {
        if (hurtVFXList != null)
        {
            foreach (var vfx in hurtVFXList) if (vfx != null) vfx.SetActive(false);
        }
        
        if (deathVFX != null) 
        {
            deathVFX.SetActive(false);
            deathVFX.transform.SetParent(this.transform, false);
            deathVFX.transform.localPosition = originalDeathVFXPos;
            deathVFX.transform.localScale = originalDeathVFXScale;
        }
        
        transform.DOKill();
        if (spriteRenderer != null) spriteRenderer.DOKill();
        
        StartCoroutine(ReviveAnimationRoutine());
    }

    private IEnumerator ReviveAnimationRoutine()
    {
        if (miner != null) miner.ResetStateToIdle();
        
        if (anim != null) anim.SetTrigger("idle");
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        if (anim != null) anim.enabled = true;
        
        if (miner != null && miner.currentShaft != null) transform.SetParent(miner.currentShaft.transform, true);
        transform.localScale = initialScale;
        
        if (miner != null && miner.startPos != null) transform.position = miner.startPos.position;
        transform.rotation = Quaternion.identity;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color c = Color.white;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        Sequence bounceSeq = DOTween.Sequence();
        bounceSeq.SetTarget(transform);
        float yPos = transform.position.y;
        bounceSeq.Append(transform.DOMoveY(yPos + 1f, 0.25f).SetEase(Ease.OutQuad));
        bounceSeq.Append(transform.DOMoveY(yPos, 0.25f).SetEase(Ease.InQuad));
        bounceSeq.Append(transform.DOMoveY(yPos + 0.5f, 0.2f).SetEase(Ease.OutQuad));
        bounceSeq.Append(transform.DOMoveY(yPos, 0.2f).SetEase(Ease.InQuad));
        
        transform.DORotate(new Vector3(0, 0, 360), 0.9f, RotateMode.FastBeyond360).SetEase(Ease.OutBack);

        yield return bounceSeq.WaitForCompletion();
        
        if (MoraleModalUI.Instance != null && MoraleModalUI.Instance.gameObject.activeInHierarchy)
        {
            MoraleModalUI.Instance.RefreshList();
        }
    }

    private void HandleCleansed()
    {
        if (hurtVFXList != null)
        {
            foreach (var vfx in hurtVFXList)
            {
                if (vfx != null && vfx.activeInHierarchy)
                {
                    StartCoroutine(FadeOutVFXSmoothly(vfx));
                }
            }
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.DOColor(Color.white, 0.5f);
        }
    }

    private IEnumerator FadeOutVFXSmoothly(GameObject vfxObject)
    {
        SpriteRenderer[] srs = vfxObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs) sr.DOFade(0f, 0.5f);

        ParticleSystem[] pSystems = vfxObject.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in pSystems) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        yield return new WaitForSeconds(1f);
        
        foreach (var sr in srs)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        vfxObject.SetActive(false);
    }
}
