using MelonLoader;
using System;
using System.Collections.Generic;
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

        private static PropertyType _activeProperty = PropertyType.Bunker;

        /// <summary>
        /// Which property the player is currently operating from (UI-controlled).
        /// </summary>
        public static PropertyType ActiveProperty => _activeProperty;

        public static void SetActiveProperty(PropertyType p)
        {
            _activeProperty = p;
            MelonLogger.Msg("[BusinessState] Active property set to: {0}", p);
            SyncToSaveable();
        }

        public static bool PropertyAllowsRaids(PropertyType p)
        {
            // Per your current spec:
            // - Warehouse: raids can occur
            // - Garage: raids can occur (until the story seizure event, later)
            // - Bunker: raids do not occur
            return p != PropertyType.Bunker;
        }

        public static bool PropertyAllowsUpgrades(PropertyType p)
        {
            // Per updated spec:
            // - Bunker: upgrades allowed
            // - Garage: upgrades allowed
            // - Warehouse: no upgrades
            return p == PropertyType.Bunker || p == PropertyType.Garage;
        }

        /// <summary>
        /// Base conversion interval per property. Used by production timers/UI.
        /// </summary>
        public static float GetBaseConversionInterval(PropertyType p)
        {
            // Delegated to prefs-driven config per property
            return BusinessConfig.GetConversionInterval(p);
        }

        // ---------------- PER-PROPERTY STOCK MODEL ----------------

        private static readonly Dictionary<PropertyType, float> _suppliesByProperty = new();
        private static readonly Dictionary<PropertyType, float> _stockByProperty = new();

        public static float GetSupplies(PropertyType p)
        {
            if (_suppliesByProperty.TryGetValue(p, out var v))
                return v;
            return 0f;
        }

        public static float GetStock(PropertyType p)
        {
            if (_stockByProperty.TryGetValue(p, out var v))
                return v;
            return 0f;
        }

        private static void SetSupplies(PropertyType p, float v)
        {
            _suppliesByProperty[p] = Mathf.Max(0f, v);
        }

        private static void SetStock(PropertyType p, float v)
        {
            _stockByProperty[p] = Mathf.Max(0f, v);
        }

        /// <summary>For console commands: set supplies/stock by property name.</summary>
        public static bool SetSuppliesForProperty(PropertyType p, float v)
        {
            SetSupplies(p, v);
            SyncToSaveable();
            return true;
        }

        /// <summary>For console commands: set stock by property name.</summary>
        public static bool SetStockForProperty(PropertyType p, float v)
        {
            SetStock(p, v);
            SyncToSaveable();
            return true;
        }

        public static float Supplies => GetSupplies(ActiveProperty);
        public static float Stock => GetStock(ActiveProperty);

        // ---------------- SAVE SYNC ----------------

        private static void SyncToSaveable()
        {
            var save = WSSaveData.Instance;
            var data = save != null ? save.Data : null;
            if (data == null)
                return;

            // ---------------- STOCK (PER PROPERTY) ----------------
            data.Stock.WarehouseSupplies = GetSupplies(PropertyType.Warehouse);
            data.Stock.WarehouseStock = GetStock(PropertyType.Warehouse);

            data.Stock.GarageSupplies = GetSupplies(PropertyType.Garage);
            data.Stock.GarageStock = GetStock(PropertyType.Garage);

            data.Stock.BunkerSupplies = GetSupplies(PropertyType.Bunker);
            data.Stock.BunkerStock = GetStock(PropertyType.Bunker);

            // ---------------- UPGRADES (PER PROPERTY) ----------------
            data.Properties.Garage.Upgrades.EquipmentOwned = GetUpgrade(_equipmentOwnedByProperty, PropertyType.Garage);
            data.Properties.Garage.Upgrades.StaffOwned = GetUpgrade(_staffOwnedByProperty, PropertyType.Garage);
            data.Properties.Garage.Upgrades.SecurityOwned = GetUpgrade(_securityOwnedByProperty, PropertyType.Garage);

            data.Properties.Bunker.Upgrades.EquipmentOwned = GetUpgrade(_equipmentOwnedByProperty, PropertyType.Bunker);
            data.Properties.Bunker.Upgrades.StaffOwned = GetUpgrade(_staffOwnedByProperty, PropertyType.Bunker);
            data.Properties.Bunker.Upgrades.SecurityOwned = GetUpgrade(_securityOwnedByProperty, PropertyType.Bunker);

            // ---------------- STATS ----------------
            data.Stats.TotalEarnings = _totalEarnings;
            data.Stats.TotalSalesCount = _totalSalesCount;
            data.Stats.TotalStockProduced = _totalStockProduced;

            data.Stats.ResupplyJobsStarted = _resupplyJobsStarted;
            data.Stats.ResupplyJobsCompleted = _resupplyJobsCompleted;

            data.Stats.HylandSellAttempts = _hylandSellAttempts;
            data.Stats.HylandSellSuccesses = _hylandSellSuccesses;
        }

        // ---------------- SUPPLIES ----------------

        public static bool TryConsumeSupplies(float amount)
        {
            if (amount <= 0f)
                return false;

            var p = ActiveProperty;
            float before = GetSupplies(p);
            if (before < amount)
                return false;

            float supplies = before - amount;
            SetSupplies(p, supplies);

            // Notify ONLY when supplies reach 0
            if (before > 0f && supplies <= 0f)
            {
                Agent28.NotifySuppliesEmpty();
            }

            SyncToSaveable();
            return true;
        }

        public static void AddSupplies(float amount)
        {
            if (amount <= 0f)
                return;

            var p = ActiveProperty;
            SetSupplies(p, GetSupplies(p) + amount);
            SyncToSaveable();
        }

        // ---------------- STOCK ----------------

        public static bool TryConsumeStock(float amount)
        {
            if (amount <= 0f)
                return false;

            var p = ActiveProperty;
            float before = GetStock(p);
            if (before < amount)
                return false;

            float stock = before - amount;
            SetStock(p, stock);
            SyncToSaveable();
            return true;
        }

        public static void AddStock(float amount)
        {
            if (amount <= 0f)
                return;

            var p = ActiveProperty;
            SetStock(p, GetStock(p) + amount);
            SyncToSaveable();
        }

        // ---------------- UPGRADES STATE ----------------
        // Upgrades are per-property (Garage and Bunker), independent per location.

        private static readonly Dictionary<PropertyType, bool> _equipmentOwnedByProperty = new();
        private static readonly Dictionary<PropertyType, bool> _staffOwnedByProperty = new();
        private static readonly Dictionary<PropertyType, bool> _securityOwnedByProperty = new();

        private static bool GetUpgrade(Dictionary<PropertyType, bool> dict, PropertyType p)
            => dict.TryGetValue(p, out var v) && v;

        private static void SetUpgrade(Dictionary<PropertyType, bool> dict, PropertyType p, bool v)
            => dict[p] = v;

        public static bool EquipmentUpgradeOwned
            => PropertyAllowsUpgrades(ActiveProperty) && GetUpgrade(_equipmentOwnedByProperty, ActiveProperty);

        public static bool StaffUpgradeOwned
            => PropertyAllowsUpgrades(ActiveProperty) && GetUpgrade(_staffOwnedByProperty, ActiveProperty);

        public static bool SecurityUpgradeOwned
            => PropertyAllowsUpgrades(ActiveProperty) && GetUpgrade(_securityOwnedByProperty, ActiveProperty);

        public static bool TryBuyEquipmentUpgrade()
        {
            var p = ActiveProperty;
            if (!PropertyAllowsUpgrades(p))
                return false;

            if (GetUpgrade(_equipmentOwnedByProperty, p))
                return false;

            SetUpgrade(_equipmentOwnedByProperty, p, true);
            MelonLogger.Msg("[BusinessState] Equipment upgrade purchased ({0}).", p);
            SyncToSaveable();
            return true;
        }

        public static bool TryBuyStaffUpgrade()
        {
            var p = ActiveProperty;
            if (!PropertyAllowsUpgrades(p))
                return false;

            if (GetUpgrade(_staffOwnedByProperty, p))
                return false;

            SetUpgrade(_staffOwnedByProperty, p, true);
            MelonLogger.Msg("[BusinessState] Staff upgrade purchased ({0}).", p);
            SyncToSaveable();
            return true;
        }

        public static bool TryBuySecurityUpgrade()
        {
            var p = ActiveProperty;
            if (!PropertyAllowsUpgrades(p))
                return false;

            if (GetUpgrade(_securityOwnedByProperty, p))
                return false;

            SetUpgrade(_securityOwnedByProperty, p, true);
            MelonLogger.Msg("[BusinessState] Security upgrade purchased ({0}).", p);
            SyncToSaveable();
            return true;
        }

        public static float GetValuePerUnitMultiplier()
        {
            var p = ActiveProperty;
            if (!PropertyAllowsUpgrades(p))
                return 1f;

            float mult = 1f;

            if (GetUpgrade(_equipmentOwnedByProperty, p))
                mult += BusinessConfig.EquipmentValuePerUnitBonus;

            return mult;
        }

        /// <summary>
        /// Stock produced per 1 supply.
        /// </summary>
        public static float GetStockPerSupply()
        {
            float amount = BusinessConfig.StockPerSupply;

            var p = ActiveProperty;
            if (PropertyAllowsUpgrades(p) && GetUpgrade(_equipmentOwnedByProperty, p))
                amount *= BusinessConfig.EquipmentStockPerSupplyMult;

            return amount;
        }

        public static float GetProductionSpeedMultiplier()
        {
            var p = ActiveProperty;
            if (!PropertyAllowsUpgrades(p))
                return 1f;

            float mult = 1f;

            if (GetUpgrade(_equipmentOwnedByProperty, p))
                mult += BusinessConfig.EquipmentProductionSpeedBonus;

            if (GetUpgrade(_staffOwnedByProperty, p))
                mult += BusinessConfig.StaffProductionSpeedBonus;

            return mult;
        }

        public static float GetRaidChanceMultiplier()
        {
            var p = ActiveProperty;
            if (!PropertyAllowsUpgrades(p))
                return 1f;

            float mult = 1f;

            if (GetUpgrade(_securityOwnedByProperty, p))
                mult *= BusinessConfig.SecurityRaidChanceMultiplier;

            return mult;
        }

        public static float GetEffectiveConversionInterval()
        {
            float baseInterval = GetBaseConversionInterval(ActiveProperty);

            // Warehouse uses a fixed pace; garage/bunker can be sped up by upgrades.
            float speedMult = GetProductionSpeedMultiplier();
            if (speedMult <= 0.01f)
                speedMult = 1f;

            return baseInterval / speedMult;
        }

        // ---------------- STATS ----------------

        private static float _totalEarnings;
        private static int _totalSalesCount;
        private static float _totalStockProduced;

        private static int _resupplyJobsStarted;
        private static int _resupplyJobsCompleted;

        private static int _hylandSellAttempts;
        private static int _hylandSellSuccesses;

        public static float TotalEarnings => _totalEarnings;
        public static int TotalSalesCount => _totalSalesCount;
        public static float TotalStockProduced => _totalStockProduced;

        public static int ResupplyJobsStarted => _resupplyJobsStarted;
        public static int ResupplyJobsCompleted => _resupplyJobsCompleted;

        public static int HylandSellAttempts => _hylandSellAttempts;
        public static int HylandSellSuccesses => _hylandSellSuccesses;

        public static void AddEarnings(float amount)
        {
            if (amount <= 0f)
                return;

            _totalEarnings += amount;
            SyncToSaveable();
        }

        public static void IncrementSalesCount()
        {
            _totalSalesCount++;
            SyncToSaveable();
        }

        public static void AddStockProduced(float amount)
        {
            if (amount <= 0f)
                return;

            _totalStockProduced += amount;
            SyncToSaveable();
        }

        public static void MarkResupplyStarted()
        {
            _resupplyJobsStarted++;
            SyncToSaveable();
        }

        public static void MarkResupplyCompleted()
        {
            _resupplyJobsCompleted++;
            SyncToSaveable();
        }

        public static void MarkHylandSellAttempt()
        {
            _hylandSellAttempts++;
            SyncToSaveable();
        }

        public static void MarkHylandSellSuccess()
        {
            _hylandSellSuccesses++;
            SyncToSaveable();
        }

        /// <summary>For console/debug: set stats directly.</summary>
        public static void SetTotalEarnings(float v) { _totalEarnings = Mathf.Max(0f, v); SyncToSaveable(); }
        public static void SetTotalSalesCount(int v) { _totalSalesCount = Mathf.Max(0, v); SyncToSaveable(); }
        public static void SetTotalStockProduced(float v) { _totalStockProduced = Mathf.Max(0f, v); SyncToSaveable(); }
        public static void SetResupplyJobsStarted(int v) { _resupplyJobsStarted = Mathf.Max(0, v); SyncToSaveable(); }
        public static void SetResupplyJobsCompleted(int v) { _resupplyJobsCompleted = Mathf.Max(0, v); SyncToSaveable(); }
        public static void SetHylandSellAttempts(int v) { _hylandSellAttempts = Mathf.Max(0, v); SyncToSaveable(); }
        public static void SetHylandSellSuccesses(int v) { _hylandSellSuccesses = Mathf.Max(0, v); SyncToSaveable(); }

        /// <summary>For console/debug: set property owned.</summary>
        public static void SetPropertyOwned(PropertyType p, bool owned)
        {
            var data = WSSaveData.Instance?.Data;
            if (data == null) return;
            switch (p)
            {
                case PropertyType.Warehouse: data.Properties.Warehouse.Owned = owned; break;
                case PropertyType.Garage: data.Properties.Garage.Owned = owned; break;
                case PropertyType.Bunker: data.Properties.Bunker.Owned = owned; break;
            }
            MelonLogger.Msg("[BusinessState] {0} Owned = {1}", p, owned);
        }

        // ---------------- 

        // ---------------- COMPAT / UI-EXPECTED API ----------------

        /// <summary>
        /// Adds supplies to the active property, clamped to that property's max supplies.
        /// Returns true if any supplies were added.
        /// </summary>
        public static bool TryAddSupplies(float amount)
        {
            if (amount <= 0f)
                return false;

            var p = ActiveProperty;
            float before = GetSupplies(p);
            float max = BusinessConfig.GetMaxSupplies(p);
            float after = Mathf.Clamp(before + amount, 0f, max);

            if (after <= before)
                return false;

            SetSupplies(p, after);
            SyncToSaveable();
            return true;
        }

        /// <summary>
        /// Adds stock to the active property, clamped to that property's max stock.
        /// Returns true if any stock were added.
        /// </summary>
        public static bool TryAddStock(float amount)
        {
            if (amount <= 0f)
                return false;

            var p = ActiveProperty;
            float before = GetStock(p);
            float max = BusinessConfig.GetMaxStock(p);
            float after = Mathf.Clamp(before + amount, 0f, max);

            if (after <= before)
                return false;

            SetStock(p, after);
            SyncToSaveable();
            return true;
        }

        public static float Round3(float value)
            => Mathf.Round(value * 1000f) / 1000f;

        // ----- Sell job gating -----

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

        // ----- Stats registration -----

        public static void RegisterResupplyJobStarted()
        {
            _resupplyJobsStarted++;
            SyncToSaveable();
        }

        public static void RegisterResupplyJobCompleted()
        {
            _resupplyJobsCompleted++;
            SyncToSaveable();
        }

        public static void RegisterStockProduced(float amount)
        {
            if (amount <= 0f)
                return;

            _totalStockProduced += amount;
            SyncToSaveable();
        }

        public static void RegisterSellAttempt(bool isHyland)
        {
            if (isHyland)
                _hylandSellAttempts++;

            SyncToSaveable();
        }

        public static void RegisterSale(float earnings, bool isHyland)
        {
            if (earnings < 0f)
                earnings = 0f;

            _totalEarnings += earnings;
            _totalSalesCount++;

            if (isHyland)
                _hylandSellSuccesses++;

            SyncToSaveable();
        }

        public static float GetResupplySuccessRate()
        {
            if (_resupplyJobsStarted <= 0)
                return 0f;

            return Mathf.Clamp01((float)_resupplyJobsCompleted / _resupplyJobsStarted);
        }

        public static float GetHylandSellSuccessRate()
        {
            if (_hylandSellAttempts <= 0)
                return 0f;

            return Mathf.Clamp01((float)_hylandSellSuccesses / _hylandSellAttempts);
        }
        // ---------------- LOAD HOOK ----------------

        internal static void ApplyLoadedData(WSSaveData.PersistedData data)
        {
            if (data == null)
                return;

            // ---------------- STOCK (PER PROPERTY) ----------------
            SetSupplies(PropertyType.Warehouse, data.Stock.WarehouseSupplies);
            SetStock(PropertyType.Warehouse, data.Stock.WarehouseStock);

            SetSupplies(PropertyType.Garage, data.Stock.GarageSupplies);
            SetStock(PropertyType.Garage, data.Stock.GarageStock);

            SetSupplies(PropertyType.Bunker, data.Stock.BunkerSupplies);
            SetStock(PropertyType.Bunker, data.Stock.BunkerStock);

            // ---------------- UPGRADES (PER PROPERTY) ----------------
            SetUpgrade(_equipmentOwnedByProperty, PropertyType.Garage, data.Properties.Garage.Upgrades.EquipmentOwned);
            SetUpgrade(_staffOwnedByProperty, PropertyType.Garage, data.Properties.Garage.Upgrades.StaffOwned);
            SetUpgrade(_securityOwnedByProperty, PropertyType.Garage, data.Properties.Garage.Upgrades.SecurityOwned);

            SetUpgrade(_equipmentOwnedByProperty, PropertyType.Bunker, data.Properties.Bunker.Upgrades.EquipmentOwned);
            SetUpgrade(_staffOwnedByProperty, PropertyType.Bunker, data.Properties.Bunker.Upgrades.StaffOwned);
            SetUpgrade(_securityOwnedByProperty, PropertyType.Bunker, data.Properties.Bunker.Upgrades.SecurityOwned);

            // ---------------- STATS ----------------
            _totalEarnings = data.Stats.TotalEarnings;
            _totalSalesCount = data.Stats.TotalSalesCount;
            _totalStockProduced = data.Stats.TotalStockProduced;

            _resupplyJobsStarted = data.Stats.ResupplyJobsStarted;
            _resupplyJobsCompleted = data.Stats.ResupplyJobsCompleted;

            _hylandSellAttempts = data.Stats.HylandSellAttempts;
            _hylandSellSuccesses = data.Stats.HylandSellSuccesses;

            MelonLogger.Msg(
                "[BusinessState] Loaded: WH({0}/{1}) GAR({2}/{3}) BUN({4}/{5}) earnings={6}",
                GetSupplies(PropertyType.Warehouse), GetStock(PropertyType.Warehouse),
                GetSupplies(PropertyType.Garage), GetStock(PropertyType.Garage),
                GetSupplies(PropertyType.Bunker), GetStock(PropertyType.Bunker),
                _totalEarnings
            );
        }
    }
}
