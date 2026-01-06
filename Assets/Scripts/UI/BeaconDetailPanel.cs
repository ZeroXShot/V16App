using UnityEngine;
using UnityEngine.UI;
using V16App.API;

namespace V16App.UI
{
    /// <summary>
    /// Panel that displays detailed information about a selected beacon
    /// Creates UI programmatically for a complete, functional panel
    /// </summary>
    public class BeaconDetailPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        
        [Header("Colors")]
        [SerializeField] private Color panelBgColor = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        [SerializeField] private Color headerColor = new Color(0.1f, 0.14f, 0.2f, 1f);
        [SerializeField] private Color activeStatusColor = new Color(0.2f, 0.9f, 0.3f, 1f);
        [SerializeField] private Color inactiveStatusColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color accentColor = new Color(0f, 0.82f, 1f, 1f);
        [SerializeField] private Color textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color labelColor = new Color(0.6f, 0.65f, 0.75f, 1f);
        
        private BeaconData _currentBeacon;
        private bool _uiBuilt = false;
        
        // Dynamic UI references
        private Text _titleText;
        private Image _statusIndicator;
        private Text _statusText;
        private Text _latitudeText;
        private Text _longitudeText;
        private Text _roadText;
        private Text _kmText;
        private Text _directionText;
        private Text _communityText;
        private Text _provinceText;
        private Text _municipalityText;
        private Text _causeText;
        private Text _timeText;
        private Button _closeButton;
        private Button _googleMapsButton;
        private Button _wazeButton;
        private Button _shareButton;
        private ScrollRect _scrollRect;
        
        private void Start()
        {
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
            
            if (!_uiBuilt)
            {
                BuildUI();
                _uiBuilt = true;
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
        
        private void BuildUI()
        {
            if (panelRoot == null) return;
            
            // Clear existing children
            foreach (Transform child in panelRoot.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Setup panel background
            RectTransform panelRt = panelRoot.GetComponent<RectTransform>();
            Image panelImg = panelRoot.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.color = panelBgColor;
            }
            
            // Create scroll view
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelRoot.transform, false);
            RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            
            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            
            // Viewport with mask
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRt = viewportObj.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportObj.AddComponent<RectMask2D>();
            
            // Content container
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            
            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 12;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            
            _scrollRect.content = contentRt;
            _scrollRect.viewport = viewportRt;
            
            // Build sections
            BuildHeader(contentObj.transform);
            BuildLocationSection(contentObj.transform);
            BuildAdministrativeSection(contentObj.transform);
            BuildIncidentSection(contentObj.transform);
            BuildActionButtons(contentObj.transform);
            
            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }
        
        private void BuildHeader(Transform parent)
        {
            GameObject headerObj = CreateSection(parent, "Header");
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = headerColor;
            
            VerticalLayoutGroup hlg = headerObj.AddComponent<VerticalLayoutGroup>();
            hlg.padding = new RectOffset(12, 40, 12, 12);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            
            LayoutElement le = headerObj.AddComponent<LayoutElement>();
            le.minHeight = 80;
            
            // Title row
            GameObject titleRow = new GameObject("TitleRow");
            titleRow.transform.SetParent(headerObj.transform, false);
            HorizontalLayoutGroup titleHlg = titleRow.AddComponent<HorizontalLayoutGroup>();
            titleHlg.spacing = 10;
            titleHlg.childAlignment = TextAnchor.MiddleLeft;
            titleHlg.childControlHeight = false;
            titleHlg.childForceExpandWidth = false;
            
            // Status indicator
            GameObject statusInd = new GameObject("StatusIndicator");
            statusInd.transform.SetParent(titleRow.transform, false);
            _statusIndicator = statusInd.AddComponent<Image>();
            _statusIndicator.color = activeStatusColor;
            LayoutElement statusLE = statusInd.AddComponent<LayoutElement>();
            statusLE.minWidth = 12;
            statusLE.minHeight = 12;
            statusLE.preferredWidth = 12;
            statusLE.preferredHeight = 12;
            
            // Title
            _titleText = CreateText(titleRow.transform, "Title", "🚨 Baliza V16", 20, FontStyle.Bold);
            _titleText.color = textColor;
            
            // Status text
            _statusText = CreateText(headerObj.transform, "StatusText", "Estado: Activa", 14, FontStyle.Normal);
            _statusText.color = activeStatusColor;
            
            // Close button
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(headerObj.transform.parent, false);
            RectTransform closeBtnRt = closeBtn.AddComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(1, 1);
            closeBtnRt.anchorMax = new Vector2(1, 1);
            closeBtnRt.pivot = new Vector2(1, 1);
            closeBtnRt.anchoredPosition = new Vector2(-8, -8);
            closeBtnRt.sizeDelta = new Vector2(32, 32);
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            
            _closeButton = closeBtn.AddComponent<Button>();
            _closeButton.targetGraphic = closeBg;
            _closeButton.onClick.AddListener(Hide);
            
            Text closeX = CreateText(closeBtn.transform, "X", "✕", 18, FontStyle.Bold);
            closeX.alignment = TextAnchor.MiddleCenter;
            RectTransform closeXRt = closeX.GetComponent<RectTransform>();
            closeXRt.anchorMin = Vector2.zero;
            closeXRt.anchorMax = Vector2.one;
            closeXRt.offsetMin = Vector2.zero;
            closeXRt.offsetMax = Vector2.zero;
        }
        
        private void BuildLocationSection(Transform parent)
        {
            GameObject section = CreateSection(parent, "Location", "📍 Ubicación");
            
            _roadText = CreateLabelValue(section.transform, "Carretera", "-");
            _kmText = CreateLabelValue(section.transform, "Punto Km", "-");
            _directionText = CreateLabelValue(section.transform, "Sentido", "-");
            _latitudeText = CreateLabelValue(section.transform, "Latitud", "-");
            _longitudeText = CreateLabelValue(section.transform, "Longitud", "-");
        }
        
        private void BuildAdministrativeSection(Transform parent)
        {
            GameObject section = CreateSection(parent, "Administrative", "🏛️ Localización");
            
            _communityText = CreateLabelValue(section.transform, "Comunidad", "-");
            _provinceText = CreateLabelValue(section.transform, "Provincia", "-");
            _municipalityText = CreateLabelValue(section.transform, "Municipio", "-");
        }
        
        private void BuildIncidentSection(Transform parent)
        {
            GameObject section = CreateSection(parent, "Incident", "⚠️ Incidencia");
            
            _causeText = CreateLabelValue(section.transform, "Causa", "-");
            _timeText = CreateLabelValue(section.transform, "Tiempo activa", "-");
        }
        
        private void BuildActionButtons(Transform parent)
        {
            GameObject section = CreateSection(parent, "Actions");
            
            HorizontalLayoutGroup hlg = section.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            
            LayoutElement le = section.AddComponent<LayoutElement>();
            le.minHeight = 50;
            
            _googleMapsButton = CreateActionButton(section.transform, "🗺️", "Maps", new Color(0.2f, 0.6f, 0.9f, 1f));
            _googleMapsButton.onClick.AddListener(OpenInGoogleMaps);
            
            _wazeButton = CreateActionButton(section.transform, "🚗", "Waze", new Color(0.2f, 0.8f, 0.9f, 1f));
            _wazeButton.onClick.AddListener(OpenInWaze);
            
            _shareButton = CreateActionButton(section.transform, "📤", "Compartir", new Color(0.5f, 0.4f, 0.9f, 1f));
            _shareButton.onClick.AddListener(ShareBeacon);
        }
        
        private GameObject CreateSection(Transform parent, string name, string title = null)
        {
            GameObject section = new GameObject(name);
            section.transform.SetParent(parent, false);
            
            RectTransform rt = section.AddComponent<RectTransform>();
            
            if (!string.IsNullOrEmpty(title))
            {
                VerticalLayoutGroup vlg = section.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 8;
                vlg.padding = new RectOffset(0, 0, 0, 8);
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                
                Text titleText = CreateText(section.transform, "Title", title, 16, FontStyle.Bold);
                titleText.color = accentColor;
            }
            
            return section;
        }
        
        private Text CreateLabelValue(Transform parent, string label, string value)
        {
            GameObject row = new GameObject(label + "Row");
            row.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            
            Text labelText = CreateText(row.transform, "Label", label + ":", 13, FontStyle.Normal);
            labelText.color = labelColor;
            LayoutElement labelLE = labelText.gameObject.AddComponent<LayoutElement>();
            labelLE.minWidth = 90;
            labelLE.preferredWidth = 90;
            
            Text valueText = CreateText(row.transform, "Value", value, 13, FontStyle.Normal);
            valueText.color = textColor;
            LayoutElement valueLE = valueText.gameObject.AddComponent<LayoutElement>();
            valueLE.flexibleWidth = 1;
            
            return valueText;
        }
        
        private Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rt = obj.AddComponent<RectTransform>();
            
            Text text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = textColor;
            
            ContentSizeFitter csf = obj.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            return text;
        }
        
