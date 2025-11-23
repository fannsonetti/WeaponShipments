using MelonLoader;
using UnityEngine;
using WeaponShipments.NPCs;

namespace WeaponShipments.Data
{
    public static class BusinessState
    {
        private static float _supplies;
        private static float _stock;

        public static float Supplies => _supplies;
        public static float Stock => _stock;

        // Now accessible everywhere
        public static float Round3(float value)
        {
            return Mathf.Round(value * 1000f) / 1000f;
        }

        // ---------------- SUPPLIES ----------------

        public static bool TryAddSupplies(float amount)
        {
            if (amount <= 0)
                return false;

            if (_supplies >= BusinessConfig.MaxSupplies)
                return false;

            _supplies = Mathf.Clamp(_supplies + amount, 0, BusinessConfig.MaxSupplies);
            _supplies = Round3(_supplies);

            MelonLogger.Msg(
                "[BusinessState] Supplies changed: {0}/{1}",
                _supplies, BusinessConfig.MaxSupplies
            );

            return true;
        }

        public static bool TryConsumeSupplies(float amount)
        {
            if (amount <= 0)
                return false;

            if (_supplies < amount)
                return false;

            float before = _supplies;

            _supplies -= amount;
            _supplies = Round3(_supplies);

            // Notify ONLY when supplies reach 0
            if (before > 0f && _supplies <= 0f)
            {
                Agent28.NotifySuppliesEmpty();
            }

            return true;
        }

        // ---------------- STOCK ----------------

        public static bool TryAddStock(float amount)
        {
            if (amount <= 0)
                return false;

            if (_stock >= BusinessConfig.MaxStock)
                return false;

            float before = _stock;

            _stock = Mathf.Clamp(_stock + amount, 0, BusinessConfig.MaxStock);
            _stock = Round3(_stock);

            MelonLogger.Msg(
                "[BusinessState] Stock changed: {0}/{1}",
                _stock, BusinessConfig.MaxStock
            );

            // Hit max for the first time this step?
            if (before < BusinessConfig.MaxStock && _stock >= BusinessConfig.MaxStock)
            {
                Agent28.NotifyStockFull(BusinessConfig.MaxStock);
            }

            return true;
        }

        public static bool TryConsumeStock(float amount)
        {
            if (amount <= 0) return false;
            if (_stock < amount) return false;

            _stock -= amount;
            _stock = Round3(_stock);

            return true;
        }

        // UPGRADES STATE
        private static bool _equipmentUpgradeOwned;
        private static bool _staffUpgradeOwned;
        private static bool _securityUpgradeOwned;

        public static bool EquipmentUpgradeOwned => _equipmentUpgradeOwned;
        public static bool StaffUpgradeOwned => _staffUpgradeOwned;
        public static bool SecurityUpgradeOwned => _securityUpgradeOwned;

        public static bool TryBuyEquipmentUpgrade()
        {
            if (_equipmentUpgradeOwned)
                return false;

            _equipmentUpgradeOwned = true;
            MelonLogger.Msg("[BusinessState] Equipment upgrade purchased.");
            SyncToSaveable();
            return true;
        }

        public static bool TryBuyStaffUpgrade()
        {
            if (_staffUpgradeOwned)
                return false;

            _staffUpgradeOwned = true;
            MelonLogger.Msg("[BusinessState] Staff upgrade purchased.");
            SyncToSaveable();
            return true;
        }

        public static bool TryBuySecurityUpgrade()
        {
            if (_securityUpgradeOwned)
                return false;

            _securityUpgradeOwned = true;
            MelonLogger.Msg("[BusinessState] Security upgrade purchased.");
            SyncToSaveable();
            return true;
        }

        /// <summary>
        /// Equipment: +X% value per unit.
        /// Used by SellCalculator.
        /// </summary>
        public static float GetValuePerUnitMultiplier()
        {
            float mult = 1f;

            if (_equipmentUpgradeOwned)
                mult += BusinessConfig.EquipmentValuePerUnitBonus;

            return mult;
        }

        /// <summary>
        /// Stock produced per 1 supply.
        /// </summary>
        public static float GetStockPerSupply()
        {
            float amount = BusinessConfig.StockPerSupply;

            // Equipment upgrade improves efficiency
            if (_equipmentUpgradeOwned)
                amount *= BusinessConfig.EquipmentStockPerSupplyMult;

            return amount;
        }

        public static float GetProductionSpeedMultiplier()
        {
            float mult = 1f;

            if (_equipmentUpgradeOwned)
                mult += BusinessConfig.EquipmentProductionSpeedBonus;

            if (_staffUpgradeOwned)
                mult += BusinessConfig.StaffProductionSpeedBonus;

            return mult;
        }

        public static float GetRaidChanceMultiplier()
        {
            float mult = 1f;

            if (_securityUpgradeOwned)
                mult *= BusinessConfig.SecurityRaidChanceMultiplier;

            return mult;
        }

