using MelonLoader;
using UnityEngine;

namespace WeaponShipments
{
    /// <summary>
    /// Sets up warehouse equipment: visibility based on SetupComplete.
    /// Drill, machine, tool are loaded from assetbundles (not here).
    /// </summary>
    public static class WarehouseEquipmentSetup
    {
        public static void ResetSetup()
        {
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

            SetEquipmentVisibility(equipment, productionReady);
        }

        private static void SetEquipmentVisibility(Transform equipment, bool visible)
        {
            equipment.gameObject.SetActive(visible);
            foreach (Transform child in equipment)
                child.gameObject.SetActive(visible);
        }
    }
}
