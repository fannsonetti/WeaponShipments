using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WeaponShipments.NPCs;

namespace WeaponShipments.Data
{
    public static class BusinessState
    {
        // ---------------- PROPERTY MODEL ----------------

        public enum PropertyType
        {
            Bunker,
            Warehouse,
            Garage
        }

        public static PropertyType ActiveProperty { get; private set; } = PropertyType.Bunker;

        public static void SetActiveProperty(PropertyType property)
        {
            ActiveProperty = property;
            // No auto-save here; the caller/UI can decide persistence semantics.
        }

        public static bool PropertyAllowsUpgrades(PropertyType p)
        {
            // Per your spec:
            // - Bunker: upgrades allowed
            // - Warehouse: no upgrades
            // - Garage: no upgrades
            return p == PropertyType.Bunker;
        }

        public static bool PropertyAllowsResearch(PropertyType p)
        {
            // Per your spec:
            // - Garage should have research
            // - Bunker already has research UI (placeholder today)
            // - Warehouse unspecified; keeping as "no research gating" is safe,
            //   but if you want it locked, change to: (p == PropertyType.Bunker || p == PropertyType.Garage)
            return p == PropertyType.Bunker || p == PropertyType.Garage;
        }

        // ---------------- PER-PROPERTY STORAGE ----------------

        // In-memory per-property stores (so storages are separate).
        private static readonly Dictionary<PropertyType, float> _suppliesByProperty = new();
        private static readonly Dictionary<PropertyType, float> _stockByProperty = new();

        private static float GetSupplies(PropertyType p)
            => _suppliesByProperty.TryGetValue(p, out var v) ? v : 0f;

        private static float GetStock(PropertyType p)
            => _stockByProperty.TryGetValue(p, out var v) ? v : 0f;

        private static void SetSupplies(PropertyType p, float v)
            => _suppliesByProperty[p] = Round3(v);

        private static void SetStock(PropertyType p, float v)
            => _stockByProperty[p] = Round3(v);

        public static float Supplies => GetSupplies(ActiveProperty);
        public static float Stock => GetStock(ActiveProperty);

        // ---------------- UTILS ----------------

        // Now accessible everywhere
        public static float Round3(float value)
        {
            return Mathf.Round(value * 1000f) / 1000f;
        }

        // ---------------- PROPERTY-SPECIFIC LIMITS / TIMING ----------------

        // Warehouse rules (per your instruction)
        private const float WarehouseConversionIntervalSeconds = 240f;
        private const int WarehouseMaxStock = 10;
        private const int WarehouseMaxSupplies = 5;

        private static int GetMaxStock(PropertyType p)
        {
            if (p == PropertyType.Warehouse) return WarehouseMaxStock;
            return BusinessConfig.MaxStock;
        }

        private static int GetMaxSupplies(PropertyType p)
        {
            if (p == PropertyType.Warehouse) return WarehouseMaxSupplies;
            return BusinessConfig.MaxSupplies;
        }

        private static float GetBaseConversionInterval(PropertyType p)
        {
            if (p == PropertyType.Warehouse) return WarehouseConversionIntervalSeconds;
            return BusinessConfig.ConversionInterval;
        }

        // ---------------- SUPPLIES ----------------

        public static bool TryAddSupplies(float amount)
        {
            if (amount <= 0)
                return false;

            var p = ActiveProperty;
            float supplies = GetSupplies(p);

            int max = GetMaxSupplies(p);
            if (supplies >= max)
                return false;

            supplies = Mathf.Clamp(supplies + amount, 0, max);
            supplies = Round3(supplies);

            SetSupplies(p, supplies);

            MelonLogger.Msg(
                "[BusinessState] Supplies changed ({0}): {1}/{2}",
                p, supplies, max
            );

            SyncToSaveable();
            return true;
        }

        public static bool TryConsumeSupplies(float amount)
        {
            if (amount <= 0)
                return false;

            var p = ActiveProperty;
            float supplies = GetSupplies(p);

            if (supplies < amount)
                return false;

            float before = supplies;

            supplies -= amount;
            supplies = Round3(supplies);

            SetSupplies(p, supplies);

            // Notify ONLY when supplies reach 0
            if (before > 0f && supplies <= 0f)
            {
                Agent28.NotifySuppliesEmpty();
            }

            SyncToSaveable();
            return true;
        }

        // ---------------- STOCK ----------------

        public static bool TryAddStock(float amount)
        {
            if (amount <= 0)
                return false;

            var p = ActiveProperty;
            float stock = GetStock(p);

            int max = GetMaxStock(p);
            if (stock >= max)
                return false;

            float before = stock;

            stock = Mathf.Clamp(stock + amount, 0, max);
            stock = Round3(stock);

            SetStock(p, stock);

            MelonLogger.Msg(
                "[BusinessState] Stock changed ({0}): {1}/{2}",
                p, stock, max
            );

            // Hit max for the first time this step?
            if (before < max && stock >= max)
            {
                Agent28.NotifyStockFull(max);
            }

            SyncToSaveable();
            return true;
        }

        public static bool TryConsumeStock(float amount)
        {
            if (amount <= 0) return false;

            var p = ActiveProperty;
            float stock = GetStock(p);

            if (stock < amount) return false;

            stock -= amount;
            stock = Round3(stock);

            SetStock(p, stock);

            SyncToSaveable();
            return true;
        }

        // ---------------- UPGRADES STATE ----------------
        // Note: upgrades are only EFFECTIVE in the Bunker. They remain stored globally.

        private static bool _equipmentUpgradeOwned;
        private static bool _staffUpgradeOwned;
        private static bool _securityUpgradeOwned;

