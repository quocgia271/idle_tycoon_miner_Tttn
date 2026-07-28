using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManagerItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image AvatarImage;
    public Image BuffIcon;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI RarityText;
    public TextMeshProUGUI BuffDescriptionText;
    public TextMeshProUGUI DurationText;
    public TextMeshProUGUI OriginalPriceText;
    
    [Header("Buttons")]
    public Button AssignButton;
    public Button SellButton;

    public SimpleSpriteAnimator AvatarAnimator;

    private ManagerData _managerData;
    private ManagerModalUI _parentModal;

    public void Setup(ManagerData data, ManagerModalUI modal)
    {
        _managerData = data;
        _parentModal = modal;

        if (_parentModal.Config != null)
        {
            var charVis = _parentModal.Config.GetCharacterVisual(data.CharacterID);
            if (charVis != null)
            {
                if (AvatarAnimator != null) AvatarAnimator.frames = charVis.AnimationFrames;
                if (AvatarImage != null && charVis.AvatarStatic != null) AvatarImage.sprite = charVis.AvatarStatic;
            }

            if (BuffIcon != null)
            {
                BuffIcon.sprite = _parentModal.Config.GetSkillIcon(data.BuffType);
            }
        }

        NameText.text = data.Name;
        
        switch (data.Rarity)
        {
            case ManagerRarity.Junior: RarityText.text = "Trẻ tuổi"; break;
            case ManagerRarity.Director: RarityText.text = "Giám đốc"; break;
            case ManagerRarity.Senior: 
                RarityText.text = $"Cấp cao ({data.AssignedFacilityType}) - {data.SpecialFeature}"; 
                break;
        }

        string buffDesc = "";
        switch (data.BuffType)
        {
            case ManagerBuffType.MiningSpeed: 
                buffDesc = data.AssignedFacilityType == FacilityType.MineShaft ? $"Tăng tốc đào: +{data.BuffValue:F1}%" : $"Tốc độ chất hàng: +{data.BuffValue:F1}%";
                break;
            case ManagerBuffType.MoveSpeed: buffDesc = $"Tốc độ di chuyển: +{data.BuffValue:F1}%"; break;
            case ManagerBuffType.ReduceCost: buffDesc = $"Giảm chi phí: {data.BuffValue:F1}%"; break;
        }
        BuffDescriptionText.text = buffDesc;

        string durationStr = data.BuffDuration >= 60 ? (data.BuffDuration / 60f).ToString("0.##") + " phút" : data.BuffDuration + "s";
        DurationText.text = durationStr;
        OriginalPriceText.text = "Mua: " + CurrencyFormatter.FormatMoney(data.OriginalHirePrice);

        AssignButton.onClick.RemoveAllListeners();
        AssignButton.onClick.AddListener(() => _parentModal.OnAssignManager(_managerData));

        SellButton.onClick.RemoveAllListeners();
        SellButton.onClick.AddListener(() => _parentModal.OnSellManager(_managerData, this.gameObject));

        // Disable assign button if already assigned
        AssignButton.interactable = !data.IsAssigned;
    }

    private void Update()
    {
        if (_managerData == null || DurationText == null) return;

        if (_managerData.IsSkillActive())
        {
            DurationText.text = $"Skill: {Mathf.CeilToInt(_managerData.GetRemainingSkillTime())}s";
            DurationText.color = Color.green;
        }
        else if (_managerData.IsOnCooldown())
        {
            DurationText.text = $"Hồi: {Mathf.CeilToInt(_managerData.GetRemainingCooldownTime())}s";
            DurationText.color = Color.red;
        }
        else
        {
            string durationStr = _managerData.BuffDuration >= 60 ? (_managerData.BuffDuration / 60f).ToString("0.##") + " phút" : _managerData.BuffDuration + "s";
            DurationText.text = durationStr;
            DurationText.color = Color.white;
        }
    }
}
