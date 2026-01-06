using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using V16App.API;
using V16App.Map;
using V16App.UI;

namespace V16App.Core
{
    /// <summary>
    /// Main application controller that coordinates all components
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        [Header("Core Components")]
        [SerializeField] private DGTApiService apiService;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private BeaconMarkerManager markerManager;
        
        [Header("UI Panels")]
        [SerializeField] private BeaconDetailPanel detailPanel;
        [SerializeField] private AnalyticsPanel analyticsPanel;
        [SerializeField] private GameObject loadingOverlay;
        
        [Header("Status Bar")]
        [SerializeField] private UnityEngine.UI.Text statusText;
        [SerializeField] private UnityEngine.UI.Text beaconCountText;
        [SerializeField] private UnityEngine.UI.Text zoomLevelText;
        
        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button refreshButton;
        [SerializeField] private UnityEngine.UI.Button analyticsButton;
        [SerializeField] private UnityEngine.UI.Button zoomInButton;
        [SerializeField] private UnityEngine.UI.Button zoomOutButton;
        [SerializeField] private UnityEngine.UI.Button centerSpainButton;
        
        // Input control
        private float _lastZoomTime;
        private bool _pinchMode;
        private const float ZOOM_COOLDOWN = 0.05f; // reduced cooldown for nicer feel but preventing explosion
        
        private void Awake()
        {
            Application.targetFrameRate = 60;
        }
        
        private void Start()
        {
            if (detailPanel != null) detailPanel.Hide();
            if (analyticsPanel != null) analyticsPanel.Hide();
            
            SetupEventListeners();
            SetupButtons();
            UpdateStatusBar();
        }
        
        private void SetupEventListeners()
        {
            if (apiService != null)
            {
                apiService.OnDataReceived += OnBeaconsReceived;
                apiService.OnError += OnApiError;
            }
            
            if (markerManager != null)
            {
                markerManager.OnBeaconSelected += OnBeaconSelected;
            }
            
            if (mapManager != null)
            {
                mapManager.OnZoomChanged += _ => UpdateStatusBar();
            }
        }
        
        private void SetupButtons()
        {
            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshData);
            
            if (analyticsButton != null)
                analyticsButton.onClick.AddListener(() => analyticsPanel?.Show());
            
            if (zoomInButton != null)
                zoomInButton.onClick.AddListener(() => mapManager?.ZoomIn());
            
            if (zoomOutButton != null)
                zoomOutButton.onClick.AddListener(() => mapManager?.ZoomOut());
            
