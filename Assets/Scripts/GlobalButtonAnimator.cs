using UnityEngine;
using UnityEngine.UI;

public class GlobalButtonAnimator : MonoBehaviour
{
    void Awake()
    {
        // Find all buttons in the scene
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

        foreach (Button btn in allButtons)
        {
            // Make sure it's part of the scene and not a prefab in project view
            if (btn.gameObject.scene.isLoaded)
            {
                // Check if it already has the animator
                if (btn.GetComponent<ButtonAnimator>() == null)
                {
                    btn.gameObject.AddComponent<ButtonAnimator>();
                }
            }
        }
    }
}
