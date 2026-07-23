using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // Thêm DOTween để làm animation

public class SellManagerModalUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform ModalPanel; // Thêm Panel để scale
    public Image AvatarImage;
    public SimpleSpriteAnimator AvatarAnimator;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI RarityText;
    public Image BuffIcon;
    public TextMeshProUGUI BuffDescriptionText;
    public TextMeshProUGUI DurationText;
    public TextMeshProUGUI OriginalPriceText;
    public TextMeshProUGUI SellPriceText; // Text hiển thị giá bán 50%
    
    [Header("Buttons")]
    public Button ConfirmSellButton;
    public Button CloseButton;

    private ManagerData _managerData;
    private ManagerModalUI _parentModal;

    private void Awake()
    {
        if (CloseButton != null)
        {
            CloseButton.onClick.AddListener(CloseModal);
        }
        
        if (ConfirmSellButton != null)
        {
            ConfirmSellButton.onClick.AddListener(OnConfirmSell);
        }
    }

    public void Setup(ManagerData data, ManagerModalUI parentModal)
    {
        _managerData = data;
        _parentModal = parentModal;

        if (parentModal != null && parentModal.Config != null)
        {
            var charVis = parentModal.Config.GetCharacterVisual(data.CharacterID);
            if (charVis != null)
            {
                if (AvatarAnimator != null) AvatarAnimator.frames = charVis.AnimationFrames;
                if (AvatarImage != null && charVis.AvatarStatic != null) AvatarImage.sprite = charVis.AvatarStatic;
            }

            if (BuffIcon != null)
            {
                BuffIcon.sprite = parentModal.Config.GetSkillIcon(data.BuffType);
            }
        }

        if (NameText != null) NameText.text = data.Name;
        
        if (RarityText != null)
        {
            switch (data.Rarity)
            {
                case ManagerRarity.Junior: RarityText.text = "Trẻ tuổi"; break;
                case ManagerRarity.Director: RarityText.text = "Giám đốc"; break;
                case ManagerRarity.Senior: RarityText.text = $"Cấp cao_{data.SupportType}"; break;
            }
        }

        if (BuffDescriptionText != null)
        {
            string buffDesc = "";
            switch (data.BuffType)
            {
                case ManagerBuffType.MiningSpeed: buffDesc = $"Tăng tốc đào: +{data.BuffValue:F1}%"; break;
                case ManagerBuffType.MoveSpeed: buffDesc = $"Tốc độ di chuyển: +{data.BuffValue:F1}%"; break;
                case ManagerBuffType.ReduceCost: buffDesc = $"Giảm chi phí: {data.BuffValue:F1}%"; break;
            }
            BuffDescriptionText.text = buffDesc;
        }

        if (DurationText != null)
        {
            string durationStr = data.BuffDuration >= 60 ? (data.BuffDuration / 60f).ToString("0.##") + " phút" : data.BuffDuration + "s";
            DurationText.text = durationStr;
        }

        if (OriginalPriceText != null)
        {
            OriginalPriceText.text = "Giá mua: " + CurrencyFormatter.FormatMoney(data.OriginalHirePrice);
        }

        if (SellPriceText != null)
        {
            double sellPrice = data.OriginalHirePrice * 0.5f;
            SellPriceText.text = "Bán: " + CurrencyFormatter.FormatMoney(sellPrice);
        }

        gameObject.SetActive(true);
        
        // Animation bật Modal
        if (ModalPanel != null)
        {
            ModalPanel.localScale = Vector3.zero;
            ModalPanel.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    private void OnConfirmSell()
    {
        if (_managerData != null && _parentModal != null)
        {
            _parentModal.ExecuteSellManager(_managerData);
        }
        CloseModal();
    }

    public void CloseModal()
    {
        if (ModalPanel != null)
        {
            // Animation tắt Modal
            ModalPanel.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => 
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
