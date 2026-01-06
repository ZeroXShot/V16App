using UnityEngine;
using UnityEngine.UI;

namespace V16App.UI
{
    /// <summary>
    /// Manages UI styling and icon loading for the application
    /// Sets up zoom button icons from Resources at runtime
    /// </summary>
    public class UIStyleManager : MonoBehaviour
    {
        [Header("Zoom Buttons")]
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        
        [Header("Button Styling")]
        [SerializeField] private float buttonSize = 50f;
        [SerializeField] private Color buttonBgColor = new Color(0.1f, 0.14f, 0.22f, 0.9f);
        
        private Sprite _zoomInSprite;
        private Sprite _zoomOutSprite;
        
        private void Awake()
        {
            LoadIcons();
        }
        
        private void Start()
        {
            SetupZoomButtons();
        }
        
        private void LoadIcons()
        {
            _zoomInSprite = Resources.Load<Sprite>("icons/zoomIn");
            _zoomOutSprite = Resources.Load<Sprite>("icons/zoomOut");
            
            if (_zoomInSprite == null)
                Debug.LogWarning("[UIStyleManager] Could not load zoomIn icon");
            if (_zoomOutSprite == null)
                Debug.LogWarning("[UIStyleManager] Could not load zoomOut icon");
        }
        
        private void SetupZoomButtons()
        {
            SetupZoomButton(zoomInButton, _zoomInSprite);
            SetupZoomButton(zoomOutButton, _zoomOutSprite);
        }
        
        private void SetupZoomButton(Button button, Sprite iconSprite)
        {
            if (button == null) return;
            
            // Get or create button background image
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = buttonBgColor;
            }
            
            // Find or create icon child
            Transform iconChild = button.transform.Find("Icon");
            Image iconImage = null;
            
            if (iconChild != null)
            {
                iconImage = iconChild.GetComponent<Image>();
            }
            else
            {
                // Create icon child
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(button.transform, false);
                
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.15f, 0.15f);
                iconRt.anchorMax = new Vector2(0.85f, 0.85f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                
                iconImage = iconObj.AddComponent<Image>();
            }
            
            if (iconImage != null && iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
            }
            
            // Setup button colors for hover effect
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;
        }
        
        /// <summary>
        /// Apply modern glassmorphism style to a panel
        /// </summary>
        public static void ApplyGlassmorphism(Image panelImage, float alpha = 0.85f)
        {
            if (panelImage == null) return;
            panelImage.color = new Color(0.08f, 0.1f, 0.15f, alpha);
        }
    }
}
