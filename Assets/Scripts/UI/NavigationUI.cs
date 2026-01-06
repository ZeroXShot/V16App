using UnityEngine;
using UnityEngine.UI;
using V16App.Map;

namespace V16App.UI
{
    /// <summary>
    /// Navigation UI with search, filters, and zoom controls
    /// </summary>
    public class NavigationUI : MonoBehaviour
    {
        [Header("Map Reference")]
        [SerializeField] private MapManager mapManager;
        
        [Header("Search")]
        [SerializeField] private InputField searchInput;
        [SerializeField] private Button searchButton;
        [SerializeField] private GameObject searchResultsPanel;
        [SerializeField] private Transform searchResultsContainer;
        [SerializeField] private GameObject searchResultItemPrefab;
        
        [Header("Filter Buttons")]
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterActiveButton;
        [SerializeField] private Toggle showClustersToggle;
        
        [Header("Quick Navigation")]
        [SerializeField] private Button[] communityButtons;
        
        [Header("Zoom Controls")]
        [SerializeField] private Slider zoomSlider;
        
        // Community centers (approximate)
        private readonly (string name, double lat, double lon)[] _communities = new[]
        {
            ("Andalucía", 37.3891, -5.9845),
            ("Aragón", 41.6488, -0.8891),
            ("Asturias", 43.3614, -5.8593),
            ("Baleares", 39.5697, 2.6502),
            ("Canarias", 28.2916, -16.6291),
            ("Cantabria", 43.1828, -3.9878),
            ("Castilla-La Mancha", 39.4702, -3.6677),
            ("Castilla y León", 41.6520, -4.7286),
            ("Cataluña", 41.5912, 1.5209),
            ("Ceuta", 35.8893, -5.3213),
            ("Extremadura", 39.4937, -6.0679),
            ("Galicia", 42.7756, -7.8661),
            ("La Rioja", 42.2871, -2.5396),
            ("Madrid", 40.4168, -3.7038),
            ("Melilla", 35.2923, -2.9381),
            ("Murcia", 37.9922, -1.1307),
            ("Navarra", 42.6954, -1.6761),
            ("País Vasco", 42.9896, -2.6189),
            ("Valencia", 39.4699, -0.3763)
        };
        
        private void Start()
        {
            SetupSearchListeners();
            SetupZoomSlider();
            SetupCommunityButtons();
        }
        
        private void SetupSearchListeners()
        {
            if (searchButton != null)
                searchButton.onClick.AddListener(PerformSearch);
            
            if (searchInput != null)
            {
                searchInput.onEndEdit.AddListener(text => {
                    if (Input.GetKeyDown(KeyCode.Return))
                        PerformSearch();
                });
            }
        }
        
        private void SetupZoomSlider()
        {
            if (zoomSlider == null || mapManager == null) return;
            
            zoomSlider.minValue = 4;
            zoomSlider.maxValue = 18;
            zoomSlider.wholeNumbers = true;
            zoomSlider.value = mapManager.CurrentZoom;
            
            zoomSlider.onValueChanged.AddListener(value => {
                mapManager.SetZoom((int)value);
            });
            
            mapManager.OnZoomChanged += zoom => {
                zoomSlider.value = zoom;
            };
        }
        
        private void SetupCommunityButtons()
        {
            if (communityButtons == null) return;
            
            for (int i = 0; i < communityButtons.Length && i < _communities.Length; i++)
            {
                int index = i;
                var community = _communities[i];
                
                if (communityButtons[i] != null)
                {
                    var textComponent = communityButtons[i].GetComponentInChildren<Text>();
                    if (textComponent != null)
                        textComponent.text = community.name;
                    
                    communityButtons[i].onClick.AddListener(() => {
                        NavigateToCommunity(index);
                    });
                }
            }
        }
        
        private void NavigateToCommunity(int index)
        {
            if (index < 0 || index >= _communities.Length || mapManager == null)
                return;
            
            var community = _communities[index];
            mapManager.SetCenter(community.lat, community.lon, 8);
        }
        
        private void PerformSearch()
        {
            if (searchInput == null || string.IsNullOrEmpty(searchInput.text))
                return;
            
            string query = searchInput.text.Trim().ToLower();
            
            // Check if it's a community name
            for (int i = 0; i < _communities.Length; i++)
            {
                if (_communities[i].name.ToLower().Contains(query))
                {
                    NavigateToCommunity(i);
                    return;
                }
            }
            
            // Check if it's coordinates (lat,lon)
            if (TryParseCoordinates(query, out double lat, out double lon))
            {
                mapManager?.SetCenter(lat, lon, 14);
                return;
            }
            
            // Otherwise search beacons (handled by AppManager)
            Debug.Log($"[NavigationUI] Searching for: {query}");
        }
        
        private bool TryParseCoordinates(string input, out double lat, out double lon)
        {
            lat = lon = 0;
            
            string[] parts = input.Replace(" ", "").Split(',');
            if (parts.Length != 2) return false;
            
            return double.TryParse(parts[0], System.Globalization.NumberStyles.Any,
                                   System.Globalization.CultureInfo.InvariantCulture, out lat) &&
                   double.TryParse(parts[1], System.Globalization.NumberStyles.Any,
                                   System.Globalization.CultureInfo.InvariantCulture, out lon);
        }
        
        /// <summary>
        /// Quick navigation to a province by name
        /// </summary>
        public void NavigateToProvince(string provinceName)
        {
            // Province centers would need a separate lookup table
            // For now, search and center on first match
            Debug.Log($"[NavigationUI] Navigating to province: {provinceName}");
        }
    }
}
