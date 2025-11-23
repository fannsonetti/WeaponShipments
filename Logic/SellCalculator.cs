using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.Logic
{
    public static class SellCalculator
    {
        public static float HylandPayout
        {
            get
            {
                float stock = BusinessState.Stock;
                float valueMult = BusinessState.GetValuePerUnitMultiplier();

                float rawPayout = stock * BusinessConfig.PriceHyland * valueMult;

                return Mathf.Round(rawPayout);
            }
        }
        public static float SerenaPayout
        {
            get
            {
                float stock = BusinessState.Stock;
                float valueMult = BusinessState.GetValuePerUnitMultiplier();

                float rawPayout = stock * BusinessConfig.PriceSerena * valueMult;

                return Mathf.Round(rawPayout);
            }
        }
    }
}
