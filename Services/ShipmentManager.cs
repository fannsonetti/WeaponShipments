using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace WeaponShipments.Services
{
    /// <summary>
    /// Manages 7 permanent weapon shipment slots with per-weapon cooldowns.
    /// </summary>
    public class ShipmentManager
    {
        public class ShipmentEntry
        {
            public string Id;
            public string GunType;
            public string Origin;
            public string Destination;
            public string ProductForm;
            public string Status;   // Pending / In Progress / Completed / Cooldown
            public bool Delivered;
            public DateTime Updated;
            public int Quantity;    // 1–3
        }

        // 5-minute cooldown per weapon after finishing
        private const float PerGunCooldownSeconds = 30f;

        // Weapon list (one slot per weapon)
        private static readonly string[] GunTypes =
        {
            "Shipment"
        };

        // Origins (Black Market excluded from steal pool)
        private static readonly string[] Origins =
        {
            "RV",
            "Gazebo",
            "Sewer Market"
        };

        // Destinations
        private static readonly string[] Destinations =
        {
            "Handy Hank's Hardware",
            "Dan's Hardware",
            "North Warehouse",
            "West Gas-mart",
            "Central Gas-mart"
        };

        // Product forms
        private static readonly string[] ProductForms =
        {
            "Crate",
            "Van"
        };

        // -----------------------------
        //   SINGLETON
        // -----------------------------

        private static ShipmentManager _instance;
        public static ShipmentManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ShipmentManager();
                return _instance;
            }
        }

        // -----------------------------
        //   INTERNAL STATE
        // -----------------------------

        private readonly List<ShipmentEntry> _shipments = new List<ShipmentEntry>();

        // When each gun type is allowed again
        private readonly Dictionary<string, DateTime> _gunCooldownUntil =
            new Dictionary<string, DateTime>();

        private bool _initialised = false;

        // -----------------------------
        //   PUBLIC API
        // -----------------------------

        /// <summary>
        /// Returns all 7 permanent slots.
        /// </summary>
        public IReadOnlyList<ShipmentEntry> GetAllShipments()
        {
            EnsureInitialSlots();
            return _shipments;
        }

        public ShipmentEntry GetShipment(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return _shipments.Find(s => s.Id == id);
        }
        public bool HasActiveInProgressShipment()
        {
            foreach (var s in _shipments)
            {
                if (s.Status == "In Progress" && !s.Delivered)
                    return true;
            }

            return false;
        }
        public bool AcceptShipment(string id)
        {
            var shipment = GetShipment(id);
            if (shipment == null)
                return false;

            // Only one active job
            if (HasActiveInProgressShipment())
                return false;

            // On cooldown?
            if (_gunCooldownUntil.TryGetValue(shipment.GunType, out var until) &&
                until > DateTime.UtcNow)
            {
                return false;
            }

            // Cooldown expired but status not updated yet
            if (shipment.Status == "Cooldown")
                shipment.Status = "Pending";

            shipment.Status = "In Progress";
            shipment.Delivered = false;
            shipment.Updated = DateTime.Now;

            MelonLogger.Msg($"[ShipmentManager] Accepted: {shipment.GunType}");

            return true;
        }

        /// <summary>
        /// Called when crate enters delivery zone: marks as Completed.
        /// </summary>
        public void DeliverShipment(string id)
        {
            var shipment = GetShipment(id);
            if (shipment == null)
                return;

            shipment.Delivered = true;
            shipment.Status = "Completed";
            shipment.Updated = DateTime.Now;

            MelonLogger.Msg($"[ShipmentManager] Delivered: {shipment.GunType}");
        }

        /// <summary>
        /// Called when the player presses "Finish" in the phone app.
        /// Starts cooldown instead of removing the row.
        /// </summary>
        public void RemoveShipment(string id)
        {
            var shipment = GetShipment(id);
            if (shipment == null)
                return;

            // Start cooldown for this weapon type
            _gunCooldownUntil[shipment.GunType] =
                DateTime.UtcNow.AddSeconds(PerGunCooldownSeconds);

            // Put slot into cooldown state
            shipment.Status = "Cooldown";
            shipment.Delivered = false;
            shipment.Updated = DateTime.Now;

            // Pre-roll the next route for after cooldown
            shipment.Origin = Origins[UnityEngine.Random.Range(0, Origins.Length)];
            shipment.Destination = Destinations[UnityEngine.Random.Range(0, Destinations.Length)];
            shipment.ProductForm = ProductForms[UnityEngine.Random.Range(0, ProductForms.Length)];
            shipment.Quantity = 1;

            MelonLogger.Msg($"[ShipmentManager] {shipment.GunType} enters cooldown.");
        }

        /// <summary>
        /// For UI: returns remaining cooldown for a shipment's weapon type, if any.
        /// If cooldown finishes, status is reset to Pending.
        /// </summary>
        public bool TryGetCooldownRemaining(string shipmentId, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;

            var shipment = GetShipment(shipmentId);
            if (shipment == null)
                return false;

            if (!_gunCooldownUntil.TryGetValue(shipment.GunType, out var until))
                return false;

            var now = DateTime.UtcNow;

            // Cooldown done? reset
            if (until <= now)
            {
                _gunCooldownUntil.Remove(shipment.GunType);

                if (shipment.Status == "Cooldown")
                {
                    shipment.Status = "Pending";
                    shipment.Updated = DateTime.Now;
                }

                return false;
            }

            remaining = until - now;
            return true;
        }

        // -----------------------------
        //   INITIAL SLOTS
        // -----------------------------

        private void EnsureInitialSlots()
        {
            if (_initialised)
                return;

            _shipments.Clear();

            // Exactly one slot per weapon
            for (int i = 0; i < GunTypes.Length; i++)
            {
                ShipmentEntry entry = new ShipmentEntry
                {
                    Id = Guid.NewGuid().ToString("N"),
                    GunType = GunTypes[i],
                    Origin = Origins[UnityEngine.Random.Range(0, Origins.Length)],
                    Destination = Destinations[UnityEngine.Random.Range(0, Destinations.Length)],
                    ProductForm = ProductForms[UnityEngine.Random.Range(0, ProductForms.Length)],
                    Status = "Pending",
                    Delivered = false,
                    Updated = DateTime.Now,
                    Quantity = 1
                };

                _shipments.Add(entry);
            }

            _initialised = true;
            MelonLogger.Msg("[ShipmentManager] Initialized 7 permanent shipment slots.");
        }

        /// <summary>
        /// Debug: Clears all shipments and cooldowns so it can re-init.
        /// </summary>
        public void ClearAll()
        {
            _shipments.Clear();
            _gunCooldownUntil.Clear();
            _initialised = false;

            MelonLogger.Msg("[ShipmentManager] All shipments cleared.");
        }
    }
}
