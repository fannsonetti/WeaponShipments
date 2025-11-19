using MelonLoader;
using UnityEngine;
using WeaponShipments.Services;
using Object = UnityEngine.Object;

namespace WeaponShipments.Components
{
    [RegisterTypeInIl2Cpp]
    public class DeliveryAreaTrigger : MonoBehaviour
    {
        private string _shipmentId;

        public void Init(string shipmentId)
        {
            _shipmentId = shipmentId;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject == null)
                return;

            // get top-level crate object
            Transform root = other.transform.root;
            if (root == null)
                root = other.transform;

            // we only care about WeaponShipment crates
            if (root.name != "WeaponShipment" && other.gameObject.name != "WeaponShipment")
                return;

            var shipment = ShipmentManager.Instance.GetShipment(_shipmentId);
            if (shipment == null || shipment.Delivered)
                return;

            ShipmentManager.Instance.DeliverShipment(_shipmentId);

            // destroy the whole crate, not just the child collider
            Object.Destroy(root.gameObject);
            // remove area + cube
            Object.Destroy(this.gameObject);

            MelonLogger.Msg("[DeliveryArea] Shipment {0} delivered and crate/area removed.", _shipmentId);

            var app = WeaponShipments.Apps.WeaponShipmentApp.Instance;
            if (app != null)
            {
                app.OnExternalShipmentChanged(_shipmentId);
            }
        }
    }    
}
