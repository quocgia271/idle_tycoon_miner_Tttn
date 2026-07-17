#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class FacilitySOCreator
{
    // Tạo 1 menu đặc biệt trên cùng của Unity Editor để bạn bấm click 1 cái là tự sinh ra
    [MenuItem("Tycoon/Generate Default SOs")]
    public static void CreateSOs()
    {
        CreateSO("MineShaftConfig", "Hầm Mỏ", new List<string> { "Tổng khai thác", "Sức chứa công nhân", "Tốc độ khai thác" });
        CreateSO("ElevatorConfig", "Thang Máy", new List<string> { "Tổng vận chuyển", "Sức chứa thang máy", "Tốc độ di chuyển" });
        CreateSO("WarehouseConfig", "Nhà Kho", new List<string> { "Tổng vận chuyển", "Sức chứa xe đẩy", "Tốc độ bước đi" });
        
        AssetDatabase.SaveAssets();
        Debug.Log("<color=green>Đã tự động tạo xong 3 file SO trong thư mục Assets/Resources!</color>");
    }
    
    private static void CreateSO(string fileName, string facName, List<string> stats)
    {
        if(!AssetDatabase.IsValidFolder("Assets/Resources")) 
            AssetDatabase.CreateFolder("Assets", "Resources");
            
        string path = $"Assets/Resources/{fileName}.asset";
        
        // Bỏ qua nếu đã có sẵn để không ghi đè
        if(AssetDatabase.LoadAssetAtPath<FacilityConfigSO>(path) != null) return; 
        
        FacilityConfigSO so = ScriptableObject.CreateInstance<FacilityConfigSO>();
        so.FacilityName = facName;
        so.StatNames = stats;
        
        AssetDatabase.CreateAsset(so, path);
    }
}
#endif
