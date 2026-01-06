using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using V16App.API;

namespace V16App.Map
{
    /// <summary>
    /// Manages the interactive tile-based map using OpenStreetMap tiles
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private int initialZoom = 6;
        [SerializeField] private double initialLatitude = 40.4168;  // Madrid
        [SerializeField] private double initialLongitude = -3.7038;
        [SerializeField] private int tileSize = 256;
        [SerializeField] private int tilesPerAxis = 5;
        
        [Header("Tile Providers")]
        [SerializeField] private string tileUrlTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
        
        [Header("Zoom Limits")]
        [SerializeField] private int minZoom = 4;
        [SerializeField] private int maxZoom = 18;
        
        [Header("References")]
        [SerializeField] private RectTransform tilesContainer;
        [SerializeField] private RectTransform markersContainer;
        
        private int _currentZoom;
        private double _centerLat;
        private double _centerLon;
        private Vector2 _mapOffset;
        private Dictionary<string, RawImage> _loadedTiles = new Dictionary<string, RawImage>();
        private Dictionary<string, Texture2D> _tileCache = new Dictionary<string, Texture2D>();
        
        public int CurrentZoom => _currentZoom;
        public double CenterLatitude => _centerLat;
        public double CenterLongitude => _centerLon;
        
        public event Action<int> OnZoomChanged;
        public event Action<double, double> OnCenterChanged;
        
        private bool _isDragging;
        private Vector2 _lastDragPosition;
        
        private void Awake()
        {
            _currentZoom = initialZoom;
            _centerLat = initialLatitude;
            _centerLon = initialLongitude;
        }
        
        private void Start()
        {
            RefreshTiles();
        }
        
        public void SetCenter(double lat, double lon, int? zoom = null)
        {
            _centerLat = lat;
            _centerLon = lon;
            if (zoom.HasValue)
            {
                _currentZoom = Mathf.Clamp(zoom.Value, minZoom, maxZoom);
            }
            RefreshTiles();
            OnCenterChanged?.Invoke(_centerLat, _centerLon);
        }
        
        public void ZoomIn()
        {
            if (_currentZoom < maxZoom)
            {
                _currentZoom++;
                RefreshTiles();
                OnZoomChanged?.Invoke(_currentZoom);
            }
        }
        
        public void ZoomOut()
        {
            if (_currentZoom > minZoom)
            {
                _currentZoom--;
                RefreshTiles();
                OnZoomChanged?.Invoke(_currentZoom);
            }
        }
        
        public void SetZoom(int zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            RefreshTiles();
            OnZoomChanged?.Invoke(_currentZoom);
        }
        
        public void Pan(Vector2 delta)
        {
            // Convert pixel delta to lat/lon delta
            double scale = 256.0 * Math.Pow(2, _currentZoom);
            double lonDelta = (delta.x / scale) * 360.0;
            double latDelta = (delta.y / scale) * 180.0;
            
            _centerLon -= lonDelta;
            _centerLat -= latDelta;
            
            // Clamp latitude
            _centerLat = Mathf.Clamp((float)_centerLat, -85f, 85f);
            
            // Wrap longitude
            while (_centerLon > 180) _centerLon -= 360;
            while (_centerLon < -180) _centerLon += 360;
            
            RefreshTiles();
            OnCenterChanged?.Invoke(_centerLat, _centerLon);
        }
        
        private void RefreshTiles()
        {
            // Clear old tiles
            foreach (var tile in _loadedTiles.Values)
            {
                if (tile != null)
                    Destroy(tile.gameObject);
            }
            _loadedTiles.Clear();
            
            // Calculate center tile
            var centerTile = LatLonToTile(_centerLat, _centerLon, _currentZoom);
            
            // Load tiles around center
            int halfTiles = tilesPerAxis / 2;
            
            for (int dx = -halfTiles; dx <= halfTiles; dx++)
            {
                for (int dy = -halfTiles; dy <= halfTiles; dy++)
                {
                    int tileX = centerTile.x + dx;
                    int tileY = centerTile.y + dy;
                    
                    // Wrap tile X
                    int maxTile = (int)Math.Pow(2, _currentZoom);
                    while (tileX < 0) tileX += maxTile;
                    while (tileX >= maxTile) tileX -= maxTile;
                    
                    // Skip invalid Y tiles
                    if (tileY < 0 || tileY >= maxTile) continue;
                    
                    StartCoroutine(LoadTile(tileX, tileY, _currentZoom, dx, dy));
                }
            }
        }
        
