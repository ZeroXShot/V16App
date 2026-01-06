using UnityEngine;
using UnityEngine.UI;

namespace V16App.Map
{
    /// <summary>
    /// Sets up the MapContainer with proper clipping and styling at runtime
    /// Adds RectMask2D to prevent markers from rendering outside the map area
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MapSetup : MonoBehaviour
    {
        [Header("Container References")]
        [SerializeField] private RectTransform markersContainer;
        
        [Header("Clipping Settings")]
        [SerializeField] private bool enableMarkerClipping = true;
        [SerializeField] private Vector4 clipPadding = new Vector4(0, 0, 0, 0); // Left, Bottom, Right, Top
        
        private RectMask2D _mask;
        
        private void Awake()
        {
            SetupClipping();
        }
        
        private void SetupClipping()
        {
            if (!enableMarkerClipping) return;
            
            // Add RectMask2D to the map container for proper clipping
            _mask = GetComponent<RectMask2D>();
            if (_mask == null)
            {
                _mask = gameObject.AddComponent<RectMask2D>();
            }
            
            // Set softness for smooth edges
            _mask.softness = new Vector2Int(2, 2);
            _mask.padding = clipPadding;
            
            Debug.Log("[MapSetup] Added RectMask2D for marker clipping");
        }
        
        /// <summary>
        /// Called when the map container needs to be resized to fullscreen
        /// </summary>
        public void SetFullscreen()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }
}
