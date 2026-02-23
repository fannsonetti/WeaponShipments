using System;

namespace WeaponShipments.Saveables
{
    [Serializable]
    public class SavedStats
    {
        public float TotalEarnings = 0f;
        public int TotalSalesCount = 0;
        public float TotalStockProduced = 0f;

        public int ResupplyJobsStarted = 0;
        public int ResupplyJobsCompleted = 0;

        public int HylandSellAttempts = 0;
        public int HylandSellSuccesses = 0;
    }
}
