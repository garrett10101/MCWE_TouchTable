using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a simple popup panel containing a long scrollable piece of text.  This
/// controller is designed to be attached to a UI object (for example on the same
/// GameObject as an "Open Info" button) and exposes methods to open and close
/// the panel which can be wired up to Unity UI button OnClick events in the
/// inspector.  When opening, the scroll position is reset to the top.
/// </summary>
public class ScrollableTextController : MonoBehaviour
{
    [Header("Popup Elements")]
    [Tooltip("The root GameObject of the popup panel that will be enabled/disabled.")]
    public GameObject popupPanel;

    [Tooltip("ScrollRect component used for scrolling the long text.")]
    public ScrollRect scrollRect;

    [Tooltip("Text component that displays the long content.")]
    public Text contentText;

    [Tooltip("The long body of text to display in the popup.  Assign via the inspector or at runtime.")]
    [TextArea(5, 20)]
    public string longText;

    private void Start()
    {
        // Assign the provided long text to the content Text on start.
        if (contentText != null && !string.IsNullOrEmpty(longText))
        {
            contentText.text = longText;
        }

        // Ensure the popup is hidden by default.
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Opens the popup panel and resets the scroll position to the top.  Can be
    /// wired to a UI Button's OnClick event.
    /// </summary>
    public void OpenPopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
        // Reset the scroll to the top (1 means top for verticalNormalizedPosition).
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// Closes the popup panel.  Can be wired to a UI Button's OnClick event.
    /// </summary>
    public void ClosePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }
}