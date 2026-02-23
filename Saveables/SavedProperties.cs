using System;
using System.Collections.Generic;

namespace WeaponShipments.Saveables
{
    [Serializable]
    public class SavedProperties
    {
        public WarehouseProperty Warehouse = new WarehouseProperty();
        public GarageProperty Garage = new GarageProperty();
        public BunkerProperty Bunker = new BunkerProperty();
    }

    // ===================== WAREHOUSE =====================

    [Serializable]
    public class WarehouseProperty
    {
        public bool Owned = false;
        public bool Compromised = false;

        /// <summary>Set when Quest 2 completes; gates supplies→stock conversion.</summary>
        public bool SetupComplete = false;

        public List<WarehouseShipment> PendingShipments = new List<WarehouseShipment>();
    }

    [Serializable]
    public class WarehouseShipment
    {
        public float ArrivesAt;
        public float SuppliesAmount;
    }

    // ===================== GARAGE =====================

    [Serializable]
    public class GarageProperty
    {
        public bool Owned = false;
        public bool Compromised = false;

        public List<GarageShipment> PendingShipments = new List<GarageShipment>();

        public GarageUpgrades Upgrades = new GarageUpgrades();
    }

    [Serializable]
    public class GarageShipment
    {
        public float ArrivesAt;
        public float SuppliesAmount;
    }

    [Serializable]
    public class GarageUpgrades
    {
        public bool EquipmentOwned = false;
        public bool StaffOwned = false;
        public bool SecurityOwned = false;
    }

    // ===================== BUNKER =====================

    [Serializable]
    public class BunkerProperty
    {
        public bool Owned = false;
        public bool Compromised = false;

        public List<BunkerShipment> PendingShipments = new List<BunkerShipment>();

        public BunkerUpgrades Upgrades = new BunkerUpgrades();
    }

    [Serializable]
    public class BunkerShipment
    {
        public float ArrivesAt;
        public float SuppliesAmount;
    }

    [Serializable]
    public class BunkerUpgrades
    {
        public bool EquipmentOwned = false;
        public bool StaffOwned = false;
        public bool SecurityOwned = false;
    }
}
