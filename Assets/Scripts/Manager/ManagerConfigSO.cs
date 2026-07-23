using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ManagerConfig", menuName = "ScriptableObjects/ManagerConfig")]
public class ManagerConfigSO : ScriptableObject
{
    [System.Serializable]
    public class BuffVisual
    {
        public ManagerBuffType BuffType;
        public Sprite SkillIcon;
    }
    
    [Header("--- VISUAL SETTINGS ---")]
    public List<BuffVisual> BuffVisuals;

    [System.Serializable]
    public class CharacterVisual
    {
        [Tooltip("Tên ID của nhân vật (VD: CharA, CharB)")]
        public string CharacterID; 
        
        [Tooltip("Ảnh hiển thị tĩnh (Nếu không có animation)")]
        public Sprite AvatarStatic;
        
        [Tooltip("Các frame ảnh để chạy Animation")]
        public Sprite[] AnimationFrames; 
    }
    public List<CharacterVisual> CharacterVisuals;


    [Header("--- GACHA SETTINGS ---")]
    [Tooltip("Số lần thuê để kích hoạt bảo hiểm (Ra chắc chắn Senior)")]
    public int PityThreshold = 10;

    [System.Serializable]
    public class RaritySetting
    {
        public ManagerRarity Rarity;
        [Tooltip("Tỷ lệ xuất hiện (Tổng số Weight)")]
        public float Weight;
        
        [Header("Buff Power Ranges")]
        public float MinBuffValue;
        public float MaxBuffValue;
        
        [Header("Duration & Cooldown")]
        public float MinDuration;
        public float MaxDuration;
        
        public float MinCooldown;
        public float MaxCooldown;
    }

    public List<RaritySetting> RaritySettings;

    public Sprite GetSkillIcon(ManagerBuffType type)
    {
        var match = BuffVisuals.Find(x => x.BuffType == type);
        return match != null ? match.SkillIcon : null;
    }

    public CharacterVisual GetCharacterVisual(string id)
    {
        return CharacterVisuals.Find(x => x.CharacterID == id);
    }
    
    public RaritySetting GetRaritySetting(ManagerRarity rarity)
    {
        return RaritySettings.Find(x => x.Rarity == rarity);
    }
}
