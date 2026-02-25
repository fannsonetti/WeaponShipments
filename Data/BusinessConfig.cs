using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Data
{
    public static class BusinessConfig
    {
        // ============================================================
        // CORE LIMITS (BUNKER DEFAULTS – PREF DRIVEN)
        // ============================================================

        public static int MaxSupplies => WeaponShipmentsPrefs.MaxSupplies.Value;
        public static int MaxStock => WeaponShipmentsPrefs.MaxStock.Value;

        public static int WarehousePrice => WeaponShipmentsPrefs.WarehousePrice.Value;
        public static int SigningBonus => WeaponShipmentsPrefs.SigningBonus.Value;
        public static int GaragePrice => WeaponShipmentsPrefs.GaragePrice.Value;
        public static int MovingUpMinEarnings => WeaponShipmentsPrefs.MovingUpMinEarnings.Value;

        // ============================================================
        // PROPERTY-SPECIFIC OVERRIDES
        // ============================================================

        public static int GetMaxSupplies(BusinessState.PropertyType property)
        {
            return property switch
            {
                BusinessState.PropertyType.Warehouse => WeaponShipmentsPrefs.WarehouseMaxSupplies.Value,
                BusinessState.PropertyType.Garage => WeaponShipmentsPrefs.GarageMaxSupplies.Value,
                _ => MaxSupplies
            };
        }

        public static int GetMaxStock(BusinessState.PropertyType property)
        {
            return property switch
            {
                BusinessState.PropertyType.Warehouse => WeaponShipmentsPrefs.WarehouseMaxStock.Value,
                BusinessState.PropertyType.Garage => WeaponShipmentsPrefs.GarageMaxStock.Value,
                _ => MaxStock
            };
        }

        public static float GetConversionInterval(BusinessState.PropertyType property)
        {
            return property switch
            {
                BusinessState.PropertyType.Warehouse => WeaponShipmentsPrefs.WarehouseConversionInterval.Value,
                BusinessState.PropertyType.Garage => WeaponShipmentsPrefs.GarageConversionInterval.Value,
                _ => ConversionInterval
            };
        }

        // ============================================================
        // BASE ECONOMY
        // ============================================================

        /// <summary>Calculate price to buy supplies to fill current property. Formula: price_per_unit = round((100 + 160/(1 + 0.0207142857*q))/50)*50, total = price_per_unit * q.</summary>
        public static int GetBuySuppliesPrice(BusinessState.PropertyType property)
        {
            float max = GetMaxSupplies(property);
            float current = BusinessState.GetSupplies(property);
            float q = Mathf.Max(0f, max - current);
            if (q <= 0f) return 0;

            int qInt = Mathf.CeilToInt(q);
            float pricePerUnit = Mathf.Round((100f + 160f / (1f + 0.0207142857f * qInt)) / 50f) * 50f;
            return Mathf.RoundToInt(pricePerUnit * qInt);
        }

        public static int BuySuppliesPrice => WeaponShipmentsPrefs.BuySuppliesPrice.Value;
        public static int PriceHyland => WeaponShipmentsPrefs.PriceHyland.Value;
        public static int PriceSerena => WeaponShipmentsPrefs.PriceSerena.Value;

        public static float StockPerSupply => WeaponShipmentsPrefs.StockPerSupply.Value;
        public static float ConversionInterval => WeaponShipmentsPrefs.ConversionInterval.Value;
        public static float BuySuppliesDeliveryDelay => WeaponShipmentsPrefs.BuySuppliesDeliveryDelay.Value;

        // ============================================================
        // RAID CONFIG
        // ============================================================

        public static int RaidMinStockToTrigger => WeaponShipmentsPrefs.RaidMinStockToTrigger.Value;
        public static float RaidCheckInterval => WeaponShipmentsPrefs.RaidCheckInterval.Value;
        public static float RaidBaseChance => WeaponShipmentsPrefs.RaidBaseChance.Value;
        public static float RaidLossMinFraction => WeaponShipmentsPrefs.RaidLossMinFraction.Value;
        public static float RaidLossMaxFraction => WeaponShipmentsPrefs.RaidLossMaxFraction.Value;

        // ============================================================
        // EQUIPMENT UPGRADE (BUNKER ONLY – GATED IN BusinessState)
        // ============================================================

        public static int EquipmentUpgradePrice => WeaponShipmentsPrefs.EquipmentUpgradePrice.Value;
        public static float EquipmentValuePerUnitBonus => WeaponShipmentsPrefs.EquipmentValuePerUnitBonus.Value;
        public static float EquipmentStockPerSupplyMult => WeaponShipmentsPrefs.EquipmentStockPerSupplyMult.Value;
        public static float EquipmentProductionSpeedBonus => WeaponShipmentsPrefs.EquipmentProductionSpeedBonus.Value;

        // ============================================================
        // STAFF UPGRADE (BUNKER ONLY)
        // ============================================================

        public static int StaffUpgradePrice => WeaponShipmentsPrefs.StaffUpgradePrice.Value;
        public static float StaffProductionSpeedBonus => WeaponShipmentsPrefs.StaffProductionSpeedBonus.Value;

        // ============================================================
        // SECURITY UPGRADE (BUNKER ONLY)
        // ============================================================

        public static int SecurityUpgradePrice => WeaponShipmentsPrefs.SecurityUpgradePrice.Value;
        public static float SecurityRaidChanceMultiplier => WeaponShipmentsPrefs.SecurityRaidChanceMultiplier.Value;

        // ============================================================
        // BUY BUST CONFIG
        // ============================================================

        public static float BuyBustTier1MaxEarnings => WeaponShipmentsPrefs.BuyBustTier1MaxEarnings.Value;
        public static float BuyBustTier2MaxEarnings => WeaponShipmentsPrefs.BuyBustTier2MaxEarnings.Value;

        public static float BuyBustChanceTier1 => WeaponShipmentsPrefs.BuyBustChanceTier1.Value;
        public static float BuyBustChanceTier2 => WeaponShipmentsPrefs.BuyBustChanceTier2.Value;
        public static float BuyBustChanceTier3 => WeaponShipmentsPrefs.BuyBustChanceTier3.Value;
    }
}
