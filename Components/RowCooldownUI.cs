using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using WeaponShipments.Apps;
using WeaponShipments.Services;

namespace WeaponShipments.Components
{
    [RegisterTypeInIl2Cpp]
    public class RowCooldownUI : MonoBehaviour
    {
        private Text _timerText;
        private string _shipmentId;

        public void Init(string shipmentId, Text timerText)
        {
            _shipmentId = shipmentId;
            _timerText = timerText;
        }

        private void Update()
        {
            if (WeaponShipmentApp.Instance == null)
                return;
            if (!WeaponShipmentApp.Instance.IsOpen())
                return;

            if (_timerText == null || string.IsNullOrEmpty(_shipmentId))
                return;

            TimeSpan remaining;
            bool onCooldown = ShipmentManager.Instance.TryGetCooldownRemaining(_shipmentId, out remaining);

            if (onCooldown && remaining.TotalSeconds > 0)
            {
                if (remaining.TotalSeconds < 0)
                    remaining = TimeSpan.Zero;

                _timerText.gameObject.SetActive(true);
                _timerText.text = remaining.ToString(@"mm\:ss");
            }
            else
            {
                _timerText.gameObject.SetActive(false);
            }
        }
    }
}