using UnityEngine;
using MelonLoader;
using Object = UnityEngine.Object;

namespace WeaponShipments.Services
{
    public static class WeaponShipmentSpawner
    {
        private static GameObject _templateCrate;

        // Where the shipment crate appears
        private static readonly Vector3 TargetPosition = new Vector3(
            21.0741f,
            0.9143f,
            -80.6995f
        );

        public static void SpawnShipmentCrate(ShipmentManager.ShipmentEntry shipment)
        {
            if (shipment == null)
            {
                MelonLogger.Warning("[WeaponShipmentSpawner] Tried to spawn crate for null shipment.");
                return;
            }

            if (_templateCrate == null)
            {
                _templateCrate = GameObject.Find("Wood Crate Prop");
                if (_templateCrate == null)
                {
                    MelonLogger.Error("[WeaponShipmentSpawner] Could not find 'Wood Crate Prop' in the scene.");
                    return;
                }
            }

            var clone = Object.Instantiate(_templateCrate);
            clone.name = "WeaponShipment";
            clone.transform.position = TargetPosition;

            // Rename child "Cube" to WeaponShipment so the collider has the right name
            RenameChildCubeToWeaponShipment(clone.transform);

            // 🔹 Scale based on quantity
            ApplyQuantityScale(clone.transform, shipment.Quantity);

            MelonLogger.Msg("[WeaponShipmentSpawner] Spawned WeaponShipment crate (qty {0}) at {1}", shipment.Quantity, TargetPosition);
        }

        private static void RenameChildCubeToWeaponShipment(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Cube")
                {
                    t.name = "WeaponShipment";
                }
            }
        }

        private static void ApplyQuantityScale(Transform root, int quantity)
        {
            float scale;

            switch (quantity)
            {
                case 1:
                    scale = 0.6f;
                    break;
                case 2:
                    scale = 0.9f;
                    break;
                case 3:
                default:
                    scale = 1.2f;
                    break;
            }

            // You can either scale the whole crate...
            // root.localScale = new Vector3(scale, scale, scale);

            // ...or prefer to scale just the visual child.
            // This assumes the visual mesh is under the root somewhere.
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "WeaponShipment")
                {
                    t.localScale = new Vector3(scale, scale, scale);
                }
            }
        }
    }
}
