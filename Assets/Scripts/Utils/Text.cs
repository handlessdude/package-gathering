using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI Text

public class TextUtils : MonoBehaviour
{
    static string DEBUG_TEXT_TAG = "DebugText";
    /// <summary>
    /// Appends the given string as a new line to the UI Text object with the specified tag.
    /// </summary>
    /// <param name="tag">Tag of the UI Text object</param>
    /// <param name="newLineText">Text to append as a new line</param>
    public static void AppendTextToTaggedObject(string newLineText)
    {
        // Find the text object by tag
        GameObject textObject = GameObject.FindWithTag(TextUtils.DEBUG_TEXT_TAG);

        // Ensure the object exists and has a Text component
        if (textObject != null && textObject.TryGetComponent<Text>(out Text uiText))
        {
            // Append the new line text
            uiText.text += "\n" + newLineText;
        }
        else
        {
            Debug.LogWarning($"No UI Text object found with tag: {TextUtils.DEBUG_TEXT_TAG}");
        }
    }
}
