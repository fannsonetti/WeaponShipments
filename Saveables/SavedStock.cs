using System;

namespace WeaponShipments.Saveables
{
    [Serializable]
    public class SavedStock
    {
        public float WarehouseSupplies = 0f;
        public float WarehouseStock = 0f;

        public float GarageSupplies = 0f;
        public float GarageStock = 0f;

        public float BunkerSupplies = 0f;
        public float BunkerStock = 0f;
    }
}
