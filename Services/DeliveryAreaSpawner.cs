using UnityEngine;
using MelonLoader;
using WeaponShipments.Apps;
using Object = UnityEngine.Object;

namespace WeaponShipments.Services
{
    public static class DeliveryAreaSpawner
    {
        private static readonly Vector3 DeliveryPosition = new Vector3(-17.2277f, -2.6895f, 173.3186f);
        private static readonly Vector3 DeliverySize = new Vector3(3f, 3f, 3f);

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

            var go = new GameObject(areaName);
            go.transform.position = DeliveryPosition;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = DeliverySize;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var trigger = go.AddComponent<DeliveryAreaTrigger>();
            trigger.Init(shipmentId);

            // Spawn visible cube
            CreateLimeGreenCube(go.transform);

            MelonLogger.Msg("[DeliveryAreaSpawner] Spawned delivery area for shipment {0}", shipmentId);
        }

        // ---------------------------
        //      Lime-Green Cube
        // ---------------------------
        private static void CreateLimeGreenCube(Transform parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "DeliveryAreaVisual_LimeCube";
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = DeliverySize;

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

            // Lime green with 5% alpha
            Color baseColor = new Color(0.5f, 1f, 0f, 0.01f);
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
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            mat.renderQueue = 3000; // transparent queue

            renderer.material = mat;

            MelonLogger.Msg("[DeliveryAreaSpawner] URP/Lit cube set to transparent. Color: {0}", baseColor);
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

                // 🔥 destroy the whole crate, not just the child collider
                Object.Destroy(root.gameObject);
                // remove area + cube
                Object.Destroy(this.gameObject);

                MelonLogger.Msg("[DeliveryArea] Shipment {0} delivered and crate/area removed.", _shipmentId);

                // if you have the WeaponShipmentApp.Instance refresh call, leave it here:
                // var app = WeaponShipmentApp.Instance;
                // if (app != null) app.OnExternalShipmentChanged(_shipmentId);
            }
        }
    }
}
