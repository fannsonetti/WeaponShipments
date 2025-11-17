using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine; // Random + WaitForSeconds

namespace WeaponShipments.Services
{
    /// <summary>
    /// Manages randomly generated weapon shipments.
    /// - Max 3 shipments in the list at a time (including Completed until removed).
    /// - Adds one new shipment every 5 minutes when there is space.
    /// - Only 1 shipment can be "In Progress" at a time.
    /// - Completed shipments stay in the list until the app "Finishes" them.
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
            public string Status;   // Pending / In Progress / Completed
            public bool Delivered;
            public DateTime Updated;

            // 🔹 NEW: how many items in this shipment (1–3)
            public int Quantity;
        }

        private const int MaxActiveShipments = 3;
        private const float OfferIntervalSeconds = 5f * 60f; // 5 minutes

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

        private readonly List<ShipmentEntry> _shipments = new List<ShipmentEntry>();

        private bool _offerRoutineStarted;
        private bool _initialised;

        // For UI timer
        private DateTime? _nextOfferTimeUtc;

        // Weapon types
        private static readonly string[] GunTypes =
        {
            "Baseball Bat",
            "Machete",
            "Revolver",
            "M1911",
            "Pump Shotgun",
            "AK-47",
            "Minigun"
        };

        // Origins
        private static readonly string[] Origins =
        {
            "RV",
            "Gazebo",
            "Sewer Market",
            "Black Market"
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

        /// <summary>
        /// Returns the active list of shipments (Pending / In Progress / Completed).
        /// Completed ones remain here until removed by the app.
        /// </summary>
        public IReadOnlyList<ShipmentEntry> GetAllShipments()
        {
            EnsureInitialOffers();
            return _shipments;
        }

        public ShipmentEntry GetShipment(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return _shipments.Find(s => s.Id == id);
        }

        /// <summary>
        /// True if a shipment is "In Progress" and not yet delivered.
        /// (You only allow 1 active at a time.)
        /// </summary>
        public bool HasActiveInProgressShipment()
        {
            foreach (var s in _shipments)
            {
                if (s.Status == "In Progress" && !s.Delivered)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Accepts a shipment if no other "In Progress" shipment exists.
        /// Sets status to "In Progress".
        /// </summary>
        public bool AcceptShipment(string id)
        {
            if (HasActiveInProgressShipment())
            {
                MelonLogger.Warning("[ShipmentManager] Cannot accept: another shipment is already In Progress.");
                return false;
            }

            var shipment = GetShipment(id);
            if (shipment == null)
            {
                MelonLogger.Warning("[ShipmentManager] AcceptShipment: shipment not found: " + id);
                return false;
            }

            if (shipment.Delivered)
            {
                MelonLogger.Warning("[ShipmentManager] AcceptShipment: shipment already delivered: " + id);
                return false;
            }

            shipment.Status = "In Progress";
            shipment.Delivered = false;
            shipment.Updated = DateTime.Now;

            MelonLogger.Msg($"[ShipmentManager] Accepted shipment {id} (In Progress)");
            return true;
        }

        /// <summary>
        /// Called by the delivery area when the crate enters.
        /// Marks it delivered and sets Status = "Completed".
        /// Does NOT remove it from the list.
        /// </summary>
        public void DeliverShipment(string id)
        {
            var shipment = GetShipment(id);
            if (shipment == null)
                return;

            shipment.Delivered = true;
            shipment.Status = "Completed";
            shipment.Updated = DateTime.Now;

            MelonLogger.Msg($"[ShipmentManager] Delivered shipment {id} (Completed)");
        }

        /// <summary>
        /// Called when the player presses the "Finish" button in the app.
        /// Removes the shipment from the list.
        /// </summary>
        public void RemoveShipment(string id)
        {
            var shipment = GetShipment(id);
            if (shipment == null)
                return;

            _shipments.Remove(shipment);
            MelonLogger.Msg($"[ShipmentManager] Removed shipment {id} from list.");
        }

        /// <summary>
        /// Clears all shipments and resets initialization.
        /// </summary>
        public void ClearAll()
        {
            _shipments.Clear();
            _initialised = false;
            _nextOfferTimeUtc = null;
        }

        /// <summary>
        /// Ensures the starting list has up to MaxActiveShipments (3) offers.
        /// </summary>
        public void EnsureInitialOffers()
        {
            if (_initialised)
                return;

            while (_shipments.Count < MaxActiveShipments)
            {
                _shipments.Add(GenerateRandomShipment());
            }

            _initialised = true;
        }

        /// <summary>
        /// Starts the 5-minute offer loop (only once).
        /// </summary>
        public void StartOfferRoutine()
        {
            if (_offerRoutineStarted)
                return;

            _offerRoutineStarted = true;
            MelonCoroutines.Start(OfferRoutine());
        }

        private IEnumerator OfferRoutine()
        {
            while (true)
            {
                // If full, just wait, no next-offer ETA
                if (_shipments.Count >= MaxActiveShipments)
                {
                    _nextOfferTimeUtc = null;
                    yield return new WaitForSeconds(OfferIntervalSeconds);
                    continue;
                }

                // We have room; schedule next offer
                _nextOfferTimeUtc = DateTime.UtcNow.AddSeconds(OfferIntervalSeconds);
                yield return new WaitForSeconds(OfferIntervalSeconds);

                // Only spawn if we still have room
                if (_shipments.Count < MaxActiveShipments)
                {
                    var newOffer = GenerateRandomShipment();
                    _shipments.Add(newOffer);
                    MelonLogger.Msg($"[ShipmentManager] New timed offer: {newOffer.GunType} ({newOffer.ProductForm})");
                }
            }
        }

        /// <summary>
        /// For the UI timer.
        /// Returns true if we have something to show.
        /// If offersFull == true, remaining is ignored.
        /// </summary>
        public bool TryGetTimeToNextOffer(out TimeSpan remaining, out bool offersFull)
        {
            if (_shipments.Count >= MaxActiveShipments)
            {
                offersFull = true;
                remaining = TimeSpan.Zero;
                return true;
            }

            offersFull = false;

            if (!_nextOfferTimeUtc.HasValue)
            {
                remaining = TimeSpan.Zero;
                return false;
            }

            remaining = _nextOfferTimeUtc.Value - DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// Uses UnityEngine.Random so each startup produces different results.
        /// </summary>
        private ShipmentEntry GenerateRandomShipment()
        {
            var s = new ShipmentEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                GunType = GunTypes[UnityEngine.Random.Range(0, GunTypes.Length)],
                Origin = Origins[UnityEngine.Random.Range(0, Origins.Length)],
                Destination = Destinations[UnityEngine.Random.Range(0, Destinations.Length)],
                ProductForm = ProductForms[UnityEngine.Random.Range(0, ProductForms.Length)],
                Status = "Pending",
                Delivered = false,
                Updated = DateTime.Now,
                Quantity = UnityEngine.Random.Range(1, 4)   // 🔹 1, 2, or 3
            };

            return s;
        }
    }
}
