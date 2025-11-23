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
                // BEFORE:
                // yield return new WaitForSeconds(BusinessConfig.ConversionInterval);

                // AFTER: faster with upgrades
                yield return new WaitForSeconds(BusinessState.GetEffectiveConversionInterval());
                ConvertOneSupply();
            }
        }

        private static void ConvertOneSupply()
        {
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

