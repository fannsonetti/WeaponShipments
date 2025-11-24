using WeaponShipments.Data;

namespace WeaponShipments.Data
{
    public static class BusinessConfig
    {
        // ---------------- CORE LIMITS ----------------

        public static int MaxSupplies => WeaponShipmentsPrefs.MaxSupplies.Value;
        public static int MaxStock => WeaponShipmentsPrefs.MaxStock.Value;

        // ---------------- BASE ECONOMY ----------------

        public static int BuySuppliesPrice => WeaponShipmentsPrefs.BuySuppliesPrice.Value;
        public static int PriceHyland => WeaponShipmentsPrefs.PriceHyland.Value;
        public static int PriceSerena => WeaponShipmentsPrefs.PriceSerena.Value;

        public static float StockPerSupply => WeaponShipmentsPrefs.StockPerSupply.Value;
        public static float ConversionInterval => WeaponShipmentsPrefs.ConversionInterval.Value;
        public static float BuySuppliesDeliveryDelay => WeaponShipmentsPrefs.BuySuppliesDeliveryDelay.Value;

        // ---------------- RAID CONFIG ----------------

        public static int RaidMinStockToTrigger => WeaponShipmentsPrefs.RaidMinStockToTrigger.Value;
        public static float RaidCheckInterval => WeaponShipmentsPrefs.RaidCheckInterval.Value;
        public static float RaidBaseChance => WeaponShipmentsPrefs.RaidBaseChance.Value;
        public static float RaidLossMinFraction => WeaponShipmentsPrefs.RaidLossMinFraction.Value;
        public static float RaidLossMaxFraction => WeaponShipmentsPrefs.RaidLossMaxFraction.Value;

        // ---------------- EQUIPMENT UPGRADE ----------------

        public static int EquipmentUpgradePrice => WeaponShipmentsPrefs.EquipmentUpgradePrice.Value;
        public static float EquipmentValuePerUnitBonus => WeaponShipmentsPrefs.EquipmentValuePerUnitBonus.Value;
        public static float EquipmentStockPerSupplyMult => WeaponShipmentsPrefs.EquipmentStockPerSupplyMult.Value;
        public static float EquipmentProductionSpeedBonus => WeaponShipmentsPrefs.EquipmentProductionSpeedBonus.Value;

        // ---------------- STAFF UPGRADE -------------------

        public static int StaffUpgradePrice => WeaponShipmentsPrefs.StaffUpgradePrice.Value;
        public static float StaffProductionSpeedBonus => WeaponShipmentsPrefs.StaffProductionSpeedBonus.Value;

        // ---------------- SECURITY UPGRADE ----------------

        public static int SecurityUpgradePrice => WeaponShipmentsPrefs.SecurityUpgradePrice.Value;
        public static float SecurityRaidChanceMultiplier => WeaponShipmentsPrefs.SecurityRaidChanceMultiplier.Value;
    }
}
