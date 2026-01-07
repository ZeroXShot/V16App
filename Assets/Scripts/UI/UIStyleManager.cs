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
        [SerializeField] private Button refreshButton;
        
        [Header("Button Styling")]
        [SerializeField] private float buttonSize = 50f;
        [SerializeField] private Color buttonBgColor = new Color(0.1f, 0.14f, 0.22f, 0.8f);
        
        private Sprite _zoomInSprite;
        private Sprite _zoomOutSprite;
        private Sprite _refreshSprite;
        
        private void Awake()
        {
            Debug.Log("[UIStyleManager] Awake - Initializing UI Style Manager");
            AutoFindButtons();
            LoadIcons();
        }
        
        private void Start()
        {
            SetupZoomButtons();
        }
        
        private void AutoFindButtons()
        {
            // If any button is null, try to find it in the scene by name
            if (zoomInButton == null) zoomInButton = FindButtonByKeywords("Zoom", "In");
            if (zoomOutButton == null) zoomOutButton = FindButtonByKeywords("Zoom", "Out");
            if (refreshButton == null) refreshButton = FindButtonByKeywords("Refresh", "Reload", "Recarga");
        }

        private Button FindButtonByKeywords(params string[] keywords)
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);
            foreach (var b in allButtons)
            {
                string name = b.name.ToLower();
                bool match = true;
                foreach (var kw in keywords)
                {
                    if (!name.Contains(kw.ToLower()))
                    {
                        match = false;
                        break;
                    }
                }
                if (match) 
                {
                    Debug.Log($"[UIStyleManager] Auto-found button: {b.name} for keywords {string.Join(",", keywords)}");
                    return b;
                }
            }
            return null;
        }
        
        private void LoadIcons()
        {
            // Try loading as Sprite first
            _zoomInSprite = Resources.Load<Sprite>("icons/zoomIn");
            _zoomOutSprite = Resources.Load<Sprite>("icons/zoomOut");
            _refreshSprite = Resources.Load<Sprite>("icons/balizav16On");
            
            Debug.Log($"[UIStyleManager] Icons loading status - ZoomIn: {_zoomInSprite != null}, ZoomOut: {_zoomOutSprite != null}, Refresh: {_refreshSprite != null}");

            // Fallback: load as Texture2D and create sprites
            if (_zoomInSprite == null) _zoomInSprite = CreateSpriteFromTexture("icons/zoomIn");
            if (_zoomOutSprite == null) _zoomOutSprite = CreateSpriteFromTexture("icons/zoomOut");
            if (_refreshSprite == null) _refreshSprite = CreateSpriteFromTexture("icons/balizav16On");
        }

        private Sprite CreateSpriteFromTexture(string path)
        {
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                Debug.Log($"[UIStyleManager] Created sprite from texture: {path} ({tex.width}x{tex.height})");
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }
        
        private void SetupZoomButtons()
        {
            SetupZoomButton(zoomInButton, _zoomInSprite);
            SetupZoomButton(zoomOutButton, _zoomOutSprite);
            SetupZoomButton(refreshButton, _refreshSprite);
        }
        
        private void SetupZoomButton(Button button, Sprite iconSprite)
        {
            if (button == null)
            {
                Debug.LogWarning("[UIStyleManager] Cannot setup button: button is null");
                return;
            }
            
            // Get or create button background image
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                // If it's the placeholder, remove its sprite to only show color or set a generic one
                // buttonImage.sprite = null; // Removed to keep shape if it's a specific UI asset
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
                iconRt.anchorMin = new Vector2(0.1f, 0.1f);
                iconRt.anchorMax = new Vector2(0.9f, 0.9f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                
                iconImage = iconObj.AddComponent<Image>();
                iconChild = iconObj.transform;
            }
            
            // IMPORTANT: Ensure icon is last sibling to render ON TOP
            iconChild.SetAsLastSibling();
            
            if (iconImage != null)
            {
                if (iconSprite != null)
                {
                    iconImage.sprite = iconSprite;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                    iconImage.enabled = true;
                    Debug.Log($"[UIStyleManager] Applied sprite to button: {button.name}");
                }
                else
                {
                    Debug.LogWarning($"[UIStyleManager] No sprite to apply to button: {button.name}");
                }
            }
            
            // Setup button colors for hover effect
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;
        }
    }
}
