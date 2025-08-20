using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace WeaponShipments
{
    public static class AssetBundleLoader
    {
        private const string BUNKER_RESOURCE_NAME = "WeaponShipments.bunker";
        private static bool _loaded;

        public static void LoadBunkerAdditiveOnce()
        {
            if (_loaded)
                return;

            _loaded = true;

            // --------------------------------------------------
            // Load embedded AssetBundle bytes
            // --------------------------------------------------
            var asm = Assembly.GetExecutingAssembly();
            byte[] bytes;

            using (var stream = asm.GetManifestResourceStream(BUNKER_RESOURCE_NAME))
            {
                if (stream == null)
                {
                    MelonLogger.Error($"[Bunker] Embedded resource not found: {BUNKER_RESOURCE_NAME}");
                    MelonLogger.Msg("[Bunker] Available resources:");
                    foreach (var n in asm.GetManifestResourceNames())
                        MelonLogger.Msg(" - " + n);
                    return;
                }

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                bytes = ms.ToArray();
            }

            var bundle = AssetBundle.LoadFromMemory(bytes);
            if (bundle == null)
            {
                MelonLogger.Error("[Bunker] AssetBundle.LoadFromMemory failed.");
                return;
            }

            try
            {
                // --------------------------------------------------
                // Load prefab
                // --------------------------------------------------
                var prefabs = bundle.LoadAllAssets<GameObject>();
                if (prefabs == null || prefabs.Length == 0)
                {
                    MelonLogger.Error("[Bunker] Bundle contains no prefabs.");
                    return;
                }

                var prefab = prefabs[0];
                var inst = Object.Instantiate(prefab);
                inst.name = $"BUNKER_INSTANCE_{prefab.name}";

                // --------------------------------------------------
                // Move into active (main) scene
                // --------------------------------------------------
                var activeScene = SceneManager.GetActiveScene();
                SceneManager.MoveGameObjectToScene(inst, activeScene);
                inst.SetActive(true);

                // --------------------------------------------------
                // FIXED TRANSFORM (your values)
                // --------------------------------------------------
                inst.transform.position = new Vector3(132.8766f, 0.2689f, -23.1931f);
                inst.transform.rotation = Quaternion.Euler(0f, 65f, 0f);
                inst.transform.localScale = new Vector3(2.1f, 2.1f, 2.1f);

                // --------------------------------------------------
                // Fix shaders/materials for runtime
                // --------------------------------------------------
                ForceRuntimeShaderAndRebind(inst);

                // --------------------------------------------------
                // Attach roof + trigger behaviors
                // --------------------------------------------------
                AttachRoofAndTrigger(inst);

                MelonLogger.Msg($"[Bunker] Spawned in scene '{activeScene.name}' at {inst.transform.position}");
            }
            finally
            {
                bundle.Unload(false);
            }
        }

        private const string BUNKER_PROPERTY_RESOURCE = "WeaponShipments.bunkerproperty";
        private static bool _propertyLoaded;

        public static void LoadBunkerPropertyOnce()
        {
            if (_propertyLoaded)
                return;

            _propertyLoaded = true;

            var asm = Assembly.GetExecutingAssembly();
            byte[] bytes;

            using (var stream = asm.GetManifestResourceStream(BUNKER_PROPERTY_RESOURCE))
            {
                if (stream == null)
                {
                    MelonLogger.Error($"[BunkerProperty] Embedded resource not found: {BUNKER_PROPERTY_RESOURCE}");
                    return;
                }

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                bytes = ms.ToArray();
            }

            var bundle = AssetBundle.LoadFromMemory(bytes);
            if (bundle == null)
            {
                MelonLogger.Error("[BunkerProperty] Failed to load AssetBundle.");
                return;
            }

            try
            {
                var prefabs = bundle.LoadAllAssets<GameObject>();
                if (prefabs.Length == 0)
                {
                    MelonLogger.Error("[BunkerProperty] Bundle contains no prefabs.");
                    return;
                }

                var prefab = prefabs[0];
                var inst = Object.Instantiate(prefab);
                inst.name = $"BUNKER_PROPERTY_{prefab.name}";

                // Move into active scene
                var scene = SceneManager.GetActiveScene();
                SceneManager.MoveGameObjectToScene(inst, scene);
                inst.SetActive(true);

                // Position (NO rotation or scale unless you want it)
                inst.transform.position = new Vector3(
                    280.2509f,
                    20.4344f,
                   -268.052f
                );

                // Fix shaders so it renders
                ForceRuntimeShaderAndRebind(inst);

                MelonLogger.Msg($"[BunkerProperty] Spawned at {inst.transform.position}");
            }
            finally
            {
                bundle.Unload(false);
            }
        }

        // ==================================================
        // ROOF + TRIGGER ATTACHMENT
        // ==================================================
        private static void AttachRoofAndTrigger(GameObject bunkerRoot)
        {
            var roof = FindChildRecursive(bunkerRoot.transform, "Roof");
            if (roof == null)
            {
                MelonLogger.Error("[Bunker] Roof object not found.");
                return;
            }

            // ---------------------------
            // Roof open/close controller
            // ---------------------------
            var roofCtrl = roof.gameObject.AddComponent<BunkerRoofController>();
            roofCtrl.closedEuler = new Vector3(270f, 0f, 0f);
            roofCtrl.openEuler = new Vector3(290f, 180f, 180f);
            roofCtrl.openDistance = 15f;
            roofCtrl.smoothSpeed = 0.3f;
            roofCtrl.vehicleName = "playerpusher";

            MelonLogger.Msg("[Bunker] Roof controller attached.");

            // ---------------------------
            // Teleport trigger
            // ---------------------------
            var trigger = FindChildRecursive(roof, "Trigger");
            if (trigger == null)
            {
                MelonLogger.Error("[Bunker] trigger object not found under Roof.");
                return;
            }

            var col = trigger.GetComponent<Collider>();
            if (col == null)
            {
                MelonLogger.Error("[Bunker] trigger object has NO collider.");
                return;
            }

            col.isTrigger = true;

            // IMPORTANT: MeshCollider triggers must be convex
            if (col is MeshCollider mc)
            {
                mc.convex = true;
            }

            // IMPORTANT: trigger needs a Rigidbody
            var rb = trigger.GetComponent<Rigidbody>();
            if (rb == null)
                rb = trigger.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;

            // Attach teleporter
            var tp = trigger.gameObject.AddComponent<BunkerTeleportTrigger>();
            tp.teleportTo = new Vector3(200f, 200f, 200f);
            tp.vehicleName = "PlayerPusher";

            MelonLogger.Msg("[Bunker] Teleport trigger attached and armed.");
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        // ==================================================
        // SHADER / MATERIAL FIX
        // ==================================================
        private static void ForceRuntimeShaderAndRebind(GameObject root)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            Shader unlitTex = Shader.Find("Unlit/Texture");
            Shader unlitCol = Shader.Find("Unlit/Color");
            Shader standard = Shader.Find("Standard");

            Shader fallback = urpLit ?? urpUnlit ?? unlitTex ?? unlitCol ?? standard;
            if (fallback == null)
            {
                MelonLogger.Error("[Bunker] No runtime shader available.");
                return;
            }

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var oldMat = mats[i];
                    if (oldMat == null) continue;

                    Texture baseTex =
                        (oldMat.HasProperty("_BaseMap") ? oldMat.GetTexture("_BaseMap") : null) ??
                        (oldMat.HasProperty("_MainTex") ? oldMat.GetTexture("_MainTex") : null);

                    Color baseColor =
                        oldMat.HasProperty("_BaseColor") ? oldMat.GetColor("_BaseColor") :
                        oldMat.HasProperty("_Color") ? oldMat.color :
                        Color.white;

                    bool badShader =
                        oldMat.shader == null ||
                        oldMat.shader.name == "Hidden/InternalErrorShader" ||
                        oldMat.shader.name == "Standard";

                    if (badShader)
                    {
                        var newMat = new Material(fallback);

                        if (baseTex != null)
                        {
                            if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", baseTex);
                            if (newMat.HasProperty("_MainTex")) newMat.SetTexture("_MainTex", baseTex);
                        }

                        if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", baseColor);
                        if (newMat.HasProperty("_Color")) newMat.color = baseColor;

                        if (newMat.HasProperty("_Surface")) newMat.SetFloat("_Surface", 0f);
                        if (newMat.HasProperty("_ZWrite")) newMat.SetFloat("_ZWrite", 1f);

                        newMat.renderQueue = 2000;
                        mats[i] = newMat;
                    }
                }

                r.sharedMaterials = mats;
                r.enabled = true;
            }
        }
    }
}
