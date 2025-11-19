using MelonLoader;
using UnityEngine;
using WeaponShipments.Apps;
using WeaponShipments.Services;

namespace WeaponShipments.Components
{
    [RegisterTypeInIl2Cpp]
    public class AutoRefreshUI : MonoBehaviour
    {
        private float _timer = 0f;

        private void Update()
        {
            var app = WeaponShipmentApp.Instance;
            if (app == null)
                return;
            if (!app.IsOpen())
                return;

            _timer += Time.deltaTime;
            if (_timer < 1f)
                return;

            _timer = 0f;

            app.RefreshList();

            if (!string.IsNullOrEmpty(app._selectedShipmentId))
            {
                var s = ShipmentManager.Instance.GetShipment(app._selectedShipmentId);
                if (s != null)
                {
                    app._statusValue.text = app.GetStatusDisplayText(s);
                    app.UpdateStatusColor(s.Status);
                    app.UpdateActionButtonForStatus(s.Status);
                }
            }
        }
    }
}