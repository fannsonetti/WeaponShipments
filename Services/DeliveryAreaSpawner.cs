using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using WeaponShipments.Apps;
using Object = UnityEngine.Object;

namespace WeaponShipments.Services
{
    public static class DeliveryAreaSpawner
    {
        // Fallback position + size + rotation if a destination is unknown
        private static readonly Vector3 FallbackDeliveryPosition = new Vector3(-17.2277f, -2.6895f, 173.3186f);
        private static readonly Vector3 DefaultDeliverySize = new Vector3(3f, 3f, 3f);
        private static readonly Quaternion DefaultRotation = Quaternion.identity;

        // Data for a delivery zone
        private struct DeliveryZone
        {
            public Vector3 Position;
            public Vector3 Size;
            public Quaternion Rotation;

            public DeliveryZone(Vector3 position, Vector3 size, Quaternion rotation)
            {
                Position = position;
                Size = size;
                Rotation = rotation;
            }
        }

        // Map shipment.Destination -> world position + area size + rotation
        // TODO: Replace these Vector3s / rotations with your actual scene coordinates/sizes/orientations
        private static readonly Dictionary<string, DeliveryZone> DestinationZones =
            new Dictionary<string, DeliveryZone>
            {
                {
                    "Handy Hank's Hardware",
                    new DeliveryZone(
                        new Vector3(106.498f, 1.805f, 31.7038f), // position
                        new Vector3(1.5f, 1.5f, 3.8f), // size
                        Quaternion.Euler(0f, 0f, 0f) // rotation
                    )
                },
                {
                    "Dan's Hardware",
                    new DeliveryZone(
                        new Vector3(-16.9721f, -2.2155f, 135.1911f),
                        new Vector3(1.5f, 1.5f, 3.5f),
                        Quaternion.Euler(0f, 90f, 0f)
                    )
                },
                {
                    "North Warehouse",
                        new DeliveryZone(
                        new Vector3(-21f, -3.1f, 173.55f),
                        new Vector3(3f, 3.6f, 3f),
                        Quaternion.Euler(0f, 0f, 0f)
                    )
                },  
                {
                    "West Gas-mart",
                    new DeliveryZone(
                        new Vector3(-107.85f, -2.2265f, 66.17f),
                        new Vector3(1.5f, 1.5f, 3.5f),
                        Quaternion.Euler(0f, 90f, 0f)
                    )
                },
                {
                    "Central Gas-mart",
                    new DeliveryZone(
                        new Vector3(20.95f, 1.7485f, 0.165f),
                        new Vector3(1.5f, 1.5f, 3.5f),
                        Quaternion.Euler(0f, 340f, 0f)
                    )
                },
            };

        private static DeliveryZone GetZoneForDestination(string destination)
        {
            if (!string.IsNullOrEmpty(destination) &&
                DestinationZones.TryGetValue(destination, out var zone))
            {
                return zone;
            }

            MelonLogger.Warning(
                "[DeliveryAreaSpawner] No zone mapped for destination '{0}', using fallback.",
                string.IsNullOrEmpty(destination) ? "<null/empty>" : destination
            );

            return new DeliveryZone(FallbackDeliveryPosition, DefaultDeliverySize, DefaultRotation);
        }

        public static void SpawnDeliveryArea(string shipmentId)
        {
            if (string.IsNullOrEmpty(shipmentId))
            {
                MelonLogger.Warning("[DeliveryAreaSpawner] No shipmentId provided.");
                return;
            }

            string areaName = $"ShipmentDeliveryArea_{shipmentId}";
            if (GameObject.Find(areaName) != null)
                return;

            // Look up the shipment so we know its destination
            var shipment = ShipmentManager.Instance.GetShipment(shipmentId);
            if (shipment == null)
            {
                MelonLogger.Warning("[DeliveryAreaSpawner] SpawnDeliveryArea: shipment not found for id {0}", shipmentId);
                return;
            }

            // Get the zone (position + size + rotation) for that destination
            DeliveryZone zone = GetZoneForDestination(shipment.Destination);

            var go = new GameObject(areaName);
            go.transform.position = zone.Position;
            go.transform.rotation = zone.Rotation;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = zone.Size;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var trigger = go.AddComponent<DeliveryAreaTrigger>();
            trigger.Init(shipmentId);

            // Spawn visible cube with matching size (inherits parent's rotation)
            CreateLimeGreenCube(go.transform, zone.Size);

            MelonLogger.Msg(
                "[DeliveryAreaSpawner] Spawned delivery area for shipment {0} at {1} (dest: {2}, size: {3}, rot: {4})",
                shipmentId,
                zone.Position,
                shipment.Destination,
                zone.Size,
                zone.Rotation.eulerAngles
            );
        }

        // ---------------------------
        //      Lime-Green Cube
        // ---------------------------
        private static void CreateLimeGreenCube(Transform parent, Vector3 size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "DeliveryAreaVisual_LimeCube";
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localRotation = Quaternion.identity; // local identity, inherits parent's rotation
            cube.transform.localScale = size;

            // Visual only – trigger collider is on the parent
            Object.Destroy(cube.GetComponent<Collider>());

            var renderer = cube.GetComponent<MeshRenderer>();

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                MelonLogger.Error("[DeliveryAreaSpawner] Shader 'Universal Render Pipeline/Lit' not found.");
                return;
            }

            var mat = new Material(shader);

            // --- URP Lit: switch to Transparent / Alpha mode ---
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1.0f);       // 0 = Opaque, 1 = Transparent

            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0.0f);         // 0 = Alpha, 1 = Premultiply, etc.

            // Optional: don’t bother receiving shadows for a marker cube
            if (mat.HasProperty("_ReceiveShadows"))
                mat.SetFloat("_ReceiveShadows", 0.0f);

            // Lime green with very low alpha
            Color baseColor = new Color(0.5f, 1f, 0f, 0.15f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", baseColor);

            // Make it self-lit so it’s never dark
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");

                // Same lime-green but a bit brighter
                mat.SetColor("_EmissionColor", new Color(0.5f, 1f, 0f) * 1.5f);
            }

            // Blending + depth settings for transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABlEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            mat.renderQueue = 3000; // transparent queue

            renderer.material = mat;

            MelonLogger.Msg(
                "[DeliveryAreaSpawner] URP/Lit cube set to transparent. Color: {0}, Size: {1}",
                baseColor,
                size
            );
        }


        // ---------------------------
        //    DELIVERY TRIGGER LOGIC
        // ---------------------------
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
}
