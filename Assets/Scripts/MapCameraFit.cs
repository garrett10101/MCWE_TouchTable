using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MapCameraFit : MonoBehaviour
{
    public SpriteRenderer mapRenderer;

    void Start()
    {
        Camera cam = GetComponent<Camera>();

        float mapWidth = mapRenderer.bounds.size.x;
        float mapHeight = mapRenderer.bounds.size.y;

        float screenRatio = (float)Screen.width / Screen.height;
        float mapRatio = mapWidth / mapHeight;

        if (screenRatio >= mapRatio)
        {
            cam.orthographicSize = mapHeight / 2;
        }
        else
        {
            float difference = mapRatio / screenRatio;
            cam.orthographicSize = mapHeight / 2 * difference;
        }
    }
}