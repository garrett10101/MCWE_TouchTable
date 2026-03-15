using UnityEngine;

public class MapMarker : MonoBehaviour
{
    [Header("Marker Data")]
    [SerializeField] private string markerTitle = "New Marker";
    [SerializeField] private string markerDescription = "Description goes here.";

    public string MarkerTitle => markerTitle;
    public string MarkerDescription => markerDescription;
}