        public static float GetEffectiveConversionInterval()
        {
            float baseInterval = BusinessConfig.ConversionInterval;
            float speedMult = GetProductionSpeedMultiplier();

            if (speedMult <= 0f)
                speedMult = 0.0001f;

            return baseInterval / speedMult;
        }

        // --------------- STATS / METRICS (HOME PAGE) ---------------

        private static float _totalEarnings;
        private static int _totalSalesCount;
        private static float _totalStockProduced;

        private static int _resupplyJobsStarted;
        private static int _resupplyJobsCompleted;

        private static int _hylandSellAttempts;
        private static int _hylandSellSuccesses;

        private static bool _sellJobInProgress;

        public static bool SellJobInProgress => _sellJobInProgress;

        public static bool TryBeginSellJob()
        {
            if (_sellJobInProgress)
                return false;

            _sellJobInProgress = true;
            return true;
        }

        public static void ClearSellJobFlag()
        {
            _sellJobInProgress = false;
        }

        public static float TotalEarnings => _totalEarnings;
        public static int TotalSalesCount => _totalSalesCount;
        public static float TotalStockProduced => _totalStockProduced;

        public static int ResupplyJobsStarted => _resupplyJobsStarted;
        public static int ResupplyJobsCompleted => _resupplyJobsCompleted;
        public static int HylandSellAttempts => _hylandSellAttempts;
        public static int HylandSellSuccesses => _hylandSellSuccesses;

        public static void RegisterStockProduced(float amount)
        {
            if (amount <= 0f) return;
            _totalStockProduced += amount;
            SyncToSaveable();
        }

        public static void RegisterSale(float saleValue, bool isHyland)
        {
            if (saleValue <= 0f) return;

            _totalEarnings += saleValue;
            _totalSalesCount++;

            if (isHyland)
                _hylandSellSuccesses++;

            SyncToSaveable();
        }

        public static void RegisterSellAttempt(bool isHyland)
        {
            if (!isHyland)
                return; // we don't track Serena yet

            _hylandSellAttempts++;
            SyncToSaveable();
        }

        public static void RegisterResupplyJobStarted()
        {
            _resupplyJobsStarted++;
            SyncToSaveable();
        }

        public static void RegisterResupplyJobCompleted()
        {
            if (_resupplyJobsCompleted < _resupplyJobsStarted)
                _resupplyJobsCompleted++;

            SyncToSaveable();
        }

        public static float GetResupplySuccessRate()
        {
            if (_resupplyJobsStarted <= 0)
                return 0f;

            return (float)_resupplyJobsCompleted / _resupplyJobsStarted;
        }

        public static float GetHylandSellSuccessRate()
        {
            if (_hylandSellAttempts <= 0)
                return 0f;

            return (float)_hylandSellSuccesses / _hylandSellAttempts;
        }

        // --------------- SAVEABLE SYNC ---------------

        /// <summary>
        /// Called by WeaponShipmentsSaveData.OnLoaded to hydrate this static state from the save file.
        /// </summary>
        internal static void ApplyLoadedData(WeaponShipmentsSaveData.PersistedData data)
        {
            if (data == null)
                return;

            _supplies = data.Supplies;
            _stock = data.Stock;

            _equipmentUpgradeOwned = data.EquipmentOwned;
            _staffUpgradeOwned = data.StaffOwned;
            _securityUpgradeOwned = data.SecurityOwned;

            _totalEarnings = data.TotalEarnings;
            _totalSalesCount = data.TotalSalesCount;
            _totalStockProduced = data.TotalStockProduced;

            _resupplyJobsStarted = data.ResupplyJobsStarted;
            _resupplyJobsCompleted = data.ResupplyJobsCompleted;
            _hylandSellAttempts = data.HylandSellAttempts;
            _hylandSellSuccesses = data.HylandSellSuccesses;

            MelonLogger.Msg(
                "[BusinessState] Loaded save data: supplies={0}, stock={1}, earnings={2}",
                _supplies,
                _stock,
                _totalEarnings
            );
        }

        /// <summary>
        /// Push current in-memory state into the S1API saveable (if it exists).
        /// This is what actually makes your numbers get written to the JSON save.
        /// </summary>
        private static void SyncToSaveable()
        {
            var instance = WeaponShipmentsSaveData.Instance;
            if (instance == null)
                return; // Save system not ready yet

            var data = instance.Data;
            if (data == null)
                return;

            data.Supplies = _supplies;
            data.Stock = _stock;

            data.EquipmentOwned = _equipmentUpgradeOwned;
            data.StaffOwned = _staffUpgradeOwned;
            data.SecurityOwned = _securityUpgradeOwned;

            data.TotalEarnings = _totalEarnings;
            data.TotalSalesCount = _totalSalesCount;
            data.TotalStockProduced = _totalStockProduced;

            data.ResupplyJobsStarted = _resupplyJobsStarted;
            data.ResupplyJobsCompleted = _resupplyJobsCompleted;
            data.HylandSellAttempts = _hylandSellAttempts;
            data.HylandSellSuccesses = _hylandSellSuccesses;
        }
    }
}