        private Button CreateActionButton(Transform parent, string icon, string label, Color color)
        {
            GameObject btnObj = new GameObject(label + "Button");
            btnObj.transform.SetParent(parent, false);
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = 44;
            le.flexibleWidth = 1;
            
            VerticalLayoutGroup vlg = btnObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.padding = new RectOffset(8, 8, 6, 6);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            
            Text iconText = CreateText(btnObj.transform, "Icon", icon, 18, FontStyle.Normal);
            iconText.alignment = TextAnchor.MiddleCenter;
            
            Text labelText = CreateText(btnObj.transform, "Label", label, 11, FontStyle.Normal);
            labelText.alignment = TextAnchor.MiddleCenter;
            
            return btn;
        }
        
        private void UpdateUI()
        {
            if (_currentBeacon == null) return;
            
            // Header
            SetText(_titleText, $"🚨 {_currentBeacon.GetDisplayName()}");
            
            if (_statusIndicator != null)
                _statusIndicator.color = _currentBeacon.IsActive ? activeStatusColor : inactiveStatusColor;
            
            SetText(_statusText, _currentBeacon.IsActive ? "● Estado: Activa" : "○ Estado: Inactiva");
            if (_statusText != null)
                _statusText.color = _currentBeacon.IsActive ? activeStatusColor : inactiveStatusColor;
            
            // Location
            SetText(_roadText, _currentBeacon.Carretera);
            SetText(_kmText, $"{_currentBeacon.PuntoKm:F2}");
            SetText(_directionText, _currentBeacon.Orientacion);
            SetText(_latitudeText, $"{_currentBeacon.Latitud:F6}");
            SetText(_longitudeText, $"{_currentBeacon.Longitud:F6}");
            
            // Administrative
            SetText(_communityText, _currentBeacon.ComunidadAutonoma);
            SetText(_provinceText, _currentBeacon.Provincia);
            SetText(_municipalityText, _currentBeacon.Municipio);
            
            // Incident
            SetText(_causeText, _currentBeacon.Causa);
            SetText(_timeText, _currentBeacon.GetTimeSinceActivation());
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
