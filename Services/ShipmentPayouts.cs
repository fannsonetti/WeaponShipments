using System.Collections.Generic;

namespace WeaponShipments.Services
{
    public static class ShipmentPayouts
    {
        private static readonly Dictionary<string, float> payouts =
            new Dictionary<string, float>
            {
                { "Baseball Bat", 20f },
                { "Machete", 100f },
                { "Revolver", 400f },
                { "M1911", 1000f },
                { "Pump Shotgun", 3000f },
                { "AK-47", 5000f },
                { "Minigun", 25000f },
            };

        public static float GetReward(string gunType)
        {
            if (payouts.TryGetValue(gunType, out float value))
                return value;

            return 0f; // fallback
        }
    }
}
