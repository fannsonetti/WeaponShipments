using MelonLoader;
using S1API.TVApp;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace WeaponShipments.UI
{
    /// <summary>
    /// TV version of Disruption Logistics. Shows on in-game TVs.
    /// Placeholder: use phone for full control. Full UI parity can be added later.
    /// </summary>
    public class WeaponShipmentTVApp : TVApp
    {
        protected override string AppName => "WeaponShipmentTVApp";
        protected override string AppTitle => "Disruption Logistics";
        protected override Sprite Icon => WeaponShipmentApp.LoadAppIcon();

        protected override void OnCreatedUI(GameObject container)
        {
            if (WeaponShipmentApp.Instance != null)
                WeaponShipmentApp.Instance.BuildUIIntoTarget(container.transform);
            else
                BuildPlaceholderUI(container.transform);
        }

        private static void BuildPlaceholderUI(Transform parent)
        {
            var bg = UIFactory.Panel("TVPlaceholder", parent, new Color(0.08f, 0.08f, 0.08f), fullAnchor: true);
            var text = UIFactory.Text("TVPlaceholderText", "Disruption Logistics\n\nUse your phone for full access.", bg.transform, 24);
            var rt = text.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.1f, 0.3f);
                rt.anchorMax = new Vector2(0.9f, 0.7f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }
}
