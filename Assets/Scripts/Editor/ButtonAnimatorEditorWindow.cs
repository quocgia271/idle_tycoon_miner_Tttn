#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class ButtonAnimatorEditorWindow : EditorWindow
{
    [MenuItem("Tools/Add Animator To All Buttons")]
    public static void AddAnimatorToAllButtons()
    {
        // Find all buttons in the scene
        Button[] allButtonsInScene = Resources.FindObjectsOfTypeAll<Button>();
        int count = 0;

        foreach (Button btn in allButtonsInScene)
        {
            // Make sure it's part of the scene (not a prefab in the project window)
            if (btn.gameObject.scene.isLoaded)
            {
                if (btn.GetComponent<ButtonAnimator>() == null)
                {
                    Undo.AddComponent<ButtonAnimator>(btn.gameObject);
                    count++;
                }
            }
        }
        
        Debug.Log($"Added ButtonAnimator to {count} buttons in the scene!");
    }
}
#endif
