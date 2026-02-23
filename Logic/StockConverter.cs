using MelonLoader;
using System.Collections;
using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Logic
{
    public static class StockConverter
    {
        private static bool _running;

        public static void Start()
        {
            if (_running) return;
            _running = true;

            MelonCoroutines.Start(ConversionLoop());
        }

        private static IEnumerator ConversionLoop()
        {
            while (true)
            {
                // Property-aware interval (Warehouse uses 240s via BusinessState/BusinessConfig routing,
                // Bunker/Garage use pref interval and (for Bunker only) upgrades affect speed).
                yield return new WaitForSeconds(BusinessState.GetEffectiveConversionInterval());

                ConvertOneSupply();
            }
        }

        private static void ConvertOneSupply()
        {
            // Weapon manufacturing only runs after Quest 2 completes (deliver truck to warehouse).
            var data = WSSaveData.Instance?.Data;
            if (data == null || !data.Properties.Warehouse.SetupComplete)
                return;

            // Note: BusinessState routes Supplies/Stock to the currently active property storage.
            // Caps are enforced inside BusinessState (and/or via BusinessConfig.GetMax* calls there).
            float suppliesCost = 1f;

            if (!BusinessState.TryConsumeSupplies(suppliesCost))
                return;

            float perSupply = BusinessState.GetStockPerSupply();
            float totalStock = perSupply;

            if (!BusinessState.TryAddStock(totalStock))
                return;

            BusinessState.RegisterStockProduced(totalStock);
        }
    }
}
