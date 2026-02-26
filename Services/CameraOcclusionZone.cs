using System.Collections;
using MelonLoader;
using S1API.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WeaponShipments.Services
{
    /// <summary>
    /// Toggles Camera.useOcclusionCulling when the player is inside the specified box.
    /// Also toggles warehouse/regions for FPS. Counts player-driven vehicles as "player in zone".
    /// Zone: x -34 to -13, y -5 to 5, z 166 to 178.
    /// </summary>
    public static class CameraOcclusionZone
    {
        private static readonly Vector3 BoxMin = new Vector3(-34f, -5f, 166f);
        private static readonly Vector3 BoxMax = new Vector3(-13f, 5f, 178f);

        private static readonly string[] RegionsToDisableInside = {
            "Hyland Point/Region_Suburbia",
            "Hyland Point/Region_Docks",
            "Hyland Point/Region_Westville"
        };

        public static void StartMonitoring()
        {
            MelonCoroutines.Start(MonitorLoop());
        }

        private static bool IsPointInBox(Vector3 p)
        {
            return p.x >= BoxMin.x && p.x <= BoxMax.x
                && p.y >= BoxMin.y && p.y <= BoxMax.y
                && p.z >= BoxMin.z && p.z <= BoxMax.z;
        }

        /// <summary>Uses shared throttled cache from WarehouseDoorReplacer - no FindObjectsOfType here.</summary>
        private static bool IsPlayerOrDrivenVehicleInZone()
        {
            var player = Player.Local;
            if (player == null) return false;
            var pos = WeaponShipments.WarehouseDoorAnimator.GetCachedPlayerOrVehiclePosition(player);
            return IsPointInBox(pos);
        }

        private static void SetZoneVisibility(bool inside)
        {
            var map = GameObject.Find("Map");
            if (map == null) return;

            foreach (var path in RegionsToDisableInside)
            {
                var t = map.transform.Find(path);
                if (t != null)
                    t.gameObject.SetActive(!inside);
            }
        }

        private static bool TryFindCamera(out Camera cam)
        {
            cam = null;
            var cameras = Object.FindObjectsOfType<Camera>();
            foreach (var c in cameras)
            {
                if (c == null) continue;
                var t = c.transform;
                if (t.parent == null || t.parent.name != "CameraContainer") continue;
                var container = t.parent;
                if (container.parent == null || container.parent.name.IndexOf("Player", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                cam = c;
                return true;
            }
            return false;
        }

        private const int HysteresisCount = 2;
        private static int _insideCount;
        private static int _outsideCount;

        private static IEnumerator MonitorLoop()
        {
            Camera cam = null;
            bool? lastInside = null;

            while (true)
            {
                if (cam == null)
                {
                    TryFindCamera(out cam);
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                bool reading = IsPlayerOrDrivenVehicleInZone();

                if (reading)
                {
                    _insideCount++;
                    _outsideCount = 0;
                }
                else
                {
                    _outsideCount++;
                    _insideCount = 0;
                }

                bool inside = lastInside ?? false;
                if (reading && _insideCount >= HysteresisCount) inside = true;
                else if (!reading && _outsideCount >= HysteresisCount) inside = false;

                if (lastInside.HasValue && lastInside.Value == inside)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                lastInside = inside;
                cam.useOcclusionCulling = !inside;
                SetZoneVisibility(inside);
                if (inside)
                    OnPlayerEnteredZone?.Invoke();
                MelonLogger.Msg($"[CameraOcclusionZone] Player {(inside ? "entered" : "left")} zone. useOcclusionCulling = {!inside}.");
                yield return new WaitForSeconds(1f);
            }
        }

        public static event System.Action OnPlayerEnteredZone;

        /// <summary>Returns true if the player (or their driven vehicle) is inside the warehouse culling zone.</summary>
        public static bool IsPlayerInWarehouseZone() => IsPlayerOrDrivenVehicleInZone();
    }
}
