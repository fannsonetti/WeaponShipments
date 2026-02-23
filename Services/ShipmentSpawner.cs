// Full ShipmentSpawner.cs with Coupe vehicle detection and renaming
// Supports old S1API (no VehicleObject, no ActiveVehicles)
// Delivery minigame uses a vehicle identified by renaming the spawned GameObject.

using MelonLoader;
using S1API.Money;
using S1API.Vehicles;
using System.Collections.Generic;
using UnityEngine;
using WeaponShipments.Data;
using WeaponShipments.NPCs;
using WeaponShipments.UI;
using Object = UnityEngine.Object;

namespace WeaponShipments.Services
{
    public static class ShipmentSpawner
    {
        private static readonly Vector3 FallbackDeliveryPosition = new Vector3(-17.2277f, -2.6895f, 173.3186f);
        private static readonly Vector3 DefaultDeliverySize = new Vector3(3f, 3f, 3f);
        private static readonly Quaternion DefaultRotation = Quaternion.identity;

        private static GameObject _templateCrate;

        private static bool _sellJobActive;
        private static bool _sellJobUsedWarehouseVeeper;
        private static string _sellVehicleGuid;
        private static GameObject _sellVehicleObject;
        private static GameObject _sellArea;
        private static GameObject _sellCrateObject;
        private static string _sellVehiclePrefabName;

        private static readonly Vector3 SellSpawnPosition = new Vector3(40f, 0.8f, 74f);
        private static readonly Quaternion SellSpawnRotation = Quaternion.Euler(0f, 270f, 0f);

        private static readonly Vector3 SellDeliveryPosition =
            new Vector3(-18.1277f, -4.1731f, 173.5805f);
        private static readonly Vector3 SellDeliverySize =
            new Vector3(9f, 3f, 8f);
        private static readonly Quaternion SellDeliveryRotation =
            Quaternion.Euler(0f, 0f, 0f);

        private static readonly Vector3 SellDeliveryPosition_Chemical =
            new Vector3(-110.4244f, -2.7369f, 97.7555f);

        private static readonly Vector3 SellDeliverySize_Chemical =
            new Vector3(3.5f, 3f, 5f);

        private static readonly Quaternion SellDeliveryRotation_Chemical =
            Quaternion.Euler(0f, 0f, 0f);

        // Black Market as a delivery-only dropoff
        private static readonly Vector3 SellDeliveryPosition_BlackMarket =
            new Vector3(-60.9603f, -0.9796f, 35.7872f);

        private static readonly Vector3 SellDeliverySize_BlackMarket =
            new Vector3(4f, 3f, 2.5f);

        private static readonly Quaternion SellDeliveryRotation_BlackMarket =
            Quaternion.Euler(0f, 0f, 0f);

        private const string SellVehicleCode = "Cheetah"; // API vehicle code
        private const string SellVehiclePrefabName = "Coupe"; // actual ingame prefab name

        private static readonly Vector3 FallbackSpawnPosition = new Vector3(0f, 0f, 0f);
        private static readonly Quaternion FallbackSpawnRotation = Quaternion.identity;

        private struct SpawnPoint
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public SpawnPoint(Vector3 pos, Quaternion rot)
            {
                Position = pos;
                Rotation = rot;
            }
        }

        private static readonly Dictionary<string, SpawnPoint> OriginSpawnPoints = new Dictionary<string, SpawnPoint>
        {
            {"RV", new SpawnPoint(new Vector3(17f,1.5f,-79f), Quaternion.Euler(0f,180f,0f))},
            {"Gazebo", new SpawnPoint(new Vector3(83f,6f,-125f), Quaternion.Euler(0f,90f,0f))},
            {"Sewer Market", new SpawnPoint(new Vector3(72.75f,-4.5f,34.65f), Quaternion.Euler(0f,65f,0f))},
            {"Black Market", new SpawnPoint(new Vector3(-63.8475f,-1.135f,23.1473f), Quaternion.Euler(0f,0f,0f))},
        };

        private static SpawnPoint GetSpawnPointForOrigin(string origin)
        {
            if (!string.IsNullOrEmpty(origin) && OriginSpawnPoints.TryGetValue(origin, out var sp))
                return sp;
            return new SpawnPoint(FallbackSpawnPosition, FallbackSpawnRotation);
        }

