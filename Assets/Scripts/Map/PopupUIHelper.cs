using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class PopupUIHelper
{
    // Returns anchoredPosition for a popup with pivot (0,1) on a center-anchored canvas.
    // Position = marker world pos converted to canvas local coords, plus offset, clamped on-screen.
    public static Vector2 GetPopupAnchoredPosition(
        Vector3 markerWorldPos, Vector2 offset,
        RectTransform canvasRect, float popupWidth, float popupHeight,
        Camera worldCam, Camera uiCam)
    {
        Vector2 screenPos = worldCam != null
            ? worldCam.WorldToScreenPoint(markerWorldPos)
            : Camera.main.WorldToScreenPoint(markerWorldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, uiCam, out Vector2 local);

        Vector2 target = local + offset;
        float hw = canvasRect.rect.width  * 0.5f;
        float hh = canvasRect.rect.height * 0.5f;
        target.x = Mathf.Clamp(target.x, -hw, hw - popupWidth);
        target.y = Mathf.Clamp(target.y, -hh + popupHeight, hh);
        return target;
    }

    // All style values supplied by caller from Inspector fields — no defaults.
    public static void CreateCloseButton(
        GameObject parent, System.Action onClose,
        float size, float inset, Color buttonColor, float labelFontSize, Color labelColor)
    {
        var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(parent.transform, false);
        var closeRect = closeGo.GetComponent<RectTransform>();
        closeRect.anchorMin        = new Vector2(1f, 1f);
        closeRect.anchorMax        = new Vector2(1f, 1f);
        closeRect.pivot            = new Vector2(1f, 1f);
        closeRect.sizeDelta        = new Vector2(size, size);
        closeRect.anchoredPosition = new Vector2(-inset, -inset);
        closeGo.GetComponent<Image>().color = buttonColor;
        closeGo.GetComponent<Button>().onClick.AddListener(() => onClose?.Invoke());

        var closeLbl = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeLbl.transform.SetParent(closeGo.transform, false);
        var clr = closeLbl.GetComponent<RectTransform>();
        clr.anchorMin = Vector2.zero; clr.anchorMax = Vector2.one;
        clr.offsetMin = Vector2.zero; clr.offsetMax = Vector2.zero;
        var clTmp = closeLbl.GetComponent<TextMeshProUGUI>();
        clTmp.text      = "X";
        clTmp.alignment = TextAlignmentOptions.Center;
        clTmp.fontSize  = labelFontSize;
        clTmp.fontStyle = FontStyles.Bold;
        clTmp.color     = labelColor;

        closeGo.transform.SetAsLastSibling();
    }

    public static Sprite CreateFallbackSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float c = size / 2f, r = c - 1f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - c, dy = y - c;
            pixels[y * size + x] = dx * dx + dy * dy <= r * r
                ? new Color32(255, 255, 255, 255)
                : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

}
