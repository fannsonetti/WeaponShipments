using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace WeaponShipments
{
    public static class WarehouseLoader
    {
        private const string WAREHOUSE_RESOURCE_NAME = "WeaponShipments.warehouse";
        private static bool _loaded;

        public static void LoadWarehouseAdditiveOnce()
        {
            if (_loaded)
                return;

            _loaded = true;

            // --------------------------------------------------
            // Load embedded AssetBundle bytes
            // --------------------------------------------------
            var asm = Assembly.GetExecutingAssembly();
            byte[] bytes;

            using (var stream = asm.GetManifestResourceStream(WAREHOUSE_RESOURCE_NAME))
            {
                if (stream == null)
                {
                    MelonLogger.Error($"[warehouse] Embedded resource not found: {WAREHOUSE_RESOURCE_NAME}");
                    MelonLogger.Msg("[warehouse] Available resources:");
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
                MelonLogger.Error("[warehouse] AssetBundle.LoadFromMemory failed.");
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
                    MelonLogger.Error("[warehouse] Bundle contains no prefabs.");
                    return;
                }

                var prefab = prefabs[0];
                var inst = Object.Instantiate(prefab);
                inst.name = $"WeaponShipments_{prefab.name}";

                // --------------------------------------------------
                // Move into active scene
                // --------------------------------------------------
                var activeScene = SceneManager.GetActiveScene();
                SceneManager.MoveGameObjectToScene(inst, activeScene);
                inst.SetActive(true);

                // --------------------------------------------------
                // Transform (your values)
                // --------------------------------------------------
                inst.transform.position = new Vector3(-27.85f, -2.515f, 173.558f);
                inst.transform.rotation = Quaternion.Euler(270f, 270f, 0f);
                inst.transform.localScale = new Vector3(430f, 575f, 250f);

                // --------------------------------------------------
                // Fix shaders/materials for runtime
                // --------------------------------------------------
                ForceRuntimeShaderAndRebind(inst);

                MelonLogger.Msg(
                    $"[warehouse] Spawned in scene '{activeScene.name}' at {inst.transform.position}"
                );
            }
            finally
            {
                bundle.Unload(false);
            }
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
                MelonLogger.Error("[warehouse] No runtime shader available.");
                return;
            }

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var oldMat = mats[i];
                    if (oldMat == null)
                        continue;

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
                            if (newMat.HasProperty("_BaseMap"))
                                newMat.SetTexture("_BaseMap", baseTex);
                            if (newMat.HasProperty("_MainTex"))
                                newMat.SetTexture("_MainTex", baseTex);
                        }

                        if (newMat.HasProperty("_BaseColor"))
                            newMat.SetColor("_BaseColor", baseColor);
                        if (newMat.HasProperty("_Color"))
                            newMat.color = baseColor;

                        if (newMat.HasProperty("_Surface"))
                            newMat.SetFloat("_Surface", 0f);
                        if (newMat.HasProperty("_ZWrite"))
                            newMat.SetFloat("_ZWrite", 1f);

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
