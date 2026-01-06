using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace V16App.API
{
    /// <summary>
    /// Service for fetching V16 beacon data from DGT eTraffic API
    /// </summary>
    public class DGTApiService : MonoBehaviour
    {
        private const string API_URL = "https://etraffic.dgt.es/etrafficWEB/api/cache/getFilteredData";
        private const byte XOR_KEY = 0x4B;
        
        [Header("Configuration")]
        [SerializeField] private float refreshInterval = 60f;
        [SerializeField] private bool autoRefresh = true;
        
        public event Action<List<BeaconData>> OnDataReceived;
        public event Action<string> OnError;
        
        private List<BeaconData> _cachedBeacons = new List<BeaconData>();
        public List<BeaconData> CachedBeacons => _cachedBeacons;
        
        public bool IsLoading { get; private set; }
        
        private void Start()
        {
            if (autoRefresh)
            {
                StartCoroutine(AutoRefreshCoroutine());
            }
        }
        
        private IEnumerator AutoRefreshCoroutine()
        {
            while (true)
            {
                yield return FetchBeaconData();
                yield return new WaitForSeconds(refreshInterval);
            }
        }
        
        public Coroutine RefreshData()
        {
            return StartCoroutine(FetchBeaconData());
        }
        
        private IEnumerator FetchBeaconData()
        {
            if (IsLoading) yield break;
            
            IsLoading = true;
            
            string requestBody = "{\"filtrosVia\":[\"Otras vialidades\"],\"filtrosCausa\":[]}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            
            using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json, text/plain, */*");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string encodedData = request.downloadHandler.text;
                        string decodedJson = DecodeResponse(encodedData);
                        ParseBeaconData(decodedJson);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"Error parsing data: {ex.Message}");
                        Debug.LogError($"[DGTApiService] Parse error: {ex}");
                    }
                }
                else
                {
                    OnError?.Invoke($"Network error: {request.error}");
                    Debug.LogError($"[DGTApiService] Network error: {request.error}");
                }
            }
            
            IsLoading = false;
        }
        
        private string DecodeResponse(string encodedData)
        {
            // Decode Base64
            byte[] base64Decoded = Convert.FromBase64String(encodedData);
            
            // XOR decrypt with key 0x4B
            byte[] decrypted = new byte[base64Decoded.Length];
            for (int i = 0; i < base64Decoded.Length; i++)
            {
                decrypted[i] = (byte)(base64Decoded[i] ^ XOR_KEY);
            }
            
            return Encoding.UTF8.GetString(decrypted);
        }
        
        private void ParseBeaconData(string json)
        {
            var response = JsonUtility.FromJson<ApiResponse>(json);
            
            if (response?.situationsRecords == null)
            {
                _cachedBeacons.Clear();
                OnDataReceived?.Invoke(_cachedBeacons);
                return;
            }
            
            _cachedBeacons.Clear();
            
            foreach (var record in response.situationsRecords)
            {
                var beacon = ConvertToBeaconData(record);
                if (beacon != null)
                {
                    _cachedBeacons.Add(beacon);
                }
            }
            
            Debug.Log($"[DGTApiService] Loaded {_cachedBeacons.Count} beacons");
            OnDataReceived?.Invoke(_cachedBeacons);
        }
        
        private BeaconData ConvertToBeaconData(SituationRecord record)
        {
            // Parse geometry to get coordinates
            if (string.IsNullOrEmpty(record.geometria)) return null;
            
            var coords = ParseGeometry(record.geometria);
            if (coords == null) return null;
            
            return new BeaconData
            {
                Id = record.id,
                SituationId = record.situationId,
                Estado = record.estado ?? "active",
                Latitud = coords.Value.lat,
                Longitud = coords.Value.lon,
                Carretera = record.carretera ?? "Desconocida",
                PuntoKm = record.pkIni,
                Sentido = record.sentido ?? "unknown",
                ComunidadAutonoma = record.cAutonomaIni ?? "Desconocida",
                Provincia = record.provinciaIni ?? "Desconocida",
                Municipio = record.municipioIni ?? "Desconocido",
                Causa = record.causa ?? "V16",
                SubCausa = record.subcausa ?? "",
                TipoVialidad = record.subtipoVialidad ?? "",
                FechaInicio = record.fechaInicio,
                Orientacion = GetOrientacion(record.sentido)
            };
        }
        
        private (double lat, double lon)? ParseGeometry(string geometria)
        {
            try
            {
                // Parse GeoJSON to extract first coordinate
                // Format: {"type":"LineString","coordinates":[[-6.2076,38.84254],...]}
                int coordStart = geometria.IndexOf("[[");
                if (coordStart < 0) coordStart = geometria.IndexOf("[");
                
                int coordEnd = geometria.IndexOf("]", coordStart + 2);
                if (coordStart < 0 || coordEnd < 0) return null;
                
                string coordPart = geometria.Substring(coordStart, coordEnd - coordStart + 1);
                coordPart = coordPart.Replace("[", "").Replace("]", "");
                
                string[] parts = coordPart.Split(',');
                if (parts.Length >= 2)
                {
                    double lon = double.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    double lat = double.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    return (lat, lon);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DGTApiService] Failed to parse geometry: {ex.Message}");
            }
            
            return null;
        }
        
        private string GetOrientacion(string sentido)
        {
            switch (sentido?.ToLower())
            {
                case "positive": return "Creciente";
                case "negative": return "Decreciente";
                case "both": return "Ambos sentidos";
                default: return "Desconocido";
            }
        }
    }
    
    [Serializable]
    public class ApiResponse
    {
        public SituationRecord[] situationsRecords;
    }
    
    [Serializable]
    public class SituationRecord
    {
        public string situationId;
        public string id;
        public string subtipoVialidad;
        public string fechaInicio;
        public string fechaFin;
        public string caracter;
        public string estado;
        public string causa;
        public string subcausa;
        public string carretera;
        public string sentido;
        public string orientacion;
        public string hacia;
        public float pkIni;
        public float pkFin;
        public string cAutonomaIni;
        public string provinciaIni;
        public string municipioIni;
        public string cAutonomaFin;
        public string provinciaFin;
        public string municipioFin;
        public string geometria;
    }
}
