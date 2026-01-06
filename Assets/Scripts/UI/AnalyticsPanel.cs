using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using V16App.API;

namespace V16App.UI
{
    /// <summary>
    /// Analytics panel showing statistics about beacons
    /// </summary>
    public class AnalyticsPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;
        
        [Header("Summary Stats")]
        [SerializeField] private Text totalBeaconsText;
        [SerializeField] private Text activeBeaconsText;
        [SerializeField] private Text lastUpdateText;
        
        [Header("By Community")]
        [SerializeField] private Transform communityListContainer;
        [SerializeField] private GameObject communityItemPrefab;
        
        [Header("By Province")]
        [SerializeField] private Transform provinceListContainer;
        [SerializeField] private GameObject provinceItemPrefab;
        
        [Header("Most Affected Roads")]
        [SerializeField] private Transform roadsListContainer;
        [SerializeField] private GameObject roadItemPrefab;
        
        [Header("Colors")]
        [SerializeField] private Gradient countGradient;
        
        private List<BeaconData> _beacons = new List<BeaconData>();
        
        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
            
            if (openButton != null)
                openButton.onClick.AddListener(Show);
            
            // Initialize gradient if not set
            if (countGradient == null)
            {
                countGradient = new Gradient();
                countGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.2f, 0.7f, 0.2f), 0f),
                        new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.5f),
                        new GradientColorKey(new Color(1f, 0.2f, 0.2f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
            }
            
            Hide();
        }
        
        public void UpdateData(List<BeaconData> beacons)
        {
            _beacons = beacons ?? new List<BeaconData>();
            RefreshUI();
        }
        
        public void Show()
        {
            RefreshUI();
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }
        
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
        
        private void RefreshUI()
        {
            UpdateSummary();
            UpdateCommunityList();
            UpdateProvinceList();
            UpdateRoadsList();
        }
        
        private void UpdateSummary()
        {
            int total = _beacons.Count;
            int active = _beacons.Count(b => b.IsActive);
            
            SetText(totalBeaconsText, total.ToString());
            SetText(activeBeaconsText, active.ToString());
            SetText(lastUpdateText, System.DateTime.Now.ToString("HH:mm:ss"));
        }
        
        private void UpdateCommunityList()
        {
            if (communityListContainer == null) return;
            
            ClearContainer(communityListContainer);
            
            var grouped = _beacons
                .GroupBy(b => b.ComunidadAutonoma ?? "Desconocida")
                .OrderByDescending(g => g.Count())
                .Take(10);
            
            int maxCount = grouped.Any() ? grouped.Max(g => g.Count()) : 1;
            
            foreach (var group in grouped)
            {
                CreateStatItem(communityListContainer, communityItemPrefab, 
                    group.Key, group.Count(), maxCount);
            }
        }
        
        private void UpdateProvinceList()
        {
            if (provinceListContainer == null) return;
            
            ClearContainer(provinceListContainer);
            
            var grouped = _beacons
                .GroupBy(b => b.Provincia ?? "Desconocida")
                .OrderByDescending(g => g.Count())
                .Take(10);
            
            int maxCount = grouped.Any() ? grouped.Max(g => g.Count()) : 1;
            
            foreach (var group in grouped)
            {
                CreateStatItem(provinceListContainer, provinceItemPrefab,
                    group.Key, group.Count(), maxCount);
            }
        }
        
        private void UpdateRoadsList()
        {
            if (roadsListContainer == null) return;
            
            ClearContainer(roadsListContainer);
            
            var grouped = _beacons
                .Where(b => !string.IsNullOrEmpty(b.Carretera))
                .GroupBy(b => b.Carretera)
                .OrderByDescending(g => g.Count())
                .Take(10);
            
            int maxCount = grouped.Any() ? grouped.Max(g => g.Count()) : 1;
            
            foreach (var group in grouped)
            {
                CreateStatItem(roadsListContainer, roadItemPrefab,
                    group.Key, group.Count(), maxCount);
            }
        }
        
        private void CreateStatItem(Transform container, GameObject prefab, string label, int count, int maxCount)
        {
            GameObject item;
            
            if (prefab != null)
            {
                item = Instantiate(prefab, container);
            }
            else
            {
                item = CreateDefaultStatItem(container);
            }
            
            // Find and set label text
            Text labelText = item.transform.Find("Label")?.GetComponent<Text>();
            if (labelText == null) labelText = item.GetComponentInChildren<Text>();
            if (labelText != null) labelText.text = label;
            
            // Find and set count text
            Text countText = item.transform.Find("Count")?.GetComponent<Text>();
            if (countText != null) countText.text = count.ToString();
            
            // Find and set bar fill
            Image barFill = item.transform.Find("Bar/Fill")?.GetComponent<Image>();
            if (barFill != null)
            {
                float ratio = (float)count / maxCount;
                barFill.fillAmount = ratio;
                barFill.color = countGradient.Evaluate(ratio);
            }
        }
        
        private GameObject CreateDefaultStatItem(Transform container)
        {
            GameObject item = new GameObject("StatItem");
            item.transform.SetParent(container, false);
            
            RectTransform rt = item.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 30);
            
            HorizontalLayoutGroup layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(5, 5, 2, 2);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(item.transform, false);
            Text label = labelObj.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.color = Color.white;
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 150;
            
            // Bar container
            GameObject barObj = new GameObject("Bar");
            barObj.transform.SetParent(item.transform, false);
            RectTransform barRt = barObj.AddComponent<RectTransform>();
            Image barBg = barObj.AddComponent<Image>();
            barBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            LayoutElement barLE = barObj.AddComponent<LayoutElement>();
            barLE.preferredWidth = 100;
            barLE.preferredHeight = 16;
            
            // Bar fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            RectTransform fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fill = fillObj.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            
            // Count
            GameObject countObj = new GameObject("Count");
            countObj.transform.SetParent(item.transform, false);
            Text count = countObj.AddComponent<Text>();
            count.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            count.fontSize = 14;
            count.fontStyle = FontStyle.Bold;
            count.color = Color.white;
            count.alignment = TextAnchor.MiddleRight;
            LayoutElement countLE = countObj.AddComponent<LayoutElement>();
            countLE.preferredWidth = 40;
            
            return item;
        }
        
        private void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }
        
        private void SetText(Text textComponent, string value)
        {
            if (textComponent != null)
                textComponent.text = value ?? "-";
        }
        
        /// <summary>
        /// Get summary statistics as formatted text
        /// </summary>
        public string GetSummaryText()
        {
            var byCommunity = _beacons
                .GroupBy(b => b.ComunidadAutonoma ?? "Desconocida")
                .OrderByDescending(g => g.Count());
            
            string summary = $"📊 Resumen de Balizas V16\n\n";
            summary += $"Total: {_beacons.Count}\n";
            summary += $"Activas: {_beacons.Count(b => b.IsActive)}\n\n";
            summary += "Por Comunidad Autónoma:\n";
            
            foreach (var group in byCommunity.Take(5))
            {
                summary += $"  • {group.Key}: {group.Count()}\n";
            }
            
            return summary;
        }
    }
}