        private IEnumerator LoadTile(int x, int y, int z, int offsetX, int offsetY)
        {
            string tileKey = $"{z}/{x}/{y}";
            string url = tileUrlTemplate.Replace("{z}", z.ToString())
                                        .Replace("{x}", x.ToString())
                                        .Replace("{y}", y.ToString());
            
            // Check cache
            if (_tileCache.TryGetValue(tileKey, out Texture2D cachedTex))
            {
                CreateTileImage(cachedTex, offsetX, offsetY, tileKey);
                yield break;
            }
            
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                request.SetRequestHeader("User-Agent", "V16BeaconTracker/1.0");
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    _tileCache[tileKey] = texture;
                    CreateTileImage(texture, offsetX, offsetY, tileKey);
                }
            }
        }
        
        private void CreateTileImage(Texture2D texture, int offsetX, int offsetY, string key)
        {
            if (_loadedTiles.ContainsKey(key)) return;
            
            GameObject tileObj = new GameObject($"Tile_{key}");
            tileObj.transform.SetParent(tilesContainer, false);
            
            RawImage img = tileObj.AddComponent<RawImage>();
            img.texture = texture;
            
            RectTransform rt = tileObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(tileSize, tileSize);
            rt.anchoredPosition = new Vector2(offsetX * tileSize, -offsetY * tileSize);
            
            _loadedTiles[key] = img;
        }
        
        /// <summary>
        /// Convert lat/lon to tile coordinates
        /// </summary>
        public (int x, int y) LatLonToTile(double lat, double lon, int zoom)
        {
            int n = (int)Math.Pow(2, zoom);
            int x = (int)((lon + 180.0) / 360.0 * n);
            int y = (int)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 
                    1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * n);
            return (x, y);
        }
        
        /// <summary>
        /// Convert lat/lon to screen position relative to map center
        /// </summary>
        public Vector2 LatLonToScreenPosition(double lat, double lon)
        {
            double scale = tileSize * Math.Pow(2, _currentZoom);
            
            // Convert center to pixels
            double centerX = (_centerLon + 180.0) / 360.0 * scale;
            double centerY = (1.0 - Math.Log(Math.Tan(_centerLat * Math.PI / 180.0) + 
                            1.0 / Math.Cos(_centerLat * Math.PI / 180.0)) / Math.PI) / 2.0 * scale;
            
            // Convert point to pixels
            double pointX = (lon + 180.0) / 360.0 * scale;
            double pointY = (1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 
                           1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * scale;
            
            return new Vector2((float)(pointX - centerX), (float)(centerY - pointY));
        }
        
        /// <summary>
        /// Convert screen position to lat/lon
        /// </summary>
        public (double lat, double lon) ScreenPositionToLatLon(Vector2 screenPos)
        {
            double scale = tileSize * Math.Pow(2, _currentZoom);
            
            double centerX = (_centerLon + 180.0) / 360.0 * scale;
            double centerY = (1.0 - Math.Log(Math.Tan(_centerLat * Math.PI / 180.0) + 
                            1.0 / Math.Cos(_centerLat * Math.PI / 180.0)) / Math.PI) / 2.0 * scale;
            
            double pointX = centerX + screenPos.x;
            double pointY = centerY - screenPos.y;
            
            double lon = pointX / scale * 360.0 - 180.0;
            double n = Math.PI - 2.0 * Math.PI * pointY / scale;
            double lat = 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
            
            return (lat, lon);
        }
        
        // Input handling for pan/zoom
        public void OnDragStart(Vector2 position)
        {
            _isDragging = true;
            _lastDragPosition = position;
        }
        
        public void OnDrag(Vector2 position)
        {
            if (_isDragging)
            {
                Vector2 delta = position - _lastDragPosition;
                Pan(delta);
                _lastDragPosition = position;
            }
        }
        
        public void OnDragEnd()
        {
            _isDragging = false;
        }
        
        public void OnScroll(float delta)
        {
            if (delta > 0)
                ZoomIn();
            else if (delta < 0)
                ZoomOut();
        }
        
        /// <summary>
        /// Get current zoom level name for display
        /// </summary>
        public string GetZoomLevelName()
        {
            if (_currentZoom <= 5) return "País";
            if (_currentZoom <= 8) return "Comunidad";
            if (_currentZoom <= 11) return "Provincia";
            if (_currentZoom <= 14) return "Ciudad";
            return "Calle";
        }
    }
}
