using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using Object = UnityEngine.Object;

namespace WeaponShipments.Services
{
    public static class WeaponShipmentSpawner
    {
        private static GameObject _templateCrate;

        // Old fixed spot now used as a fallback
        private static readonly Vector3 FallbackSpawnPosition = new Vector3(
            0f,
            0f,
            0f
        );

        private static readonly Quaternion FallbackSpawnRotation = Quaternion.identity;

        // Data for a spawn point
        private struct SpawnPoint
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public SpawnPoint(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }

        // Map shipment.Origin -> crate spawn position + rotation
        // TODO: Replace these with your actual scene coordinates/orientations.
        private static readonly Dictionary<string, SpawnPoint> OriginSpawnPoints =
            new Dictionary<string, SpawnPoint>
            {
                {
                    "RV",
                    new SpawnPoint(
                        new Vector3(17f, 1.5f, -79f),
                        Quaternion.Euler(0f, 180f, 0f)
                    )
                },
                {
                    "Gazebo",
                    new SpawnPoint(
                        new Vector3(83f, 6f, -125f),
                        Quaternion.Euler(0f, 90f, 0f)
                    )
                },
                {
                    "Sewer Market",
                    new SpawnPoint(
                        new Vector3(72.75f, -4.5f, 34.65f),
                        Quaternion.Euler(0f, 65f, 0f)
                    )
                },
                {
                    "Black Market",
                    new SpawnPoint(
                        new Vector3(-61.1777f, -1.54f, 32.429f),
                        Quaternion.Euler(0f, 0f, 0f)
                    )
                },
            };

        private static SpawnPoint GetSpawnPointForOrigin(string origin)
        {
            if (!string.IsNullOrEmpty(origin) &&
                OriginSpawnPoints.TryGetValue(origin, out var spawn))
            {
                return spawn;
            }

            MelonLogger.Warning(
                "[WeaponShipmentSpawner] No spawn mapped for origin '{0}', using fallback.",
                string.IsNullOrEmpty(origin) ? "<null/empty>" : origin
            );

            return new SpawnPoint(FallbackSpawnPosition, FallbackSpawnRotation);
        }

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

            // Decide spawn point based on shipment origin
            var spawn = GetSpawnPointForOrigin(shipment.Origin);
            clone.transform.position = spawn.Position;
            clone.transform.rotation = spawn.Rotation;

            // Rename child "Cube" to WeaponShipment so the collider has the right name
            RenameChildCubeToWeaponShipment(clone.transform);

            // Scale based on quantity (1–3)
            ApplyQuantityScale(clone.transform, shipment.Quantity);

            MelonLogger.Msg(
                "[WeaponShipmentSpawner] Spawned WeaponShipment crate (qty {0}) at {1} (origin: {2}, rot: {3})",
                shipment.Quantity,
                spawn.Position,
                shipment.Origin,
                spawn.Rotation.eulerAngles
            );
        }

        private static void RenameChildCubeToWeaponShipment(Transform root)
        {
            if (root == null) 
                return;

            // Check root itself
            if (root.name == "Cube")
                root.name = "WeaponShipment";

            // Recursively traverse children
            for (int i = 0; i < root.childCount; i++)
            {
                RenameChildCubeToWeaponShipment(root.GetChild(i));
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

            // Apply uniform scaling to the crate root
            root.localScale = new Vector3(scale, scale, scale);
        }
    }
}
