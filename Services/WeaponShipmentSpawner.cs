using UnityEngine;
using MelonLoader;
using Object = UnityEngine.Object;

namespace WeaponShipments.Services
{
    public static class WeaponShipmentSpawner
    {
        // Wherever you want the shipment to spawn
        private static readonly Vector3 SpawnPosition = new Vector3(21.0741f, 0.9143f, -80.6995f);

        private static GameObject _templateCrate;

        public static GameObject SpawnShipmentCrate()
        {
            if (_templateCrate == null)
            {
                _templateCrate = GameObject.Find("Wood Crate Prop");
                if (_templateCrate == null)
                {
                    MelonLogger.Error("[WeaponShipmentSpawner] Could not find 'Wood Crate Prop' in the scene.");
                    return null;
                }
            }

            var clone = Object.Instantiate(_templateCrate);
            clone.name = "WeaponShipment";
            clone.transform.position = SpawnPosition;

            // Rename child "Cube" to WeaponShipment as well
            RenameChildCubeToWeaponShipment(clone.transform);

            MelonLogger.Msg("[WeaponShipmentSpawner] Spawned WeaponShipment crate at {0}", SpawnPosition);
            return clone;
        }

        private static void RenameChildCubeToWeaponShipment(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Cube")
                {
                    t.name = "WeaponShipment";
                    MelonLogger.Msg("[WeaponShipmentSpawner] Renamed child Cube to WeaponShipment (path: {0})", t.GetHierarchyPath());
                }
            }
        }

        // Small helper to print nice hierarchy paths in logs
        private static string GetHierarchyPath(this Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
