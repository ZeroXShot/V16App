using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using V16App.API;

namespace V16App.Map
{
    /// <summary>
    /// Manages beacon markers on the map with clustering support
    /// </summary>
    public class BeaconMarkerManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapManager mapManager;
        [SerializeField] private RectTransform markersContainer;
        [SerializeField] private GameObject beaconMarkerPrefab;
        [SerializeField] private GameObject clusterMarkerPrefab;
        
        [Header("Clustering")]
        [SerializeField] private bool enableClustering = true;
        [SerializeField] private float clusterRadius = 50f;
        [SerializeField] private int minClusterSize = 3;
        
        [Header("Colors")]
        [SerializeField] private Color activeColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        private List<BeaconData> _beacons = new List<BeaconData>();
        private List<GameObject> _markers = new List<GameObject>();
        
        public event System.Action<BeaconData> OnBeaconSelected;
        
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
                
                // Check if visible on screen
                if (Mathf.Abs(screenPos.x) > 1000 || Mathf.Abs(screenPos.y) > 800)
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
                    // Calculate cluster center
                    double avgLat = 0, avgLon = 0;
                    foreach (var b in cluster)
                    {
                        avgLat += b.Latitud;
                        avgLon += b.Longitud;
                    }
                    avgLat /= cluster.Count;
                    avgLon /= cluster.Count;
                    
                    Vector2 screenPos = mapManager.LatLonToScreenPosition(avgLat, avgLon);
                    
                    if (Mathf.Abs(screenPos.x) <= 1000 && Mathf.Abs(screenPos.y) <= 800)
                    {
                        CreateClusterMarker(cluster, screenPos, avgLat, avgLon);
                    }
                }
                else
                {
                    foreach (var beacon in cluster)
                    {
                        Vector2 screenPos = mapManager.LatLonToScreenPosition(beacon.Latitud, beacon.Longitud);
                        
                        if (Mathf.Abs(screenPos.x) <= 1000 && Mathf.Abs(screenPos.y) <= 800)
                        {
                            CreateBeaconMarker(beacon, screenPos);
                        }
                    }
                }
            }
        }
        
        private void CreateBeaconMarker(BeaconData beacon, Vector2 position)
        {
            GameObject marker;
            
            if (beaconMarkerPrefab != null)
            {
                marker = Instantiate(beaconMarkerPrefab, markersContainer);
            }
            else
            {
                // Create default marker
                marker = CreateDefaultMarker();
            }
            
            RectTransform rt = marker.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            
            // Set color based on status
            Image img = marker.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.color = beacon.IsActive ? activeColor : inactiveColor;
            }
            
            // Add click handler
            Button btn = marker.GetComponent<Button>();
            if (btn == null) btn = marker.AddComponent<Button>();
            
            BeaconData capturedBeacon = beacon;
            btn.onClick.AddListener(() => OnBeaconSelected?.Invoke(capturedBeacon));
            
            _markers.Add(marker);
        }
        
        private void CreateClusterMarker(List<BeaconData> cluster, Vector2 position, double lat, double lon)
        {
            GameObject marker;
            
            if (clusterMarkerPrefab != null)
            {
                marker = Instantiate(clusterMarkerPrefab, markersContainer);
            }
            else
            {
                marker = CreateDefaultClusterMarker(cluster.Count);
            }
            
            RectTransform rt = marker.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            
            // Update count text
            Text countText = marker.GetComponentInChildren<Text>();
            if (countText != null)
            {
                countText.text = cluster.Count.ToString();
            }
            
            // Add click handler to zoom in
            Button btn = marker.GetComponent<Button>();
            if (btn == null) btn = marker.AddComponent<Button>();
            
            double capturedLat = lat;
            double capturedLon = lon;
            btn.onClick.AddListener(() => {
                mapManager.SetCenter(capturedLat, capturedLon, mapManager.CurrentZoom + 2);
            });
            
            _markers.Add(marker);
        }
        
        private GameObject CreateDefaultMarker()
        {
            GameObject marker = new GameObject("BeaconMarker");
            marker.transform.SetParent(markersContainer, false);
            
            RectTransform rt = marker.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(24, 24);
            
            // Create circular marker
            Image img = marker.AddComponent<Image>();
            img.color = activeColor;
            
            // Add outline/glow effect
            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(marker.transform, false);
            RectTransform outlineRt = outline.AddComponent<RectTransform>();
            outlineRt.sizeDelta = new Vector2(28, 28);
            Image outlineImg = outline.AddComponent<Image>();
            outlineImg.color = new Color(1f, 1f, 1f, 0.8f);
            outline.transform.SetAsFirstSibling();
            
            return marker;
        }
        
        private GameObject CreateDefaultClusterMarker(int count)
        {
            GameObject marker = new GameObject("ClusterMarker");
            marker.transform.SetParent(markersContainer, false);
            
            float size = Mathf.Lerp(40, 70, Mathf.Clamp01((count - minClusterSize) / 20f));
            
            RectTransform rt = marker.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            
            Image img = marker.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            
            // Add count text
            GameObject textObj = new GameObject("Count");
            textObj.transform.SetParent(marker.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(size, size);
            textRt.anchoredPosition = Vector2.zero;
            
            Text text = textObj.AddComponent<Text>();
            text.text = count.ToString();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = (int)(size * 0.4f);
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            return marker;
        }
    }
}
