using System;
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
        private const float OpenDist = 4f;
        private const float CloseDist = 5f;

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

            float dist = Vector3.Distance(player.Position, _closedPos);
            float target = dist <= OpenDist ? 1f : dist >= CloseDist ? 0f : _openAmount;
            _openAmount = Mathf.MoveTowards(_openAmount, target, _smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(_closedPos, _openPos, _openAmount);
        }
    }
}
