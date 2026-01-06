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
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
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
            
            // Mouse/Trackpad scroll for zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                // Allow zoom if not over a high-priority UI panel like Detail or Analytics
                bool overPanel = (detailPanel != null && detailPanel.gameObject.activeSelf && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) ||
                                (analyticsPanel != null && analyticsPanel.gameObject.activeSelf && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
                
                // Rate limit zoom to prevent trackpad sensitivity explosion
                if (!overPanel && Time.time - _lastZoomTime >= ZOOM_COOLDOWN)
                {
                    mapManager?.OnScroll(scroll);
                    _lastZoomTime = Time.time;
                }
            }
            
            // Mouse drag for pan
            if (Input.GetMouseButtonDown(0))
            {
                // Allow pan if not over a high-priority UI panel
                bool overPanel = (detailPanel != null && detailPanel.gameObject.activeSelf && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) ||
                                (analyticsPanel != null && analyticsPanel.gameObject.activeSelf && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) ||
                                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() && !IsPointerOverMap());
                
                if (!overPanel)
                {
                    mapManager?.OnDragStart(Input.mousePosition);
                }
            }
            
            if (Input.GetMouseButton(0))
            {
                mapManager?.OnDrag(Input.mousePosition);
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                mapManager?.OnDragEnd();
            }
            
            // Touch handling for mobile
            HandleTouchInput();
        }
        
        private void HandleTouchInput()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                
                if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            mapManager?.OnDragStart(touch.position);
                            break;
                        case TouchPhase.Moved:
                            mapManager?.OnDrag(touch.position);
                            break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            mapManager?.OnDragEnd();
                            break;
                    }
                }
            }
            else if (Input.touchCount == 2)
            {
                // Pinch to zoom
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                
                Vector2 prevPos0 = touch0.position - touch0.deltaPosition;
                Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
                
                float prevMagnitude = (prevPos0 - prevPos1).magnitude;
                float currentMagnitude = (touch0.position - touch1.position).magnitude;
                
                float difference = currentMagnitude - prevMagnitude;
                
                if (Mathf.Abs(difference) > 10f)
                {
                    mapManager?.OnScroll(difference * 0.01f);
                }
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
