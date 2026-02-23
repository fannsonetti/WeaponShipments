using System.Collections;
using System.Linq;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WeaponShipments
{
    /// <summary>
    /// Sets up warehouse equipment: visibility based on production, laundering table clone, pallet props.
    /// </summary>
    public static class WarehouseEquipmentSetup
    {
        private static bool _launderingSetup;
        private static bool _palletSetup;

        public static void ResetSetup()
        {
            _launderingSetup = false;
            _palletSetup = false;
        }

        /// <summary>Call when SetupComplete becomes true to show equipment mid-session.</summary>
        public static void RefreshEquipmentVisibility()
        {
            var warehouse = GameObject.Find("WeaponShipments_warehouse");
            if (warehouse == null) return;
            var equipment = warehouse.transform.Find("equipment");
            if (equipment == null) return;
            SetEquipmentVisibility(equipment, true);
        }

        public static void SetupEquipment()
        {
            var warehouse = GameObject.Find("WeaponShipments_warehouse");
            if (warehouse == null) return;

            var data = Data.WSSaveData.Instance?.Data;
            if (data == null || !data.Properties.Warehouse.Owned) return;

            var equipment = warehouse.transform.Find("equipment");
            if (equipment == null) return;

            bool productionReady = data.Properties.Warehouse.SetupComplete;

            SetupLaunderingTable(equipment);
            SetupPalletProps(equipment);

            SetEquipmentVisibility(equipment, productionReady);
        }

        private static void SetEquipmentVisibility(Transform equipment, bool visible)
        {
            equipment.gameObject.SetActive(visible);
            foreach (Transform child in equipment)
                child.gameObject.SetActive(visible);
        }

        private static void SetupLaunderingTable(Transform equipmentParent)
        {
            if (_launderingSetup) return;

            var propContents = GameObject.Find("Property Contents");
            if (propContents == null) return;

            Transform laundering = null;
            foreach (var t in propContents.GetComponentsInChildren<Transform>(true))
                if (t.name.Contains("LaunderingStation") && t.name.Contains("Built"))
                { laundering = t; break; }
            if (laundering == null) return;

            laundering.gameObject.SetActive(true);
            var clone = Object.Instantiate(laundering.gameObject);
            clone.SetActive(true);

            Transform keep1 = null, keep2 = null;
            foreach (var t in clone.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("plastictable_2x1", System.StringComparison.OrdinalIgnoreCase)) keep1 = t;
                if (t.name.Equals("oldcomputer", System.StringComparison.OrdinalIgnoreCase)) keep2 = t;
            }
            if (keep1 != null) keep1.SetParent(clone.transform, true);
            if (keep2 != null) keep2.SetParent(clone.transform, true);
            foreach (Transform child in clone.transform.Cast<Transform>().ToArray())
                if (child != keep1 && child != keep2)
                    Object.Destroy(child.gameObject);

            RemoveLaunderingStationComponent(clone);

            clone.transform.position = new Vector3(-22.9864f, -4.6226f, 170.6525f);
            clone.transform.SetParent(equipmentParent, true);
            clone.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            bool productionReady = Data.WSSaveData.Instance?.Data?.Properties.Warehouse.SetupComplete ?? false;
            clone.SetActive(productionReady);
            _launderingSetup = true;
            MelonLogger.Msg("[WarehouseEquipment] Laundering table clone added to equipment.");
        }

        private static void SetupPalletProps(Transform equipmentParent)
        {
            if (_palletSetup) return;

            var map = GameObject.Find("Map");
            if (map == null) return;

            var palletPath = "Hyland Point/Region_Northtown/Waterfront/Pallet Prop (1)";
            var pallet = map.transform.Find(palletPath);
            if (pallet == null) return;

            var pos1 = new Vector3(-26.6109f, -4.9986f, 170.5643f);
            var rot1 = Quaternion.Euler(0f, 20f, 0f);
            var pos2 = new Vector3(-28.8105f, -3.8031f, 170.6557f);
            var rot2 = Quaternion.Euler(0f, 98f, 0f);

            var clone1 = Object.Instantiate(pallet.gameObject);
            clone1.transform.position = pos1;
            clone1.transform.rotation = rot1;
            RemoveRigidbodies(clone1);
            clone1.transform.SetParent(equipmentParent, true);

            var clone2 = Object.Instantiate(pallet.gameObject);
            clone2.transform.position = pos2;
            clone2.transform.rotation = rot2;
            RemoveRigidbodies(clone2);
            clone2.transform.SetParent(equipmentParent, true);

            bool productionReady = Data.WSSaveData.Instance?.Data?.Properties.Warehouse.SetupComplete ?? false;
            clone1.SetActive(productionReady);
            clone2.SetActive(productionReady);

            _palletSetup = true;
            MelonLogger.Msg("[WarehouseEquipment] Pallet props added to equipment.");
        }

        private static void RemoveLaunderingStationComponent(GameObject go)
        {
            var asm = System.Reflection.Assembly.Load("Assembly-CSharp");
            var type = asm?.GetType("ScheduleOne.ObjectScripts.LaunderingStation");
            if (type != null)
            {
                foreach (var c in go.GetComponentsInChildren(type, true))
                    Object.Destroy(c);
            }
        }

        private static void RemoveRigidbodies(GameObject root)
        {
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
                Object.Destroy(rb);
        }
    }
}
