using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a slider to blend between multiple layered background images.  As the slider
/// value moves between integer indices (0 to N-1) the nearest background becomes
/// fully opaque while neighbouring backgrounds fade in and out proportionally to the
/// distance from the slider value.  This controller also optionally updates a
/// label with a human‑readable name for the current time of day.
/// </summary>
public class BackgroundBlendController : MonoBehaviour
{
    [Header("Background Images")]
    [Tooltip("Ordered list of UI Image components representing the backgrounds.  Index 0 is the first (e.g. Morning).  Ensure they are layered in the UI so later images appear on top.")]
    public Image[] backgroundImages;

    [Header("Slider")]
    [Tooltip("Slider controlling the interpolation between backgrounds.  The slider's min and max values will be set automatically based on the number of backgrounds.")]
    public Slider timeOfDaySlider;

    [Header("Label (Optional)")]
    [Tooltip("Text component to display the name of the nearest time of day.  Leave null if not used.")]
    public Text timeOfDayLabel;

    [Tooltip("Human‑friendly names for each background.  Should have the same length as backgroundImages.")]
    public string[] labels;

    private void Start()
    {
        // Validate array lengths.
        if (backgroundImages == null || backgroundImages.Length == 0)
        {
            Debug.LogWarning("BackgroundBlendController requires at least one background image.");
            return;
        }

        // If no labels provided, create default numeric labels.
        if (labels == null || labels.Length != backgroundImages.Length)
        {
            labels = new string[backgroundImages.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = $"{i}";
            }
        }

        if (timeOfDaySlider != null)
        {
            // Configure slider to cover the range of indices.
            timeOfDaySlider.minValue = 0f;
            timeOfDaySlider.maxValue = backgroundImages.Length - 1;
            timeOfDaySlider.wholeNumbers = false;
            timeOfDaySlider.onValueChanged.AddListener(OnSliderValueChanged);
            // Initialize the blend with the current value.
            OnSliderValueChanged(timeOfDaySlider.value);
        }
    }

    private void OnDestroy()
    {
        // Clean up listener to avoid leaks.
        if (timeOfDaySlider != null)
        {
            timeOfDaySlider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }

    /// <summary>
    /// Called whenever the slider value changes.  Updates the alpha of each
    /// background image based on the distance to its index and updates the
    /// label accordingly.
    /// </summary>
    /// <param name="value">Slider value between 0 and backgroundImages.Length‑1.</param>
    private void OnSliderValueChanged(float value)
    {
        // Blend backgrounds: the nearest index has alpha 1, others fade linearly.
        for (int i = 0; i < backgroundImages.Length; i++)
        {
            Image img = backgroundImages[i];
            if (img != null)
            {
                float alpha = Mathf.Clamp01(1f - Mathf.Abs(value - i));
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }

        // Update label to show the nearest index's name.
        if (timeOfDayLabel != null && labels != null && labels.Length == backgroundImages.Length)
        {
            int nearestIndex = Mathf.Clamp(Mathf.RoundToInt(value), 0, labels.Length - 1);
            timeOfDayLabel.text = labels[nearestIndex];
        }
    }
}