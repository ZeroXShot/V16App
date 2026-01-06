using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using V16App.API;

namespace V16App.Map
{
    /// <summary>
    /// Manages beacon markers on the map with clustering support and custom icons
    /// </summary>
    public class BeaconMarkerManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapManager mapManager;
        [SerializeField] private RectTransform markersContainer;
        
        [Header("Clustering")]
        [SerializeField] private bool enableClustering = true;
        [SerializeField] private float clusterRadius = 50f;
        [SerializeField] private int minClusterSize = 3;
        
        [Header("Marker Settings")]
        [SerializeField] private float individualMarkerSize = 40f;
        [SerializeField] private float clusterMinSize = 50f;
        [SerializeField] private float clusterMaxSize = 80f;
        
        private List<BeaconData> _beacons = new List<BeaconData>();
        private List<GameObject> _markers = new List<GameObject>();
        
        // Cached icon sprites
        private Sprite _beaconOnSprite;
        private Sprite _beaconOffSprite;
        
        public event System.Action<BeaconData> OnBeaconSelected;
        
        private void Awake()
        {
            LoadIcons();
        }
        
        private void LoadIcons()
        {
            // Load beacon icons from Resources/icons folder
            // Note: Icons must be imported as Sprite type in Unity (Texture Type = Sprite)
            _beaconOnSprite = Resources.Load<Sprite>("icons/balizav16On");
            _beaconOffSprite = Resources.Load<Sprite>("icons/balizav16Off");
            
            // Fallback: try loading as Texture2D and create sprites
            if (_beaconOnSprite == null)
            {
                Texture2D texOn = Resources.Load<Texture2D>("icons/balizav16On");
                if (texOn != null)
                {
                    _beaconOnSprite = Sprite.Create(texOn, new Rect(0, 0, texOn.width, texOn.height), new Vector2(0.5f, 0.5f));
                }
            }
            if (_beaconOffSprite == null)
            {
                Texture2D texOff = Resources.Load<Texture2D>("icons/balizav16Off");
                if (texOff != null)
                {
                    _beaconOffSprite = Sprite.Create(texOff, new Rect(0, 0, texOff.width, texOff.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
        
        private void Start()
        {
            if (mapManager != null)
            {
                mapManager.OnZoomChanged += _ => RefreshMarkers();
                mapManager.OnCenterChanged += (_, __) => RefreshMarkers();
            }
        }
        
        public void SetBeacons(List<BeaconData> beacons)
        {
            _beacons = beacons ?? new List<BeaconData>();
            RefreshMarkers();
        }
        
        public void RefreshMarkers()
        {
            ClearMarkers();
            
            if (enableClustering && mapManager.CurrentZoom < 12)
            {
                CreateClusteredMarkers();
            }
            else
            {
                CreateIndividualMarkers();
            }
        }
        
        private void ClearMarkers()
        {
            foreach (var marker in _markers)
            {
                if (marker != null)
                    Destroy(marker);
            }
            _markers.Clear();
        }
        
        private void CreateIndividualMarkers()
        {
            foreach (var beacon in _beacons)
            {
                Vector2 screenPos = mapManager.LatLonToScreenPosition(beacon.Latitud, beacon.Longitud);
                
                // Check if visible on screen (with margin)
                if (Mathf.Abs(screenPos.x) > 1200 || Mathf.Abs(screenPos.y) > 900)
                    continue;
                
                CreateBeaconMarker(beacon, screenPos);
            }
        }
        
        private void CreateClusteredMarkers()
        {
            List<List<BeaconData>> clusters = new List<List<BeaconData>>();
            List<bool> assigned = new List<bool>(new bool[_beacons.Count]);
            
            for (int i = 0; i < _beacons.Count; i++)
            {
                if (assigned[i]) continue;
                
                List<BeaconData> cluster = new List<BeaconData> { _beacons[i] };
                assigned[i] = true;
                
                Vector2 pos1 = mapManager.LatLonToScreenPosition(_beacons[i].Latitud, _beacons[i].Longitud);
                
                for (int j = i + 1; j < _beacons.Count; j++)
                {
                    if (assigned[j]) continue;
                    
                    Vector2 pos2 = mapManager.LatLonToScreenPosition(_beacons[j].Latitud, _beacons[j].Longitud);
                    
                    if (Vector2.Distance(pos1, pos2) < clusterRadius)
                    {
                        cluster.Add(_beacons[j]);
                        assigned[j] = true;
                    }
                }
                
                clusters.Add(cluster);
            }
            
            foreach (var cluster in clusters)
            {
                if (cluster.Count >= minClusterSize)
                {
                    double avgLat = 0, avgLon = 0;
                    int activeCount = 0;
                    foreach (var b in cluster)
                    {
                        avgLat += b.Latitud;
                        avgLon += b.Longitud;
                        if (b.IsActive) activeCount++;
                    }
                    avgLat /= cluster.Count;
                    avgLon /= cluster.Count;
                    
                    Vector2 screenPos = mapManager.LatLonToScreenPosition(avgLat, avgLon);
                    
                    if (Mathf.Abs(screenPos.x) <= 1200 && Mathf.Abs(screenPos.y) <= 900)
                    {
                        CreateClusterMarker(cluster, screenPos, avgLat, avgLon, activeCount > 0);
                    }
                }
                else
                {
                    foreach (var beacon in cluster)
                    {
                        Vector2 screenPos = mapManager.LatLonToScreenPosition(beacon.Latitud, beacon.Longitud);
                        
                        if (Mathf.Abs(screenPos.x) <= 1200 && Mathf.Abs(screenPos.y) <= 900)
                        {
                            CreateBeaconMarker(beacon, screenPos);
                        }
                    }
                }
            }
        }
        
        private void CreateBeaconMarker(BeaconData beacon, Vector2 position)
        {
            GameObject marker = new GameObject($"Beacon_{beacon.Id}");
            marker.transform.SetParent(markersContainer, false);
            
            RectTransform rt = marker.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(individualMarkerSize, individualMarkerSize);
            rt.anchoredPosition = position;
            
            // Add beacon icon image
            Image img = marker.AddComponent<Image>();
            img.preserveAspect = true;
            
            // Use appropriate icon based on beacon status
            if (beacon.IsActive && _beaconOnSprite != null)
            {
                img.sprite = _beaconOnSprite;
                img.color = Color.white;
            }
            else if (!beacon.IsActive && _beaconOffSprite != null)
            {
                img.sprite = _beaconOffSprite;
                img.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            }
            else
            {
                // Fallback if icons not loaded - colored circle
                img.color = beacon.IsActive ? 
                    new Color(1f, 0.3f, 0.3f, 1f) : 
                    new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }
            
            // Add subtle shadow/glow for active beacons
            if (beacon.IsActive)
            {
                GameObject glow = new GameObject("Glow");
                glow.transform.SetParent(marker.transform, false);
                glow.transform.SetAsFirstSibling();
                
                RectTransform glowRt = glow.AddComponent<RectTransform>();
                glowRt.sizeDelta = new Vector2(individualMarkerSize + 8, individualMarkerSize + 8);
                glowRt.anchoredPosition = Vector2.zero;
                
                Image glowImg = glow.AddComponent<Image>();
                if (_beaconOnSprite != null)
                {
                    glowImg.sprite = _beaconOnSprite;
                }
                glowImg.color = new Color(1f, 0.5f, 0.2f, 0.4f);
            }
            
            // Add button for interaction
            Button btn = marker.AddComponent<Button>();
            btn.targetGraphic = img;
            
            // Setup button colors for hover effect
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;
            
            BeaconData capturedBeacon = beacon;
            btn.onClick.AddListener(() => OnBeaconSelected?.Invoke(capturedBeacon));
            
            _markers.Add(marker);
        }
        
        private void CreateClusterMarker(List<BeaconData> cluster, Vector2 position, double lat, double lon, bool hasActive)
        {
            GameObject marker = new GameObject("ClusterMarker");
            marker.transform.SetParent(markersContainer, false);
            
            float size = Mathf.Lerp(clusterMinSize, clusterMaxSize, Mathf.Clamp01((cluster.Count - minClusterSize) / 20f));
            
            RectTransform rt = marker.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = position;
            
            // Use beacon icon for cluster
            Image img = marker.AddComponent<Image>();
            img.preserveAspect = true;
            
            if (_beaconOnSprite != null)
            {
                img.sprite = _beaconOnSprite;
                img.color = hasActive ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.9f);
            }
            else
            {
                // Fallback gradient blue
                img.color = hasActive ? 
                    new Color(0.2f, 0.6f, 1f, 0.95f) : 
                    new Color(0.4f, 0.5f, 0.6f, 0.9f);
            }
            
            // Add count badge
            GameObject badge = new GameObject("CountBadge");
            badge.transform.SetParent(marker.transform, false);
            
            RectTransform badgeRt = badge.AddComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(size * 0.5f, size * 0.35f);
            badgeRt.anchorMin = new Vector2(0.5f, 0f);
            badgeRt.anchorMax = new Vector2(0.5f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(0, -size * 0.15f);
            
            Image badgeBg = badge.AddComponent<Image>();
            badgeBg.color = hasActive ? 
                new Color(0.9f, 0.2f, 0.2f, 0.95f) : 
                new Color(0.3f, 0.3f, 0.4f, 0.9f);
            
            // Count text
            GameObject textObj = new GameObject("Count");
            textObj.transform.SetParent(badge.transform, false);
            
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            
            Text text = textObj.AddComponent<Text>();
            text.text = cluster.Count.ToString();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = (int)(size * 0.25f);
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // Add button
            Button btn = marker.AddComponent<Button>();
            btn.targetGraphic = img;
            
            double capturedLat = lat;
            double capturedLon = lon;
            btn.onClick.AddListener(() => {
                mapManager.SetCenter(capturedLat, capturedLon, mapManager.CurrentZoom + 2);
            });
            
            _markers.Add(marker);
        }
    }
}
