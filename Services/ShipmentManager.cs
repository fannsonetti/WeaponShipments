using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine; // Needed for Random and WaitForSeconds

namespace WeaponShipments.Services
{
    /// <summary>
    /// Manages randomly generated weapon shipments.
    /// - Max 3 active shipments.
    /// - Adds one new shipment every 5 minutes.
    /// - Only 1 shipment can be accepted at a time.
    /// - Accepted shipments stay visible until delivered.
    /// </summary>
    public class ShipmentManager
    {
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

        public class ShipmentEntry
        {
            public string Id;
            public string GunType;
            public string Origin;
            public string Destination;
            public string ProductForm; // Crate or Van
            public string Status;      // Pending, Accepted, Delivered
            public bool Delivered;
            public DateTime Updated;
        }

        private readonly List<ShipmentEntry> _shipments = new List<ShipmentEntry>();

        // Weapon Types
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

        // Product Forms
        private static readonly string[] ProductForms =
        {
            "Crate",
            "Van"
        };

        private bool _offerRoutineStarted;
        private bool _initialised;

        /// <summary>
        /// Returns the active shipments (Pending or Accepted).
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
        /// True if an accepted (but not yet delivered) shipment exists.
        /// </summary>
        public bool HasActiveAcceptedShipment()
        {
            foreach (var s in _shipments)
            {
                if (s.Status == "Accepted" && !s.Delivered)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Accepts a shipment if no other accepted shipment exists.
        /// </summary>
        public bool AcceptShipment(string id)
        {
            if (HasActiveAcceptedShipment())
            {
                MelonLogger.Warning("[ShipmentManager] Cannot accept: another shipment is active.");
                return false;
            }

            var shipment = GetShipment(id);
            if (shipment == null) return false;

            shipment.Status = "Accepted";
            shipment.Delivered = false;
            shipment.Updated = DateTime.Now;

            MelonLogger.Msg($"[ShipmentManager] Accepted shipment {id}");
            return true;
        }

        /// <summary>
        /// Marks the shipment as delivered and removes it from the list.
        /// </summary>
        public void DeliverShipment(string id)
        {
            var shipment = GetShipment(id);
            if (shipment == null) return;

            shipment.Delivered = true;
            shipment.Status = "Delivered";
            shipment.Updated = DateTime.Now;

            _shipments.Remove(shipment);

            MelonLogger.Msg($"[ShipmentManager] Delivered shipment {id}");
        }

        /// <summary>
        /// Clears all shipments.
        /// </summary>
        public void ClearAll()
        {
            _shipments.Clear();
            _initialised = false;
        }

        /// <summary>
        /// Ensures starting list has up to 3 offers.
        /// </summary>
        public void EnsureInitialOffers()
        {
            if (_initialised) return;

            while (_shipments.Count < MaxActiveShipments)
                _shipments.Add(GenerateRandomShipment());

            _initialised = true;
        }

        public void StartOfferRoutine()
        {
            if (_offerRoutineStarted) return;

            _offerRoutineStarted = true;
            MelonCoroutines.Start(OfferRoutine());
        }

        private IEnumerator OfferRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(OfferIntervalSeconds);

                if (_shipments.Count < MaxActiveShipments)
                {
                    var newOffer = GenerateRandomShipment();
                    _shipments.Add(newOffer);
                    MelonLogger.Msg($"[ShipmentManager] New timed offer: {newOffer.GunType} ({newOffer.ProductForm})");
                }
            }
        }

        /// <summary>
        /// Uses UnityEngine.Random so each startup produces different results.
        /// </summary>
        private ShipmentEntry GenerateRandomShipment()
        {
            var entry = new ShipmentEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                GunType = GunTypes[UnityEngine.Random.Range(0, GunTypes.Length)],
                Origin = Origins[UnityEngine.Random.Range(0, Origins.Length)],
                Destination = Destinations[UnityEngine.Random.Range(0, Destinations.Length)],
                ProductForm = ProductForms[UnityEngine.Random.Range(0, ProductForms.Length)],
                Status = "Pending",
                Delivered = false,
                Updated = DateTime.Now
            };

            MelonLogger.Msg($"[ShipmentManager] Generated: {entry.GunType} ({entry.ProductForm}) from {entry.Origin} → {entry.Destination}");

            return entry;
        }
    }
}
