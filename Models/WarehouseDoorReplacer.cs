using System;
using System.Reflection;
using MelonLoader;
using S1API.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WeaponShipments
{
    /// <summary>When warehouse/garage owned, replace garage doors with simple visible slabs. Proximity-based smooth open/close.</summary>
    public static class WarehouseDoorReplacer
    {
        private const string WAREHOUSE_DOOR_PATH = "Hyland Point/Region_Northtown/Small warehouse/Bodyshop (1)/bodyshop/GarageDoor (1)";
        private const string GARAGE_DOOR_PATH = "Hyland Point/Region_Northtown/North apartments/North apartments/Foundation/GarageDoor (1)";
        private static bool _warehouseReplaced;
        private static bool _garageReplaced;

        public static void ResetReplaced()
        {
            _warehouseReplaced = false;
            _garageReplaced = false;
        }

        public static void TryReplaceWarehouseDoor()
        {
            if (_warehouseReplaced) return;
            var existing = GameObject.Find("warehousedoor");
            if (existing != null) { _warehouseReplaced = true; return; }

            var data = Data.WSSaveData.Instance?.Data;
            if (data == null || !data.Properties.Warehouse.Owned) return;

            var map = GameObject.Find("Map");
            if (map == null) return;
            var oldDoor = map.transform.Find(WAREHOUSE_DOOR_PATH);
            if (oldDoor == null) return;

            var warehouse = GameObject.Find("WeaponShipments_warehouse");
            if (warehouse == null)
            {
                MelonLogger.Warning("[WarehouseDoor] WeaponShipments_warehouse not found; ensure warehouse is loaded first.");
                return;
            }

            var newDoor = warehouse.transform.Find("warehouse door");
            if (newDoor == null)
            {
                foreach (var t in warehouse.GetComponentsInChildren<Transform>(true))
                    if (t.gameObject.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0)
                    { newDoor = t; break; }
            }
            if (newDoor == null)
            {
                MelonLogger.Warning("[WarehouseDoor] 'warehouse door' not found under WeaponShipments_warehouse.");
                return;
            }

            var pos = oldDoor.position;

            Object.Destroy(oldDoor.gameObject);

            newDoor.SetParent(warehouse.transform, false);
            newDoor.localPosition = warehouse.transform.InverseTransformPoint(pos);
            newDoor.localRotation = Quaternion.Euler(0f, 0f, 0f);
            newDoor.gameObject.name = "warehousedoor";
            newDoor.gameObject.SetActive(true);

            var anim = newDoor.gameObject.AddComponent<WarehouseDoorAnimator>();
            anim.Init(pos, 3.5f, 1f);

            _warehouseReplaced = true;
            MelonLogger.Msg("[WarehouseDoor] Replaced with WeaponShipments_warehouse/warehouse door.");
        }

        public static void TryReplaceGarageDoor()
        {
            if (_garageReplaced) return;
            var existing = GameObject.Find("garagedoor");
            if (existing != null) { _garageReplaced = true; return; }

            var data = Data.WSSaveData.Instance?.Data;
            if (data == null || !data.Properties.Garage.Owned) return;

            var map = GameObject.Find("Map");
            if (map == null) return;
            var door = map.transform.Find(GARAGE_DOOR_PATH);
            if (door == null) return;

            var doorGo = door.gameObject;
            var pos = doorGo.transform.position;
            var rot = doorGo.transform.rotation;
            var parent = doorGo.transform.parent;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "garagedoor";
            cube.transform.SetParent(parent, false);
            cube.transform.position = pos;
            cube.transform.rotation = rot;
            cube.transform.localScale = new Vector3(0.1f, 3.5f, 4f);

            var bc = cube.GetComponent<BoxCollider>();
            if (bc != null) bc.isTrigger = false;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.35f, 1f));
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.2f, 0.25f));
                }
                cube.GetComponent<MeshRenderer>().material = mat;
            }

            var anim = cube.AddComponent<WarehouseDoorAnimator>();
            anim.Init(pos, 3.5f, 1f);

            Object.Destroy(doorGo);
            _garageReplaced = true;
            MelonLogger.Msg("[WarehouseDoor] Replaced North apartments garage door with garagedoor.");
        }
    }

    public class WarehouseDoorAnimator : MonoBehaviour
    {
        private Vector3 _closedPos;
        private Vector3 _openPos;
        private float _openAmount;
        private const float TriggerDist = 4f;

        private float _smoothSpeed = 1f;

        public void Init(Vector3 closedPos, float openDistance, float smoothSpeed)
        {
            _closedPos = closedPos;
            _openPos = closedPos + Vector3.up * openDistance;
            _openAmount = 0f;
            _smoothSpeed = smoothSpeed;
        }

        private void Update()
        {
            var player = Player.Local;
            if (player == null) return;

            Vector3 checkPos = GetCachedPlayerOrVehiclePosition(player);
            float dist = Vector3.Distance(checkPos, _closedPos);
            float target = dist <= TriggerDist ? 1f : 0f;
            _openAmount = Mathf.MoveTowards(_openAmount, target, _smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(_closedPos, _openPos, _openAmount);
        }

        private const float CacheInterval = 0.5f;
        private const float SkipVehicleScanDistSq = 400f; // 20m - when player far from doors, use position only
        private static float _lastCacheTime;
        private static Vector3 _cachedPosition;
        private static bool _cacheValid;

        /// <summary>Shared cache for player/vehicle position. Used by door animators and CameraOcclusionZone. Throttled to avoid costly FindObjectsOfType.</summary>
        public static Vector3 GetCachedPlayerOrVehiclePosition(Player player)
        {
            if (player == null) return Vector3.zero;
            var playerPos = player.Position;
            // When far from doors, skip expensive vehicle scan
            float distSqToWarehouse = (playerPos - new Vector3(-23f, 0f, 170f)).sqrMagnitude;
            if (distSqToWarehouse > SkipVehicleScanDistSq)
            {
                float distSqToGarage = (playerPos - new Vector3(-18f, 0f, 185f)).sqrMagnitude;
                if (distSqToGarage > SkipVehicleScanDistSq)
                    return playerPos;
            }
            float now = Time.time;
            if (!_cacheValid || now - _lastCacheTime >= CacheInterval)
            {
                var vehiclePos = TryGetDrivenVehiclePosition(player);
                _cachedPosition = vehiclePos ?? playerPos;
                _cacheValid = true;
                _lastCacheTime = now;
            }
            return _cachedPosition;
        }

        private static System.Type _cachedLandVehicleType;
        private static PropertyInfo _cachedDriverPlayerProp;

        private static Vector3? TryGetDrivenVehiclePosition(Player player)
        {
            try
            {
                if (_cachedLandVehicleType == null)
                {
                    var asm = Assembly.Load("Assembly-CSharp");
                    _cachedLandVehicleType = asm?.GetType("ScheduleOne.Vehicles.LandVehicle") ?? asm?.GetType("LandVehicle");
                    if (_cachedLandVehicleType != null)
                        _cachedDriverPlayerProp = _cachedLandVehicleType.GetProperty("DriverPlayer", BindingFlags.Public | BindingFlags.Instance)
                            ?? _cachedLandVehicleType.GetProperty("Driver", BindingFlags.Public | BindingFlags.Instance);
                }
                if (_cachedLandVehicleType == null || _cachedDriverPlayerProp == null) return null;

                var vehicles = Object.FindObjectsOfType(_cachedLandVehicleType);
                foreach (var v in vehicles)
                {
                    if (v == null) continue;
                    var driver = _cachedDriverPlayerProp.GetValue(v);
                    if (driver != null && driver.Equals(player))
                    {
                        var tr = (v as Component)?.transform;
                        if (tr != null) return tr.position;
                        var posProp = v.GetType().GetProperty("Position", BindingFlags.Public | BindingFlags.Instance);
                        if (posProp?.PropertyType == typeof(Vector3))
                            return (Vector3)posProp.GetValue(v);
                        return null;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