        public static bool EquipmentUpgradeOwned => _equipmentUpgradeOwned;
        public static bool StaffUpgradeOwned => _staffUpgradeOwned;
        public static bool SecurityUpgradeOwned => _securityUpgradeOwned;

        public static bool TryBuyEquipmentUpgrade()
        {
            if (!PropertyAllowsUpgrades(ActiveProperty))
                return false;

            if (_equipmentUpgradeOwned)
                return false;

            _equipmentUpgradeOwned = true;
            MelonLogger.Msg("[BusinessState] Equipment upgrade purchased.");
            SyncToSaveable();
            return true;
        }

        public static bool TryBuyStaffUpgrade()
        {
            if (!PropertyAllowsUpgrades(ActiveProperty))
                return false;

            if (_staffUpgradeOwned)
                return false;

            _staffUpgradeOwned = true;
            MelonLogger.Msg("[BusinessState] Staff upgrade purchased.");
            SyncToSaveable();
            return true;
        }

        public static bool TryBuySecurityUpgrade()
        {
            if (!PropertyAllowsUpgrades(ActiveProperty))
                return false;

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
            if (!PropertyAllowsUpgrades(ActiveProperty))
                return 1f;

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

            if (PropertyAllowsUpgrades(ActiveProperty) && _equipmentUpgradeOwned)
                amount *= BusinessConfig.EquipmentStockPerSupplyMult;

            return amount;
        }

        public static float GetProductionSpeedMultiplier()
        {
            if (!PropertyAllowsUpgrades(ActiveProperty))
                return 1f;

            float mult = 1f;

            if (_equipmentUpgradeOwned)
                mult += BusinessConfig.EquipmentProductionSpeedBonus;

            if (_staffUpgradeOwned)
                mult += BusinessConfig.StaffProductionSpeedBonus;

            return mult;
        }

        public static float GetRaidChanceMultiplier()
        {
            if (!PropertyAllowsUpgrades(ActiveProperty))
                return 1f;

            float mult = 1f;

            if (_securityUpgradeOwned)
                mult *= BusinessConfig.SecurityRaidChanceMultiplier;

            return mult;
        }

        public static float GetEffectiveConversionInterval()
        {
            float baseInterval = GetBaseConversionInterval(ActiveProperty);

            // Warehouse uses a fixed interval and no upgrades; multiplier will be 1 anyway,
            // but we keep the common pathway.
            float speedMult = GetProductionSpeedMultiplier();

            if (speedMult <= 0f)
                speedMult = 0.0001f;

            return baseInterval / speedMult;
        }

        // --------------- STATS / METRICS (HOME PAGE) ---------------
        // These remain global totals (as in your current implementation).

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
        /// Backwards compatible: if Warehouse/Garage fields don't exist yet, they default to 0.
        /// </summary>
        internal static void ApplyLoadedData(WeaponShipmentsSaveData.PersistedData data)
        {
            if (data == null)
                return;

            // Default: your existing save schema = bunker core resources.
            SetSupplies(PropertyType.Bunker, data.Supplies);
            SetStock(PropertyType.Bunker, data.Stock);

            // Optional extended schema: try to read per-property values if they exist.
            // This avoids hard compile dependency on new fields while you iterate.
            TryLoadFloatField(data, "WarehouseSupplies", v => SetSupplies(PropertyType.Warehouse, v));
            TryLoadFloatField(data, "WarehouseStock", v => SetStock(PropertyType.Warehouse, v));
            TryLoadFloatField(data, "GarageSupplies", v => SetSupplies(PropertyType.Garage, v));
            TryLoadFloatField(data, "GarageStock", v => SetStock(PropertyType.Garage, v));

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
                "[BusinessState] Loaded save data: bunker supplies={0}, bunker stock={1}, earnings={2}",
                GetSupplies(PropertyType.Bunker),
                GetStock(PropertyType.Bunker),
                _totalEarnings
            );
        }

        private static void TryLoadFloatField(object data, string fieldName, Action<float> apply)
        {
            try
            {
                var f = data.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if (f == null || f.FieldType != typeof(float))
                    return;

                var val = (float)f.GetValue(data);
                apply?.Invoke(val);
            }
            catch
            {
                // intentionally ignore: field not present / incompatible
            }
        }

        /// <summary>
        /// Push current in-memory state into the S1API saveable (if it exists).
        /// Backwards compatible: writes bunker fields always; writes Warehouse/Garage fields only if present.
        /// </summary>
        private static void SyncToSaveable()
        {
            var save = WeaponShipmentsSaveData.Instance;
            var data = save != null ? save.Data : null;
            if (data == null)
                return;

            // Always keep your original save schema in sync (bunker).
            data.Supplies = GetSupplies(PropertyType.Bunker);
            data.Stock = GetStock(PropertyType.Bunker);

            // Extended schema (optional):
            TryWriteFloatField(data, "WarehouseSupplies", GetSupplies(PropertyType.Warehouse));
            TryWriteFloatField(data, "WarehouseStock", GetStock(PropertyType.Warehouse));
            TryWriteFloatField(data, "GarageSupplies", GetSupplies(PropertyType.Garage));
            TryWriteFloatField(data, "GarageStock", GetStock(PropertyType.Garage));

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

        private static void TryWriteFloatField(object data, string fieldName, float value)
        {
            try
            {
                var f = data.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
                if (f == null || f.FieldType != typeof(float))
                    return;

                f.SetValue(data, value);
            }
            catch
            {
                // intentionally ignore: field not present / incompatible
            }
        }
    }
}
