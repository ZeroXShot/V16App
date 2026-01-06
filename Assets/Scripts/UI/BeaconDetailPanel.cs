using UnityEngine;
using UnityEngine.UI;
using V16App.API;

namespace V16App.UI
{
    /// <summary>
    /// Panel that displays detailed information about a selected beacon
    /// </summary>
    public class BeaconDetailPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        
        [Header("Header")]
        [SerializeField] private Text titleText;
        [SerializeField] private Image statusIndicator;
        [SerializeField] private Text statusText;
        
        [Header("Location Info")]
        [SerializeField] private Text latitudeText;
        [SerializeField] private Text longitudeText;
        [SerializeField] private Text roadText;
        [SerializeField] private Text kmText;
        [SerializeField] private Text directionText;
        [SerializeField] private Text orientationText;
        
        [Header("Administrative Info")]
        [SerializeField] private Text communityText;
        [SerializeField] private Text provinceText;
        [SerializeField] private Text municipalityText;
        
        [Header("Incident Info")]
        [SerializeField] private Text causeText;
        [SerializeField] private Text typeText;
        [SerializeField] private Text timeText;
        
        [Header("Actions")]
        [SerializeField] private Button googleMapsButton;
        [SerializeField] private Button wazeButton;
        [SerializeField] private Button shareButton;
        
        [Header("Colors")]
        [SerializeField] private Color activeStatusColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color inactiveStatusColor = new Color(0.8f, 0.2f, 0.2f);
        
        private BeaconData _currentBeacon;
        
        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
                
            if (googleMapsButton != null)
                googleMapsButton.onClick.AddListener(OpenInGoogleMaps);
                
            if (wazeButton != null)
                wazeButton.onClick.AddListener(OpenInWaze);
                
            if (shareButton != null)
                shareButton.onClick.AddListener(ShareBeacon);
            
            Hide();
        }
        
        public void Show(BeaconData beacon)
        {
            _currentBeacon = beacon;
            
            if (beacon == null)
            {
                Hide();
                return;
            }
            
            UpdateUI();
            
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }
        
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
        
        private void UpdateUI()
        {
            if (_currentBeacon == null) return;
            
            // Header
            SetText(titleText, _currentBeacon.GetDisplayName());
            
            if (statusIndicator != null)
                statusIndicator.color = _currentBeacon.IsActive ? activeStatusColor : inactiveStatusColor;
            
            SetText(statusText, _currentBeacon.IsActive ? "Activa" : "Inactiva");
            
            // Location
            SetText(latitudeText, $"{_currentBeacon.Latitud:F6}");
            SetText(longitudeText, $"{_currentBeacon.Longitud:F6}");
            SetText(roadText, _currentBeacon.Carretera);
            SetText(kmText, $"{_currentBeacon.PuntoKm:F2}");
            SetText(directionText, _currentBeacon.Sentido);
            SetText(orientationText, _currentBeacon.Orientacion);
            
            // Administrative
            SetText(communityText, _currentBeacon.ComunidadAutonoma);
            SetText(provinceText, _currentBeacon.Provincia);
            SetText(municipalityText, _currentBeacon.Municipio);
            
            // Incident
            SetText(causeText, _currentBeacon.Causa);
            SetText(typeText, _currentBeacon.TipoVialidad);
            SetText(timeText, _currentBeacon.GetTimeSinceActivation());
        }
        
        private void SetText(Text textComponent, string value)
        {
            if (textComponent != null)
                textComponent.text = value ?? "-";
        }
        
        private void OpenInGoogleMaps()
        {
            if (_currentBeacon != null)
            {
                Application.OpenURL(_currentBeacon.GetGoogleMapsUrl());
            }
        }
        
        private void OpenInWaze()
        {
            if (_currentBeacon != null)
            {
                Application.OpenURL(_currentBeacon.GetWazeUrl());
            }
        }
        
        private void ShareBeacon()
        {
            if (_currentBeacon == null) return;
            
            string shareText = $"🚨 {_currentBeacon.GetDisplayName()}\n" +
                              $"📍 {_currentBeacon.GetLocationString()}\n" +
                              $"🛣️ {_currentBeacon.Carretera} km {_currentBeacon.PuntoKm:F2}\n" +
                              $"🗺️ {_currentBeacon.GetGoogleMapsUrl()}";
            
            // Copy to clipboard
            GUIUtility.systemCopyBuffer = shareText;
            Debug.Log("Beacon info copied to clipboard");
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            ShareOnAndroid(shareText);
            #endif
        }
        
        #if UNITY_ANDROID
        private void ShareOnAndroid(string text)
        {
            try
            {
                AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
                AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
                
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                
                AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                
                AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Compartir baliza");
                currentActivity.Call("startActivity", chooser);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error sharing: {e.Message}");
            }
        }
        #endif
    }
}