            if (centerSpainButton != null)
                centerSpainButton.onClick.AddListener(CenterOnSpain);
        }
        
        private void Update()
        {
            HandleInput();
            UpdateLoadingState();
        }
        
        private void HandleInput()
        {
            // Keyboard zoom controls
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
                mapManager?.ZoomIn();
            
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                mapManager?.ZoomOut();
            
            // Escape to close panels
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                detailPanel?.Hide();
                analyticsPanel?.Hide();
            }
            
            // R to refresh
            if (Input.GetKeyDown(KeyCode.R) && !Input.GetKey(KeyCode.LeftControl))
                RefreshData();
            
            // Mouse/Trackpad scroll for zoom - with improved sensitivity
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                // Check if over UI panels that should block zoom
                bool overBlockingUI = IsPointerOverBlockingUI();
                
                // Apply zoom with rate limiting
                if (!overBlockingUI && Time.time - _lastZoomTime >= ZOOM_COOLDOWN)
                {
                    // Amplify scroll for better responsiveness with trackpads
                    float amplifiedScroll = scroll * 2f;
                    mapManager?.OnScroll(amplifiedScroll);
                    _lastZoomTime = Time.time;
                }
            }
            
            // Middle mouse button drag for pan (button 2)
            if (Input.GetMouseButtonDown(2))
            {
                bool overBlockingUI = IsPointerOverBlockingUI();
                
                if (!overBlockingUI)
                {
                    mapManager?.OnDragStart(Input.mousePosition);
                }
            }
            
            if (Input.GetMouseButton(2))
            {
                mapManager?.OnDrag(Input.mousePosition);
            }
            
            if (Input.GetMouseButtonUp(2))
            {
                mapManager?.OnDragEnd();
            }
            
            // Touch handling for mobile
            HandleTouchInput();
        }
        
        private bool IsPointerOverBlockingUI()
        {
            if (EventSystem.current == null) return false;
            
            // Check if detail panel or analytics panel is open and pointer is over them
            if (detailPanel != null && detailPanel.gameObject.activeSelf)
            {
                GameObject panelRoot = detailPanel.gameObject.transform.parent?.gameObject ?? detailPanel.gameObject;
                if (IsPointerOverGameObject(panelRoot))
                    return true;
            }
            
            if (analyticsPanel != null && analyticsPanel.gameObject.activeSelf)
            {
                GameObject panelRoot = analyticsPanel.gameObject.transform.parent?.gameObject ?? analyticsPanel.gameObject;
                if (IsPointerOverGameObject(panelRoot))
                    return true;
            }
            
            return false;
        }
        
        private bool IsPointerOverGameObject(GameObject target)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;
            
            System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (var result in results)
            {
                if (result.gameObject == target || result.gameObject.transform.IsChildOf(target.transform))
                {
                    return true;
                }
            }
            return false;
        }
        
        private void HandleTouchInput()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                
                // Skip if over UI element
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    return;
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _pinchMode = false;
                        mapManager?.OnDragStart(touch.position);
                        break;
                    case TouchPhase.Moved:
                        if (!_pinchMode)
                            mapManager?.OnDrag(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        mapManager?.OnDragEnd();
                        break;
                }
            }
            else if (Input.touchCount == 2)
            {
                _pinchMode = true;
                mapManager?.OnDragEnd(); // Cancel any drag
                
                // Pinch to zoom
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                
                Vector2 prevPos0 = touch0.position - touch0.deltaPosition;
                Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
                
                float prevMagnitude = (prevPos0 - prevPos1).magnitude;
                float currentMagnitude = (touch0.position - touch1.position).magnitude;
                
                float difference = currentMagnitude - prevMagnitude;
                
                // Threshold for pinch detection
                if (Mathf.Abs(difference) > 5f && Time.time - _lastZoomTime >= ZOOM_COOLDOWN)
                {
                    mapManager?.OnScroll(difference * 0.02f);
                    _lastZoomTime = Time.time;
                }
            }
            else if (Input.touchCount == 0)
            {
                _pinchMode = false;
            }
        }
        
        private void UpdateLoadingState()
        {
            if (loadingOverlay != null && apiService != null)
            {
                loadingOverlay.SetActive(apiService.IsLoading);
            }
        }
        
        private void OnBeaconsReceived(List<BeaconData> beacons)
        {
            Debug.Log($"[AppManager] Received {beacons.Count} beacons");
            
            markerManager?.SetBeacons(beacons);
            analyticsPanel?.UpdateData(beacons);
            
            UpdateStatusBar();
            SetStatus($"Actualizado: {beacons.Count} balizas cargadas");
        }
        
        private void OnApiError(string error)
        {
            Debug.LogError($"[AppManager] API Error: {error}");
            SetStatus($"Error: {error}");
        }
        
        private void OnBeaconSelected(BeaconData beacon)
        {
            Debug.Log($"[AppManager] Beacon selected: {beacon.GetDisplayName()}");
            detailPanel?.Show(beacon);
        }
        
        private void RefreshData()
        {
            SetStatus("Actualizando datos...");
            apiService?.RefreshData();
        }
        
        private void CenterOnSpain()
        {
            mapManager?.SetCenter(40.4168, -3.7038, 6);
        }
        
        private void UpdateStatusBar()
        {
            if (beaconCountText != null && apiService != null)
            {
                int count = apiService.CachedBeacons?.Count ?? 0;
                beaconCountText.text = $"{count} balizas activas";
            }
            
            if (zoomLevelText != null && mapManager != null)
            {
                zoomLevelText.text = $"Zoom: {mapManager.CurrentZoom} ({mapManager.GetZoomLevelName()})";
            }
        }
        
        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            
            Debug.Log($"[Status] {message}");
        }
        
        /// <summary>
        /// Navigate to a specific beacon
        /// </summary>
        public void NavigateToBeacon(BeaconData beacon)
        {
            if (beacon == null || mapManager == null) return;
            
            mapManager.SetCenter(beacon.Latitud, beacon.Longitud, 15);
            detailPanel?.Show(beacon);
        }
        
        /// <summary>
        /// Search beacons by road name
        /// </summary>
        public List<BeaconData> SearchByRoad(string roadName)
        {
            if (apiService?.CachedBeacons == null) return new List<BeaconData>();
            
            return apiService.CachedBeacons.FindAll(b => 
                b.Carretera?.ToLower().Contains(roadName.ToLower()) == true);
        }
        
        /// <summary>
        /// Filter beacons by community
        /// </summary>
        public List<BeaconData> FilterByCommunity(string community)
        {
            if (apiService?.CachedBeacons == null) return new List<BeaconData>();
            
            return apiService.CachedBeacons.FindAll(b => 
                b.ComunidadAutonoma?.ToLower().Contains(community.ToLower()) == true);
        }
        
        /// <summary>
        /// Check if the pointer is over the map container UI
        /// </summary>
        private bool IsPointerOverMap()
        {
            if (EventSystem.current == null) return false;
            
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (var result in results)
            {
                if (result.gameObject == mapManager.gameObject || result.gameObject.transform.IsChildOf(mapManager.transform))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
