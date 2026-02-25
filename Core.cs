using MelonLoader;
using S1API.Entities;
using S1API.Quests;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using WeaponShipments.Quests;
using WeaponShipments.Services;
using WeaponShipments.Utils;

[assembly: MelonInfo(typeof(WeaponShipments.Core), Constants.MOD_NAME, Constants.MOD_VERSION, Constants.MOD_AUTHOR)]
[assembly: MelonGame(Constants.Game.GAME_STUDIO, Constants.Game.GAME_NAME)]

namespace WeaponShipments
{
    public class Core : MelonMod
    {
        private static bool _bunkerRequested;
        private static bool _act0Hooked;

        public override void OnInitializeMelon()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            MelonLogger.Msg("WeaponShipments initialized!");
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == "Main")
            {
                _act0Hooked = false;
                CameraOcclusionZone.OnPlayerEnteredZone -= OnWarehouseZoneEntered;
                WarehouseLoader.ResetLoaded();
                GarageLoader.ResetLoaded();
                TerrainLoader.ResetLoaded();
                WarehouseDoorReplacer.ResetReplaced();
                WarehouseEquipmentSetup.ResetSetup();
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main")
                return;

            var debugGo = new GameObject("WeaponShipments_DebugMenu");
            UnityEngine.Object.DontDestroyOnLoad(debugGo);
            debugGo.AddComponent<WSDebugMenu>();

            WarehouseLoader.LoadWarehouseAdditiveOnce();
            GarageLoader.LoadGarageAdditiveOnce();
            TerrainLoader.LoadTerrainAdditiveOnce();
            MelonCoroutines.Start(DeferredWarehouseDoorReplace());
            MelonCoroutines.Start(SpawnTeleportLocationsWhenReady());
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Main")
                return;

            if (_act0Hooked)
                return;

            _act0Hooked = true;

            MelonLogger.Msg("[WeaponShipments] Main scene loaded. Deferring quest init until S1API is ready...");
            MelonCoroutines.Start(InitQuestsWhenReady());
            MelonCoroutines.Start(StartCameraOcclusionZoneWhenReady());
        }

        private static IEnumerator DeferredWarehouseDoorReplace()
        {
            yield return new WaitForSeconds(1.5f);
            WarehouseDoorReplacer.TryReplaceWarehouseDoor();
            WarehouseDoorReplacer.TryReplaceGarageDoor();
            WarehouseEquipmentSetup.SetupEquipment();
            DisableWarehouseProps();
            EnsureWarehouseVeeperWhenReady();
        }

        private static void EnsureWarehouseVeeperWhenReady()
        {
            var data = Data.WSSaveData.Instance?.Data;
            if (data == null || !data.Properties.Warehouse.SetupComplete) return;
            Services.WarehouseVeeperManager.EnsureWarehouseVeeperExists();
        }

        private static void DisableWarehouseProps()
        {
            var map = GameObject.Find("Map");
            if (map == null) return;
            var basePath = "Hyland Point/Region_Northtown/Small warehouse";
            foreach (var name in new[] { "Forklift", "Dumpster" })
            {
                var t = map.transform.Find($"{basePath}/{name}");
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                    MelonLogger.Msg("[WeaponShipments] Disabled {0}.", name);
                }
            }
        }

        private static IEnumerator InitQuestsWhenReady()
        {
            float timeout = 30f;
            while (Player.Local == null && timeout > 0f)
            {
                timeout -= UnityEngine.Time.deltaTime;
                yield return null;
            }

            if (Player.Local == null)
            {
                MelonLogger.Warning("[WeaponShipments] Player.Local never became available; cannot init quests.");
                yield break;
            }

            yield return null;

            MelonLogger.Msg("[WeaponShipments] S1API ready. Initializing quests and dependencies.");

            WeaponShipments.Quests.QuestManager.Initialize();
            Act1NewNumberQuest.StartWhenReady();
        }

        private static IEnumerator StartCameraOcclusionZoneWhenReady()
        {
            float timeout = 30f;
            while (Player.Local == null && timeout > 0f)
            {
                timeout -= UnityEngine.Time.deltaTime;
                yield return null;
            }

            if (Player.Local != null)
            {
                CameraOcclusionZone.StartMonitoring();
                CameraOcclusionZone.OnPlayerEnteredZone += OnWarehouseZoneEntered;
            }
        }

        private static void OnWarehouseZoneEntered()
        {
            var data = Data.WSSaveData.Instance?.Data;
            var p = Data.WSPersistent.Instance?.Data;
            if (data == null || p == null) return;
            if (!data.Properties.Warehouse.SetupComplete) return;

            if (!p.WarehouseZoneEnteredAfterDelivery)
            {
                p.WarehouseZoneEnteredAfterDelivery = true;
                WeaponShipments.Quests.QuestManager.TryStartUnpackingIfEligible();
            }
        }

        public override void OnApplicationQuit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private static IEnumerator SpawnTeleportLocationsWhenReady()
        {
            yield return new WaitForSeconds(2f);
            var map = GameObject.Find("Map");
            if (map == null) yield break;
            var teleportRoot = map.transform.Find("Teleport Locations");
            if (teleportRoot == null)
            {
                var go = new GameObject("Teleport Locations");
                go.transform.SetParent(map.transform, false);
                teleportRoot = go.transform;
            }
            var nw = new GameObject("NorthWarehouse");
            nw.transform.SetParent(teleportRoot, false);
            nw.transform.position = new Vector3(-19f, -4f, 173.5f);
            nw.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
            var gar = new GameObject("Garage");
            gar.transform.SetParent(teleportRoot, false);
            gar.transform.position = new Vector3(-67f, -3.9f, 150f);
            gar.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            MelonLogger.Msg("[WeaponShipments] Created teleport locations NorthWarehouse and Garage.");
        }
    }
}
