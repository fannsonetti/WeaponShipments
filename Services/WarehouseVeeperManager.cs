using System.Collections;
using MelonLoader;
using S1API.Vehicles;
using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Services
{
    /// <summary>
    /// Manages the warehouse van (equipmentvan). Spawns when SetupComplete; used for sell jobs.
    /// </summary>
    public static class WarehouseVeeperManager
    {
        private static readonly Vector3 DefaultPosition = new Vector3(-26f, -4.3f, 173.5f);
        private static readonly Quaternion DefaultRotation = Quaternion.Euler(0f, 270f, 0f);

        public static void EnsureWarehouseVeeperExists()
        {
            var van = GameObject.Find("equipmentvan");
            if (van == null) van = GameObject.Find("equipmentcar"); // migration
            if (van != null)
            {
                var root = van.transform.root != null ? van.transform.root.gameObject : van;
                root.transform.position = DefaultPosition;
                root.transform.rotation = DefaultRotation;
                root.name = "equipmentvan";
                SetVehicleOwned(root, false);
                MelonLogger.Msg("[WarehouseVeeper] Moved equipmentvan to warehouse default.");
                return;
            }

            var v = VehicleRegistry.CreateVehicle("Veeper");
            if (v == null)
            {
                MelonLogger.Warning("[WarehouseVeeper] Failed to create Veeper.");
                return;
            }

            v.Color = VehicleColor.White;
            v.IsPlayerOwned = false;
            v.Spawn(DefaultPosition, DefaultRotation);

            var go = GetVehicleGameObject(v);
            if (go != null)
            {
                var root = go.transform.root != null ? go.transform.root.gameObject : go;
                root.name = "equipmentvan";
                MelonLogger.Msg("[WarehouseVeeper] Spawned white equipmentvan at {0}.", DefaultPosition);
            }
        }

        public static GameObject GetWarehouseVeeper()
        {
            var v = GameObject.Find("equipmentvan");
            if (v != null) return v;
            v = GameObject.Find("deliveryvan"); // during sell job
            if (v != null) return v;
            return GameObject.Find("equipmentcar"); // migration
        }

        /// <summary>Prepares the warehouse Veeper for a sell job. Does not teleport – van stays where it is; only sets player-owned so the player drives it to the dropoff.</summary>
        public static void PrepareForSellJob(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var go = GetWarehouseVeeper();
            if (go == null) return;

            var root = go.transform.root != null ? go.transform.root.gameObject : go;
            root.name = "deliveryvan";
            SetVehicleOwned(root, true);
        }

        public static void ReturnAfterSellJob()
        {
            MelonCoroutines.Start(ReturnAfterSellJobDelayed());
        }

        private static IEnumerator ReturnAfterSellJobDelayed()
        {
            const float delayMinutes = 5f;
            float elapsed = 0f;
            while (elapsed < delayMinutes * 60f)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            while (CameraOcclusionZone.IsPlayerInWarehouseZone())
            {
                MelonLogger.Msg("[WarehouseVeeper] Player in warehouse zone – waiting 10s before teleporting van.");
                yield return new WaitForSeconds(10f);
            }

            var go = GetWarehouseVeeper();
            if (go == null) yield break;

            var root = go.transform.root != null ? go.transform.root.gameObject : go;
            root.transform.position = DefaultPosition;
            root.transform.rotation = DefaultRotation;
            root.name = "equipmentvan";
            SetVehicleOwned(root, false);
            MelonLogger.Msg("[WarehouseVeeper] Van returned to warehouse after sell job.");
        }

        /// <summary>Set vehicle to unowned. Call when sell job completes so the player doesn't keep the van.</summary>
        public static void SetVehicleUnowned(GameObject vehicleRoot)
        {
            SetVehicleOwned(vehicleRoot, false);
        }

        private static void SetVehicleOwned(GameObject vehicleRoot, bool owned)
        {
            var v = VehicleRegistry.GetByName(vehicleRoot.name);
            if (v != null)
            {
                v.IsPlayerOwned = owned;
                return;
            }
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (obj != vehicleRoot && obj.transform.root != vehicleRoot.transform) continue;
                v = VehicleRegistry.GetByName(obj.name);
                if (v == null) continue;
                v.IsPlayerOwned = owned;
                return;
            }
        }

        private static GameObject GetVehicleGameObject(object vehicle)
        {
            if (vehicle is Component c)
                return c.gameObject;
            var prop = vehicle.GetType().GetProperty("GameObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return prop?.GetValue(vehicle) as GameObject;
        }
    }
}
