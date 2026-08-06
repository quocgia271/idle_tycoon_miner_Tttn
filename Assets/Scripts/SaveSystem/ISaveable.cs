using UnityEngine;

public interface ISaveable
{
    void PopulateSaveData(SaveData data);
    void LoadFromSaveData(SaveData data);
}
