using MelonLoader;

namespace WeaponShipments.Data
{
    public static class WeaponShipmentsPrefs
    {
        private static readonly MelonPreferences_Category _category;

        // ---------------- CORE LIMITS ----------------
        public static MelonPreferences_Entry<int> MaxSupplies;
        public static MelonPreferences_Entry<int> MaxStock;

        // ---------------- BASE ECONOMY ----------------
        public static MelonPreferences_Entry<int> BuySuppliesPrice;
        public static MelonPreferences_Entry<int> PriceHyland;
        public static MelonPreferences_Entry<int> PriceSerena;

        public static MelonPreferences_Entry<float> StockPerSupply;
        public static MelonPreferences_Entry<float> ConversionInterval;

        // ---------------- RAID CONFIG ----------------
        public static MelonPreferences_Entry<int> RaidMinStockToTrigger;
        public static MelonPreferences_Entry<float> RaidCheckInterval;
        public static MelonPreferences_Entry<float> RaidBaseChance;
        public static MelonPreferences_Entry<float> RaidLossMinFraction;
        public static MelonPreferences_Entry<float> RaidLossMaxFraction;

        // ---------------- EQUIPMENT UPGRADE ----------------
        public static MelonPreferences_Entry<int> EquipmentUpgradePrice;
        public static MelonPreferences_Entry<float> EquipmentValuePerUnitBonus;
        public static MelonPreferences_Entry<float> EquipmentStockPerSupplyMult;
        public static MelonPreferences_Entry<float> EquipmentProductionSpeedBonus;

        // ---------------- STAFF UPGRADE -------------------
        public static MelonPreferences_Entry<int> StaffUpgradePrice;
        public static MelonPreferences_Entry<float> StaffProductionSpeedBonus;

        // ---------------- SECURITY UPGRADE ----------------
        public static MelonPreferences_Entry<int> SecurityUpgradePrice;
        public static MelonPreferences_Entry<float> SecurityRaidChanceMultiplier;


        // STATIC CONSTRUCTOR
        static WeaponShipmentsPrefs()
        {
            _category = MelonPreferences.CreateCategory(
                "WeaponShipments",
                "Weapon Shipments"
            );

            // ---------------- CORE LIMITS ----------------
            MaxSupplies = _category.CreateEntry(
                "MaxSupplies",
                40,
                "Max Supplies",
                "Maximum supplies storage."
            );

            MaxStock = _category.CreateEntry(
                "MaxStock",
                100,
                "Max Stock",
                "Maximum stock storage."
            );

            // ---------------- BASE ECONOMY ----------------
            BuySuppliesPrice = _category.CreateEntry(
                "BuySuppliesPrice",
                75000,
                "Buy Supplies Price",
                "Cost to buy a full batch of supplies."
            );

            PriceHyland = _category.CreateEntry(
                "PriceHyland",
                5000,
                "Hyland Point Price",
                "Money earned per unit stock when selling to Hyland Point."
            );

            PriceSerena = _category.CreateEntry(
                "PriceSerena",
                7500,
                "Serena Flats Price",
                "Money earned per unit stock when selling to Serena Flats."
            );

            StockPerSupply = _category.CreateEntry(
                "StockPerSupply",
                0.35f,
                "Stock Per Supply",
                "Base stock created from 1 supply before upgrades."
            );

            ConversionInterval = _category.CreateEntry(
                "ConversionInterval",
                5f,
                "Conversion Interval",
                "Seconds between automatic supply-to-stock conversions."
            );

            // ---------------- RAID CONFIG ----------------
            RaidMinStockToTrigger = _category.CreateEntry(
                "RaidMinStockToTrigger",
                25,
                "Minimum Stock For Raids",
                "Raids only occur when your stock is above this amount."
            );

            RaidCheckInterval = _category.CreateEntry(
                "RaidCheckInterval",
                5f,
                "Raid Check Interval",
                "Seconds between raid chance checks."
            );

            RaidBaseChance = _category.CreateEntry(
                "RaidBaseChance",
                0.01f,
                "Raid Base Chance",
                "Base chance (0–1) at 100% stock for a raid to trigger."
            );

            RaidLossMinFraction = _category.CreateEntry(
                "RaidLossMinFraction",
                0.25f,
                "Raid Loss Min Fraction",
                "Minimum percent (0–1) of stock lost during a raid."
            );

            RaidLossMaxFraction = _category.CreateEntry(
                "RaidLossMaxFraction",
                0.75f,
                "Raid Loss Max Fraction",
                "Maximum percent (0–1) of stock lost during a raid."
            );

            // ---------------- EQUIPMENT UPGRADE ----------------
            EquipmentUpgradePrice = _category.CreateEntry(
                "EquipmentUpgradePrice",
                1155000,
                "Equipment Upgrade Price",
                "Cost of the Equipment upgrade."
            );

            EquipmentValuePerUnitBonus = _category.CreateEntry(
                "EquipmentValuePerUnitBonus",
                0.4f,
                "Equipment Value Bonus",
                "Extra sell value from Equipment upgrade (0.4 = +40%)."
            );

            EquipmentStockPerSupplyMult = _category.CreateEntry(
                "EquipmentStockPerSupplyMult",
                1.5f,
                "Equipment Stock/Supply Multiplier",
                "Multiplier on stock output per supply when Equipment is owned."
            );

            EquipmentProductionSpeedBonus = _category.CreateEntry(
                "EquipmentProductionSpeedBonus",
                0.5f,
                "Equipment Speed Bonus",
                "Extra production speed from Equipment upgrade (0.5 = +50%)."
            );

            // ---------------- STAFF UPGRADE -------------------
            StaffUpgradePrice = _category.CreateEntry(
                "StaffUpgradePrice",
                598500,
                "Staff Upgrade Price",
                "Cost of the Staff upgrade."
            );

            StaffProductionSpeedBonus = _category.CreateEntry(
                "StaffProductionSpeedBonus",
                0.25f,
                "Staff Speed Bonus",
                "Extra production speed from Staff upgrade (0.25 = +25%)."
            );

            // ---------------- SECURITY UPGRADE ----------------
            SecurityUpgradePrice = _category.CreateEntry(
                "SecurityUpgradePrice",
                351000,
                "Security Upgrade Price",
                "Cost of the Security upgrade."
            );

            SecurityRaidChanceMultiplier = _category.CreateEntry(
                "SecurityRaidChanceMultiplier",
                0.5f,
                "Security Raid Chance Multiplier",
                "Raid chance is multiplied by this when Security is owned."
            );
        }
    }
}
