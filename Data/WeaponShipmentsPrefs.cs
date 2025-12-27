using MelonLoader;

namespace WeaponShipments.Data
{
    public static class WeaponShipmentsPrefs
    {
        private static readonly MelonPreferences_Category _category;

        // ---------------- CORE LIMITS ----------------
        public static MelonPreferences_Entry<int> MaxSupplies;
        public static MelonPreferences_Entry<int> MaxStock;

        // ---------------- PROPERTY LIMITS (NEW) ----------------
        public static MelonPreferences_Entry<int> WarehouseMaxSupplies;
        public static MelonPreferences_Entry<int> WarehouseMaxStock;
        public static MelonPreferences_Entry<float> WarehouseConversionInterval;

        public static MelonPreferences_Entry<int> GarageMaxSupplies;
        public static MelonPreferences_Entry<int> GarageMaxStock;
        public static MelonPreferences_Entry<float> GarageConversionInterval;

        // ---------------- BASE ECONOMY ----------------
        public static MelonPreferences_Entry<int> BuySuppliesPrice;
        public static MelonPreferences_Entry<float> BuySuppliesDeliveryDelay;
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

        // ---------------- BUY BUST CONFIG ----------------
        public static MelonPreferences_Entry<float> BuyBustTier1MaxEarnings;
        public static MelonPreferences_Entry<float> BuyBustTier2MaxEarnings;

        public static MelonPreferences_Entry<float> BuyBustChanceTier1;
        public static MelonPreferences_Entry<float> BuyBustChanceTier2;
        public static MelonPreferences_Entry<float> BuyBustChanceTier3;

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
                "Maximum supplies storage (default / bunker)."
            );

            MaxStock = _category.CreateEntry(
                "MaxStock",
                100,
                "Max Stock",
                "Maximum stock storage (default / bunker)."
            );

            // ---------------- PROPERTY LIMITS (NEW) ----------------
            WarehouseMaxSupplies = _category.CreateEntry(
                "WarehouseMaxSupplies",
                5,
                "Warehouse Max Supplies",
                "Maximum supplies storage for Warehouse."
            );

            WarehouseMaxStock = _category.CreateEntry(
                "WarehouseMaxStock",
                10,
                "Warehouse Max Stock",
                "Maximum stock storage for Warehouse."
            );

            WarehouseConversionInterval = _category.CreateEntry(
                "WarehouseConversionInterval",
                240f,
                "Warehouse Conversion Interval",
                "Seconds between automatic supply-to-stock conversions for Warehouse."
            );

            GarageMaxSupplies = _category.CreateEntry(
                "GarageMaxSupplies",
                40,
                "Garage Max Supplies",
                "Maximum supplies storage for Garage."
            );

            GarageMaxStock = _category.CreateEntry(
                "GarageMaxStock",
                100,
                "Garage Max Stock",
                "Maximum stock storage for Garage."
            );

            GarageConversionInterval = _category.CreateEntry(
                "GarageConversionInterval",
                120f,
                "Garage Conversion Interval",
                "Seconds between automatic supply-to-stock conversions for Garage."
            );

            // ---------------- BASE ECONOMY ----------------
            BuySuppliesPrice = _category.CreateEntry(
                "BuySuppliesPrice",
                7500,
                "Buy Supplies Price",
                "Cost to buy a full batch of supplies."
            );

            BuySuppliesDeliveryDelay = _category.CreateEntry(
                "BuySuppliesDeliveryDelay",
                600f,
                "Supplies Delivery Delay",
                "How long (in seconds) a purchased supply shipment takes to arrive."
            );

            PriceHyland = _category.CreateEntry(
                "PriceHyland",
                500,
                "Hyland Point Price",
                "Money earned per unit stock when selling to Hyland Point."
            );

            PriceSerena = _category.CreateEntry(
                "PriceSerena",
                750,
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
                120f,
                "Conversion Interval",
                "Seconds between automatic supply-to-stock conversions (default / bunker)."
            );

            // ---------------- RAID CONFIG ----------------
            RaidMinStockToTrigger = _category.CreateEntry(
                "RaidMinStockToTrigger",
                20,
                "Minimum Stock For Raids",
                "Raids only occur when your stock is above this amount."
            );

            RaidCheckInterval = _category.CreateEntry(
                "RaidCheckInterval",
                60f,
                "Raid Check Interval",
                "Seconds between raid chance checks."
            );

            RaidBaseChance = _category.CreateEntry(
                "RaidBaseChance",
                0.02f,
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
                115500,
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
                59800,
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
                35000,
                "Security Upgrade Price",
                "Cost of the Security upgrade."
            );

            SecurityRaidChanceMultiplier = _category.CreateEntry(
                "SecurityRaidChanceMultiplier",
                0.25f,
                "Security Raid Chance Multiplier",
                "Raid change from Security upgrade (0.25 = -75%)."
            );

            // ---------------- BUY BUST CONFIG ----------------
            BuyBustTier1MaxEarnings = _category.CreateEntry(
                "BuyBustTier1MaxEarnings",
                25000f,
                "Buy Bust Tier 1 Max Earnings",
                "Tier 1 applies when TotalEarnings <= this value."
            );

            BuyBustTier2MaxEarnings = _category.CreateEntry(
                "BuyBustTier2MaxEarnings",
                100000f,
                "Buy Bust Tier 2 Max Earnings",
                "Tier 2 applies when TotalEarnings is between Tier1Max and this value. Tier 3 is above this."
            );

            BuyBustChanceTier1 = _category.CreateEntry(
                "BuyBustChanceTier1",
                0.05f,
                "Buy Bust Chance (Tier 1)",
                "Chance (0–1) that a purchased supplies delivery triggers a bust in Tier 1."
            );

            BuyBustChanceTier2 = _category.CreateEntry(
                "BuyBustChanceTier2",
                0.1f,
                "Buy Bust Chance (Tier 2)",
                "Chance (0–1) that a purchased supplies delivery triggers a bust in Tier 2."
            );

            BuyBustChanceTier3 = _category.CreateEntry(
                "BuyBustChanceTier3",
                0.15f,
                "Buy Bust Chance (Tier 3)",
                "Chance (0–1) that a purchased supplies delivery triggers a bust in Tier 3."
            );
        }
    }
}