        private struct SellSpawnPoint
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public string Label;

            public SellSpawnPoint(Vector3 pos, Quaternion rot, string label)
            {
                Position = pos;
                Rotation = rot;
                Label = label;
            }
        }

        private static readonly SellSpawnPoint[] SellSpawnPoints =
        {
            new SellSpawnPoint(
                new Vector3(40f, 0.8f, 74f),
                Quaternion.Euler(0f, 270f, 0f),
                "next to the Crimson Canary"
            ),

            new SellSpawnPoint(
                new Vector3(73.2514f, 0.85f, 33.9687f),
                Quaternion.Euler(0f, 90f, 0f),
                "behind the bank"
            ),

            new SellSpawnPoint(
                new Vector3(124.5079f, 0.85f, 31.2666f),
                Quaternion.Euler(0f, 180f, 0f),
                "behind Handy Hank's"
            ),

            new SellSpawnPoint(
                new Vector3(25.2742f, 1.1f, -79.7621f),
                Quaternion.Euler(9.06f, 290f, 10f),
                "near the RV"
            ),

            new SellSpawnPoint(
                new Vector3(158.1861f, 4.8f, -110.7269f),
                Quaternion.Euler(0f, 0f, 0f),
                "near the Manor"
            ),
        };

        // A single possible sell dropoff location
        private struct SellDropoff
        {
            public Vector3 Position;
            public Vector3 Size;
            public Quaternion Rotation;
            public string Label;

            public SellDropoff(Vector3 pos, Vector3 size, Quaternion rot, string label)
            {
                Position = pos;
                Size = size;
                Rotation = rot;
                Label = label;
            }
        }

        // All possible sell job dropoffs (randomly chosen per job)
        private static readonly SellDropoff[] SellDropoffs =
        {
            new SellDropoff(
                SellDeliveryPosition,
                SellDeliverySize,
                SellDeliveryRotation,
                "the North Warehouse"
            ),
            new SellDropoff(
                SellDeliveryPosition_Chemical,
                SellDeliverySize_Chemical,
                SellDeliveryRotation_Chemical,
                "the Chemical Company"
            ),
            new SellDropoff(
                SellDeliveryPosition_BlackMarket,
                SellDeliverySize_BlackMarket,
                SellDeliveryRotation_BlackMarket,
                "the Black Market"
            ),
        };

        public static void SpawnShipmentCrate(ShipmentManager.ShipmentEntry shipment)
        {
            if (shipment == null) return;

            if (_templateCrate == null)
            {
                _templateCrate = GameObject.Find("Wood Crate Prop");
                if (_templateCrate == null)
                {
                    MelonLogger.Error("[ShipmentSpawner] Missing Wood Crate Prop");
                    return;
                }
            }

            var clone = Object.Instantiate(_templateCrate);
            clone.name = "WeaponShipment";

            var spawn = GetSpawnPointForOrigin(shipment.Origin);
            clone.transform.position = spawn.Position;
            clone.transform.rotation = spawn.Rotation;

            RenameChildCubeToWeaponShipment(clone.transform);
            ApplyQuantityScale(clone.transform, shipment.Quantity);

            // -------------------------------
            //  ADD PROXIMITY DETECTOR HERE
            // -------------------------------
            var detector = clone.AddComponent<ShipmentProximityDetector>();
            detector.Init(shipment.Id, shipment.Destination);
        }

        private static void RenameChildCubeToWeaponShipment(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Cube") t.name = "WeaponShipment";
            }
        }

        private static void ApplyQuantityScale(Transform root, int quantity)
        {
            float scale = quantity switch { 1 => 0.8f, 2 => 0.9f, _ => 1.2f };
            root.localScale = new Vector3(scale, scale, scale);
        }

        private struct DeliveryZone
        {
            public Vector3 Position;
            public Vector3 Size;
            public Quaternion Rotation;
            public DeliveryZone(Vector3 p, Vector3 s, Quaternion r)
            { Position = p; Size = s; Rotation = r; }
        }

        private static readonly Dictionary<string, DeliveryZone> DestinationZones = new Dictionary<string, DeliveryZone>
        {
            {"Handy Hank's Hardware", new DeliveryZone(new Vector3(106.498f,1.805f,31.7038f), new Vector3(1.5f,1.5f,3.8f), Quaternion.Euler(0f,0f,0f))},
            {"Dan's Hardware", new DeliveryZone(new Vector3(-16.9721f,-2.2155f,135.1911f), new Vector3(1.5f,1.5f,3.5f), Quaternion.Euler(0f,90f,0f))},
            {"North Warehouse", new DeliveryZone(new Vector3(-21f,-3.1f,173.55f), new Vector3(3f,3.6f,3f), Quaternion.Euler(0f,0f,0f))},
            {"West Gas-mart", new DeliveryZone(new Vector3(-107.85f,-2.2265f,66.17f), new Vector3(1.5f,1.5f,3.5f), Quaternion.Euler(0f,90f,0f))},
            {"Central Gas-mart", new DeliveryZone(new Vector3(20.95f,1.7485f,0.165f), new Vector3(1.5f,1.5f,3.5f), Quaternion.Euler(0f,340f,0f))},
        };

        private static DeliveryZone GetZoneForDestination(string destination)
        {
            if (!string.IsNullOrEmpty(destination) && DestinationZones.TryGetValue(destination, out var z))
                return z;
            return new DeliveryZone(FallbackDeliveryPosition, DefaultDeliverySize, DefaultRotation);
        }

        private static void SpawnSellVehicleJob(
            float stockAmount,
            float payout,
            string vehicleCode,
            string prefabName,
            Vector3 spawnPosition,
            Quaternion spawnRotation
        )
        {
            _sellVehiclePrefabName = prefabName;

            var vehicle = VehicleRegistry.CreateVehicle(vehicleCode);
            if (vehicle == null)
            {
                MelonLogger.Error("[ShipmentSpawner] Failed to create delivery vehicle with code: " + vehicleCode);
                _sellJobActive = false;
                return;
            }

            vehicle.Color = VehicleColor.Black;
            vehicle.IsPlayerOwned = true; // drive it during the job

            vehicle.Spawn(spawnPosition, spawnRotation);

            // Find the spawned vehicle closest to this spawn point and treat that as the delivery car
            _sellVehicleObject = null;
            float bestDistSq = float.MaxValue;

            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (!go.name.Contains(prefabName))
                    continue;

                GameObject root = go.transform.root != null ? go.transform.root.gameObject : go;
                float distSq = (root.transform.position - spawnPosition).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    _sellVehicleObject = root;
                }
            }

            if (_sellVehicleObject != null)
            {
                _sellVehicleObject.name = "Delivery" + prefabName; // e.g. DeliveryShitbox, DeliverySUV
                MelonLogger.Msg("[ShipmentSpawner] Marked delivery vehicle root: " + _sellVehicleObject.name);
            }
            else
            {
                MelonLogger.Warning("[ShipmentSpawner] Could not find spawned " + prefabName + " to mark as delivery car.");
            }

            MelonLogger.Msg(
                "[ShipmentSpawner] Sell job started with vehicle {0} ({1}) for {2} stock -> ${3:N0}.",
                vehicleCode,
                prefabName,
                stockAmount,
                payout
            );
        }

        private static void SpawnSellCrateJob(float stockAmount, float payout, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            // Reuse the Wood Crate Prop like other shipments
            if (_templateCrate == null)
            {
                _templateCrate = GameObject.Find("Wood Crate Prop");
                if (_templateCrate == null)
                {
                    MelonLogger.Error("[ShipmentSpawner] Sell crate job: Wood Crate Prop not found.");
                    _sellJobActive = false;
                    return;
                }
            }

            var clone = Object.Instantiate(_templateCrate);
            clone.name = "SellShipment";
            clone.transform.position = spawnPosition;
            clone.transform.rotation = spawnRotation;

            // Just reuse the scale logic if you like
            ApplyQuantityScale(clone.transform, 1);

            _sellCrateObject = clone;

            MelonLogger.Msg(
                "[ShipmentSpawner] Sell job started with crate for {0} stock -> ${1:N0}.",
                stockAmount,
                payout
            );
        }

        // Creates the sell delivery trigger + visible lime cube,
        // and returns the chosen dropoff name for Agent 28.
        private static string SpawnSellDeliveryArea(float stockAmount, float payout)
        {
            var go = new GameObject("SellDeliveryArea");

            // Random dropoff (North Warehouse / Chemical Company / Black Market)
            int idx = UnityEngine.Random.Range(0, SellDropoffs.Length);
            SellDropoff d = SellDropoffs[idx];

            go.transform.position = d.Position;
            go.transform.rotation = d.Rotation;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = d.Size;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            go.AddComponent<SellDeliveryAreaTrigger>().Init(stockAmount, payout);

            // Visible helper, same look as shipment delivery zones
            CreateLimeGreenCube(go.transform, d.Size);

            return d.Label;
        }

        public static void SpawnDeliveryArea(string shipmentId)
        {
            if (string.IsNullOrEmpty(shipmentId)) return;

            string areaName = $"ShipmentDeliveryArea_{shipmentId}";
            if (GameObject.Find(areaName) != null) return;

            var shipment = ShipmentManager.Instance.GetShipment(shipmentId);
            if (shipment == null) return;

            var zone = GetZoneForDestination(shipment.Destination);

            var go = new GameObject(areaName);
            go.transform.position = zone.Position;
            go.transform.rotation = zone.Rotation;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = zone.Size;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            go.AddComponent<DeliveryAreaTrigger>().Init(shipmentId);

            CreateLimeGreenCube(go.transform, zone.Size);
        }

        public static void SpawnSellJob(float stockAmount, float payout)
        {
            if (_sellJobActive)
            {
                MelonLogger.Warning("[ShipmentSpawner] Tried to start sell job while one is active.");
                return;
            }

            if (stockAmount <= 0f || payout <= 0f)
            {
                MelonLogger.Warning("[ShipmentSpawner] SpawnSellJob called with invalid stock/payout.");
                return;
            }

            _sellJobActive = true;
            _sellJobUsedWarehouseVeeper = false;
            _sellVehicleObject = null;
            _sellCrateObject = null;
            _sellVehiclePrefabName = null;

            // Pick a random sell spawn
            int idx = UnityEngine.Random.Range(0, SellSpawnPoints.Length);
            SellSpawnPoint sp = SellSpawnPoints[idx];

            bool isVehicleJob;

            var data = WSSaveData.Instance?.Data;
            bool useWarehouseVeeper = data != null && data.Properties.Warehouse.SetupComplete;

            if (stockAmount <= 5.26f)
            {
                isVehicleJob = false;
                SpawnSellCrateJob(stockAmount, payout, sp.Position, sp.Rotation);
            }
            else if (useWarehouseVeeper && WarehouseVeeperManager.GetWarehouseVeeper() != null)
            {
                isVehicleJob = true;
                _sellJobUsedWarehouseVeeper = true;
                WarehouseVeeperManager.PrepareForSellJob(sp.Position, sp.Rotation);
                var wv = WarehouseVeeperManager.GetWarehouseVeeper();
                _sellVehicleObject = wv != null && wv.transform.root != null ? wv.transform.root.gameObject : wv;
                _sellVehiclePrefabName = "Van";
                MelonLogger.Msg("[ShipmentSpawner] Sell job using warehouse Veeper for {0} stock -> ${1:N0}.", stockAmount, payout);
            }
            else
            {
                isVehicleJob = true;
                SpawnSellVehicleJob(stockAmount, payout, vehicleCode: "Veeper", prefabName: "Van", spawnPosition: sp.Position, spawnRotation: sp.Rotation);
            }

            // Create delivery area and get the selected dropoff name
            string chosenDropoff = SpawnSellDeliveryArea(stockAmount, payout);
            Agent28.NotifySellJobStart(sp.Label, isVehicleJob, chosenDropoff);
        }

        public class SellDeliveryAreaTrigger : MonoBehaviour
        {
            private float _stockAmount;
            private float _payout;

            public void Init(float stock, float payout)
            {
                _stockAmount = stock;
                _payout = payout;
            }
            private void OnTriggerEnter(Collider other)
            {
                if (other == null) return;

                Transform root = other.attachedRigidbody != null
                    ? other.attachedRigidbody.transform.root
                    : (other.transform.root != null ? other.transform.root : other.transform);

                if (root == null) return;

                GameObject go = root.gameObject;
                if (go == null) return;

                bool isCorrectCarrier = false;

                // Case 1: crate job
                if (_sellCrateObject != null && go == _sellCrateObject)
                {
                    isCorrectCarrier = true;
                }

                // Case 2: vehicle job
                if (!isCorrectCarrier && _sellVehicleObject != null)
                {
                    if (go == _sellVehicleObject || (_sellVehicleObject != null && go.transform.IsChildOf(_sellVehicleObject.transform)))
                    {
                        isCorrectCarrier = true;

                        if (!_sellJobUsedWarehouseVeeper)
                        {
                            var vehicle = VehicleRegistry.GetByName(_sellVehicleObject.name);
                            if (vehicle != null)
                            {
                                vehicle.IsPlayerOwned = false;
                                vehicle.TopSpeed = 0f;
                                vehicle.VehiclePrice = 0f;
                            }
                        }
                    }
                }

                if (!isCorrectCarrier)
                    return;

                if (!BusinessState.SellJobInProgress || _payout <= 0f || _stockAmount <= 0f)
                    return;

                // Complete the job: pay money and stats
                Money.ChangeCashBalance(_payout);
                BusinessState.RegisterSale(_payout, true);
                BusinessState.ClearSellJobFlag();
                WeaponShipments.Quests.QuestManager.TryStartAct3IfEligible();

                // Tell Agent 28 – Hyland is the only target for now
                Agent28.NotifySellReport(
                    _stockAmount,
                    _payout,
                    "Hyland Point"
                );

                MelonLogger.Msg(
                    "[SellDeliveryArea] Sell job completed. Delivered {0} stock for ${1:N0}.",
                    _stockAmount,
                    _payout
                );

                // Remove the crate immediately (if this was a crate job)
                if (_sellCrateObject != null)
                {
                    Object.Destroy(_sellCrateObject);
                    _sellCrateObject = null;
                }

                // Return warehouse Veeper or destroy spawned vehicle
                if (_sellJobUsedWarehouseVeeper)
                {
                    WarehouseVeeperManager.ReturnAfterSellJob();
                }
                else if (_sellVehicleObject != null)
                {
                    Object.Destroy(_sellVehicleObject, 180f);
                }
                _sellVehicleObject = null;

                // Remove the delivery trigger/area right away
                Object.Destroy(this.gameObject);

                _sellJobActive = false;
                _sellArea = null;

                WeaponShipmentApp.Instance?.UpdateBars();
            }
        }

        private static void CreateLimeGreenCube(Transform parent, Vector3 size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "DeliveryAreaVisual_LimeCube";
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = size;

            Object.Destroy(cube.GetComponent<Collider>());

            var renderer = cube.GetComponent<MeshRenderer>();

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);

            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ReceiveShadows", 0f);
            mat.SetColor("_BaseColor", new Color(0.5f, 1f, 0f, 0.15f));

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.5f, 1f, 0f) * 1.5f);
            }

            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;

            renderer.material = mat;
        }

        // -----------------------------------------------------------
        // SHIPMENT DELIVERY TRIGGER
        // -----------------------------------------------------------

        public class DeliveryAreaTrigger : MonoBehaviour
        {
            private string _shipmentId;

            public void Init(string shipmentId) => _shipmentId = shipmentId;

            private void OnTriggerEnter(Collider other)
            {
                if (other == null || other.gameObject == null) return;

                Transform root = other.transform.root ?? other.transform;

                if (root.name != "WeaponShipment") return;

                var shipment = ShipmentManager.Instance.GetShipment(_shipmentId);
                if (shipment == null || shipment.Delivered) return;

                ShipmentManager.Instance.DeliverShipment(_shipmentId);

                int reward = Mathf.Clamp(shipment.Quantity, 10, BusinessConfig.MaxSupplies);
                BusinessState.TryAddSupplies(reward);
                BusinessState.RegisterResupplyJobCompleted();
                Agent28.NotifySuppliesArrived(
                    reward,
                    BusinessState.Supplies,
                    true   // fromShipment = true
                );

                ShipmentManager.Instance.RemoveShipment(_shipmentId);

                Destroy(root.gameObject);
                Destroy(this.gameObject);

                MelonLogger.Msg($"[DeliveryArea] Shipment {_shipmentId} delivered. +{reward} supplies");

                WeaponShipmentApp.Instance?.OnExternalShipmentChanged(_shipmentId);
            }
        }
    }
}
