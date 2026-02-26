using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.NPCs.Northtown;
using S1API.Entities.NPCs.Westville;
using S1API.Entities.Schedule;
using S1API.GameTime;
using S1API.Growing;
using S1API.Leveling;
using S1API.Map;
using S1API.Map.Buildings;
using S1API.Map.ParkingLots;
using S1API.Money;
using S1API.Products;
using S1API.Properties;
using S1API.Quests;
using S1API.Vehicles;
using System.Linq;
using UnityEngine;
using WeaponShipments.Data;
using WeaponShipments.Quests;
using WeaponShipments.Saveables;

namespace WeaponShipments.NPCs
{
    public sealed partial class Agent28 : NPC
    {
        public static Agent28 Instance { get; private set; }
        public override bool IsPhysical => true;

        public static void NotifyStockFull(int maxStock)
        {
            if (Instance == null)
                return;

            string[] lines =
            {
                $"Storage is full, {maxStock} units at capacity.",
                $"Warehouse maxed out. {maxStock} units sitting in storage.",
                $"Production paused. We're holding {maxStock}/{maxStock} units.",
                $"Shelves packed. Capacity reached at {maxStock} units.",
                $"Output halted. Storage maxed at {maxStock} units.",
                $"Can't add more stock, {maxStock} units on hand.",
                $"Inventory space tapped out. {maxStock} units stored."
            };

            int i = UnityEngine.Random.Range(0, lines.Length);
            Instance.SendTextMessage(lines[i]);
        }

        public static void NotifySuppliesEmpty()
        {
            if (Instance == null)
                return;

            string[] lines =
            {
                "We're out of supplies. Nothing left to process.",
                "Supplies just hit zero. Can't make product until we restock.",
                "Nothing left to work with. Storage is empty.",
                "Supply count reached 0. We’re tapped for now.",
                "We're completely out of materials.",
                "Zero supplies remaining. We need a restock ASAP.",
                "No supplies left on hand. Storage is cleared out.",
                "We burned through the last of it. Supply is at zero.",
                "Office is empty. Nothing left to pull from.",
            };

            int i = UnityEngine.Random.Range(0, lines.Length);
            Instance.SendTextMessage(lines[i]);
        }

        public static void NotifySuppliesArrived(int amount, float newTotal, bool fromShipment)
        {
            if (Instance == null)
                return;

            string source = fromShipment ? "shipment" : "order";

            string[] lines =
            {
                $"Supplies just landed at the office.",
                $"Delivery arrived, fresh supplies on site.",
                $"New materials checked in. Supplies restocked.",
                $"Shipment just came through the door.",
                $"Office received the latest shipment.",
                $"Fresh batch of materials arrived at HQ.",
                $"Shipment delivered. Supplies are ready to use."
            };

            int i = UnityEngine.Random.Range(0, lines.Length);
            Instance.SendTextMessage(lines[i]);
        }

        public static void NotifyStealPickup(string pickupLocation)
        {
            if (Instance == null)
                return;

            string[] lines =
            {
                $"Shipment spotted at {pickupLocation}. Go collect it.",
                $"There's a load waiting at {pickupLocation}. Pick it up.",
                $"Supplies are sitting at {pickupLocation}. Go grab the batch.",
                $"A fresh drop just landed at {pickupLocation}. Retrieve it quietly.",
                $"Crates are staged at {pickupLocation}. Go scoop them up.",
                $"Supplies left unattended at {pickupLocation}. Move in and take them.",
                $"Found a shipment at {pickupLocation}. Get your hands on it.",
                $"There's gear sitting at {pickupLocation}. Pick it up before it moves.",
                $"Cargo sitting out at {pickupLocation}. Go secure the load.",
                $"A full batch is resting at {pickupLocation}. Collect it now."
            };

            int i = UnityEngine.Random.Range(0, lines.Length);
            Instance.SendTextMessage(lines[i]);
        }

        public static void NotifyStealDropoff(string dropoffLocation)
        {
            if (Instance == null)
                return;

            string[] lines =
            {
                $"Got a spot for that load — bring it to {dropoffLocation}.",
                $"Drop point ready. Move the shipment to {dropoffLocation}.",
                $"Take what you collected and head to {dropoffLocation}.",
                $"Route updated. Deliver the batch to {dropoffLocation}.",
                $"Now bring everything you picked up to {dropoffLocation}.",
                $"Delivery zone’s set. Get the load to {dropoffLocation}.",
                $"All right, move the cargo to {dropoffLocation}.",
                $"Drop it off at {dropoffLocation} once you're clear.",
                $"Your destination is {dropoffLocation}. Bring the shipment in.",
                $"Head to {dropoffLocation} and hand off the load."
            };

            int i = UnityEngine.Random.Range(0, lines.Length);
            Instance.SendTextMessage(lines[i]);
        }

        public static void NotifySellJobStart(string spawnLabel, bool isVehicle, string dropoffLabel)
        {
            if (Instance == null)
                return;

            MelonCoroutines.Start(SellJobMessageRoutine(spawnLabel, isVehicle, dropoffLabel));
        }

        private static System.Collections.IEnumerator DeferredWarehouseDoorReplaceCoroutine()
        {
            yield return null;
            WarehouseDoorReplacer.TryReplaceWarehouseDoor();
        }

        private static System.Collections.IEnumerator SellJobMessageRoutine(
            string spawnLabel,
            bool isVehicle,
            string dropoffLabel
        )
        {
            string[] crateLines =
            {
                $"Shipment's sitting in a crate {spawnLabel}.",
                $"Crate's prepped {spawnLabel}. Go pick it up.",
                $"Your load's packed into a crate {spawnLabel}.",
                $"Crate is staged {spawnLabel}. Move it when you're ready.",
                $"Product's boxed up in a crate {spawnLabel}."
            };

            string[] vehicleLines =
            {
                $"Delivery car is waiting {spawnLabel}.",
                $"Vehicle's staged {spawnLabel}. Keys are in it.",
                $"Your load's already loaded into a vehicle {spawnLabel}.",
                $"Ride is parked {spawnLabel}. That's your delivery car.",
                $"Vehicle with the product is sitting {spawnLabel}."
            };

            string first;
            if (isVehicle)
            {
                int i = UnityEngine.Random.Range(0, vehicleLines.Length);
                first = vehicleLines[i];
            }
            else
            {
                int i = UnityEngine.Random.Range(0, crateLines.Length);
                first = crateLines[i];
            }

            Instance.SendTextMessage(first);

            float delay = UnityEngine.Random.Range(6f, 8f);
            yield return new UnityEngine.WaitForSeconds(delay);

            string[] dropoffLines =
            {
                $"Buyer just confirmed the location. Deliver it to {dropoffLabel}.",
                $"All right, buyer's locked in on the location. Get it to {dropoffLabel}.",
                $"Route's live. Take the shipment over to {dropoffLabel}.",
                $"Buyer signed off on the spot. Run it to {dropoffLabel}.",
                $"Drop is green-lit. Deliver the load to {dropoffLabel}."
            };

            int d = UnityEngine.Random.Range(0, dropoffLines.Length);
            Instance.SendTextMessage(dropoffLines[d]);
        }

        public static void NotifySellReport(float stockSold, float payout, string destinationName)
        {
            if (Instance == null)
                return;

            float payoutK = payout / 1000f;

            string[] lines =
            {
                $"Sell run to {destinationName} complete. Moved {stockSold:0.#} units for ~${payout:N0}.",
                $"Report from {destinationName}: {stockSold:0.#} units offloaded, around {payoutK:0.#}k in cash.",
                $"Job wrapped at {destinationName}. {stockSold:0.#} stock sold, payout ~${payout:N0}."
            };

            int i = UnityEngine.Random.Range(0, lines.Length);
            Instance.SendTextMessage(lines[i]);
        }

        public static void NotifyRaid(float lostFraction, float lostValue)
        {
            if (Instance == null)
                return;

            MelonCoroutines.Start(RaidMessageRoutine(lostFraction, lostValue));
        }

        private static System.Collections.IEnumerator RaidMessageRoutine(float lostFraction, float lostValue)
        {
            int percent = Mathf.Clamp(Mathf.RoundToInt(lostFraction * 100f), 1, 100);

            float valueK = Mathf.Max(0f, lostValue / 1000f);

            string[] percentLines =
            {
                $"We just got hit. Around {percent}% of our stock is gone.",
                $"Cops swept the place. Looks like about {percent}% of our stash is missing.",
                $"Bad news. Raid went through the warehouse – roughly {percent}% gone.",
                $"Warehouse got tossed. We're down about {percent}% of product.",
                $"{percent}% of what we had just vanished. Raid hit harder than expected."
            };

            string[] valueLines =
            {
                $"Numbers came in... missing roughly {valueK:0.#}k worth.",
                $"Accounting ran it – about {valueK:0.#}k in product is gone.",
                $"Rough estimate puts the loss at ~{valueK:0.#}k.",
                $"On paper, that's around {valueK:0.#}k down the drain.",
                $"Call it {valueK:0.#}k gone, give or take."
            };

            string[] combinedLines =
            {
                $"We got raided. Value loss is {valueK:0.#}k, around {percent}% gone.",
                $"Raid report: about {percent}% of stock wiped, roughly {valueK:0.#}k in losses.",
                $"Warehouse hit. Estimate {valueK:0.#}k missing, close to {percent}% of what we had.",
                $"Police tore through. Roughly {percent}% gone – around {valueK:0.#}k in product.",
                $"Summary: {percent}% of inventory, about {valueK:0.#}k, is off the books."
            };

            bool combined = UnityEngine.Random.value < 0.4f; // ~40% of the time, single combined msg

            if (combined)
            {
                int i = UnityEngine.Random.Range(0, combinedLines.Length);
                Instance.SendTextMessage(combinedLines[i]);
                yield break;
            }

            int firstIndex = UnityEngine.Random.Range(0, percentLines.Length);
            Instance.SendTextMessage(percentLines[firstIndex]);

            float delay = UnityEngine.Random.Range(4f, 9f);
            yield return new WaitForSeconds(delay);

            int secondIndex = UnityEngine.Random.Range(0, valueLines.Length);
            Instance.SendTextMessage(valueLines[secondIndex]);
        }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Vector3 spawnPos = new Vector3(0f, 500f, 0f);
            builder.WithIdentity("Agent28", "Agent 28", "")
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0.0f;
                    av.Height = 1.0f;
                    av.Weight = 0.35f;
                    av.SkinColor = new Color(0.525f, 0.427f, 0.337f);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = new Color(1.0f, 0.82f, 0.82f);
                    av.PupilDilation = 0.6f;
                    av.EyebrowScale = 1.23f;
                    av.EyebrowThickness = 1.48f;
                    av.EyebrowRestingHeight = -0.25f;
                    av.EyebrowRestingAngle = 5.67f;
                    av.LeftEye = (0.31f, 0.35f);
                    av.RightEye = (0.31f, 0.35f);
                    av.HairColor = new Color(0.075f, 0.075f, 0.075f);
                    av.HairPath = "Avatar/Hair/Peaked/Peaked";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_Neutral", Color.black);
                    av.WithFaceLayer("Avatar/Layers/Face/FacialHair_Stubble", Color.black);
                    av.WithBodyLayer("Avatar/Layers/Top/Buttonup", new Color(0.151f, 0.151f, 0.151f));
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.151f, 0.151f, 0.151f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/CombatBoots/CombatBoots", new Color(0.151f, 0.151f, 0.151f));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/BulletproofVest/BulletproofVest", new Color(0.151f, 0.151f, 0.151f));
                })
                .WithSpawnPosition(spawnPos)
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(5.0f)
                        .SetUnlocked(false)
                        .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
                })
                .WithSchedule(plan =>
                {
                });
        }

        public Agent28() : base()
        {
        }

        private const string DEFAULT_CONTAINER = "Agent28_DefaultBusy";

        private static bool _defaultDialogueRegistered = false;
        private static bool _meetupDialogueRegistered = false;

        private void RegisterDefaultDialogue()
        {
            if (_defaultDialogueRegistered)
                return;

            _defaultDialogueRegistered = true;

            Dialogue.BuildAndRegisterContainer(DEFAULT_CONTAINER, c =>
            {
                c.AddNode("ENTRY", "I'm busy right now.", ch =>
                {
                    ch.Add("OK", "Alright.", "EXIT");
                });

                c.AddNode("EXIT", "");
            });
        }

        private void ActivateDefaultDialogue()
        {
            RegisterDefaultDialogue();
            Dialogue.UseContainerOnInteract(DEFAULT_CONTAINER);
        }

        private void ActivateMeetupDialogue()
        {
            RegisterMeetupDialogue();
            Dialogue.UseContainerOnInteract(ACT0_CONTAINER);
        }

        public static void SetDefaultDialogueActive()
        {
            if (Instance == null)
                return;

            Instance.ActivateDefaultDialogue();
        }

        public static void SetMeetupDialogueActive()
        {
            if (Instance == null)
                return;

            Instance.ActivateMeetupDialogue();
        }

        private static bool _warehouseDialogueRegistered = false;
        private const string WAREHOUSE_CONTAINER = "Agent28_Warehouse_StartAct2";

        public static void SetWarehouseDialogueActive()
        {
            if (Instance == null)
                return;

            Instance.RegisterWarehouseDialogue();
            Instance.ActivateWarehouseDialogue();
        }

        private const string MOVINGUP_CONTAINER = "Agent28_MovingUp";
        private static bool _movingUpDialogueRegistered;

        public static void SetMovingUpDialogueActive()
        {
            if (Instance == null) return;
            Instance.RegisterMovingUpDialogue();
            Instance.ActivateMovingUpDialogue();
        }

        private void RegisterMovingUpDialogue()
        {
            if (_movingUpDialogueRegistered) return;
            _movingUpDialogueRegistered = true;
            Dialogue.BuildAndRegisterContainer(MOVINGUP_CONTAINER, c =>
            {
                c.AddNode("ENTRY",
                    "The garage gives us room to expand. Get the equipment set up and we can scale.",
                    ch => ch.Add("OK", "Got it.", "EXIT"));
                c.AddNode("EXIT", "");
            });
            Dialogue.OnChoiceSelected("OK", () =>
            {
                var quest = WeaponShipments.Quests.QuestManager.GetMovingUpQuest();
                if (quest != null && quest.QuestEntries.Count >= 2)
                    quest.QuestEntries[1].Complete();
                ActivateDefaultDialogue();
            });
        }

        private void ActivateMovingUpDialogue()
        {
            RegisterMovingUpDialogue();
            Dialogue.UseContainerOnInteract(MOVINGUP_CONTAINER);
        }

        public static void SetDialogueFromWarehouseState()
        {
            if (Instance == null) return;

            var movingUp = WeaponShipments.Quests.QuestManager.GetMovingUpQuest();
            if (movingUp != null && movingUp.Stage >= 2)
            {
                var entries = movingUp.QuestEntries;
                if (entries != null && entries.Count >= 2)
                {
                    SetMovingUpDialogueActive();
                    return;
                }
            }

            var saved = WSSaveData.Instance?.NewNumberQuest;
            if (saved != null && saved.Stage == 1)
            {
                SetMeetupDialogueActive();
                return;
            }

            var p = WSPersistent.Instance?.Data;
            if (p != null && p.DealCompleteAwaitingSleep)
            {
                SetGoToSleepDialogueActive();
                return;
            }
            if (p != null && p.AwaitingWarehouseTalk)
            {
                SetWarehouseDialogueActive();
                return;
            }

            SetDefaultDialogueActive();
        }

        private const string GOTOSLEEP_CONTAINER = "Agent28_GoToSleep";
        private static bool _goToSleepRegistered;

        public static void SetGoToSleepDialogueActive()
        {
            if (Instance == null) return;
            Instance.RegisterGoToSleepDialogue();
            Instance.ActivateGoToSleepDialogue();
        }

        private void RegisterGoToSleepDialogue()
        {
            if (_goToSleepRegistered) return;
            _goToSleepRegistered = true;

            Dialogue.BuildAndRegisterContainer(GOTOSLEEP_CONTAINER, c =>
            {
                c.AddNode("ENTRY",
                    "Get some rest. We'll handle this in the morning.",
                    ch => ch.Add("OK", "Alright.", "EXIT"));
                c.AddNode("EXIT", "");
            });
        }

        private void ActivateGoToSleepDialogue()
        {
            RegisterGoToSleepDialogue();
            Dialogue.UseContainerOnInteract(GOTOSLEEP_CONTAINER);
        }

        private void RegisterWarehouseDialogue()
        {
            if (_warehouseDialogueRegistered)
                return;

            _warehouseDialogueRegistered = true;

            Dialogue.BuildAndRegisterContainer(WAREHOUSE_CONTAINER, c =>
            {
                c.AddNode("ENTRY",
                    "The warehouse is yours. I've got someone in mind for the crew – I'll reach out when I have details.",
                    ch =>
                    {
                        ch.Add("START", "Let's get started.", "EXIT");
                    });
                c.AddNode("EXIT", "");
            });

            Dialogue.OnChoiceSelected("START", () =>
            {
                var p = WSPersistent.Instance?.Data;
                if (p != null) p.AwaitingWarehouseTalk = false;

                var q1 = WeaponShipments.Quests.QuestManager.GetNewNumberQuest();
                if (q1 != null)
                    q1.Complete();

                WeaponShipments.Quests.QuestManager.PurchaseWarehouse();
                ActivateDefaultDialogue();
                MelonLogger.Msg("[Agent28] Warehouse dialogue – Act 2 started.");
            });
        }

        private void ActivateWarehouseDialogue()
        {
            RegisterWarehouseDialogue();
            Dialogue.UseContainerOnInteract(WAREHOUSE_CONTAINER);
        }

        private const string ACT0_CONTAINER = "Act0_Agent28_FirstMeet";
        private const string ACT0_CH_PAYNOW = "ACT0_PAY_NOW";
        private const string ACT0_CH_NOTYET = "ACT0_NOT_YET";
        private const string ACT0_CH_LEAVE = "ACT0_LEAVE";

        private static int ACT0_WAREHOUSE_PRICE => BusinessConfig.WarehousePrice;

        private void RegisterMeetupDialogue()
        {
            if (_meetupDialogueRegistered)
                return;

            _meetupDialogueRegistered = true;

            try
            {
                int warehousePrice = BusinessConfig.WarehousePrice;
                int signingBonus = BusinessConfig.SigningBonus;
                int totalDue = warehousePrice + signingBonus;

                string costsText =
                    "Paper trails matter. Mine is quieter than yours.\n\n" +
                    $"Warehouse: ${warehousePrice:N0}\n\n" +
                    "I already have someone lined up.\n" +
                    "Logistics. Discreet. Useful.\n\n" +
                    $"Signing bonus: ${signingBonus:N0}\n\n" +
                    $"Total due now: ${totalDue:N0}";

                Dialogue.BuildAndRegisterContainer(ACT0_CONTAINER, c =>
                {
                    {
                        c.AddNode("ENTRY",
                            "You showed up. That already tells me you’re capable of following instructions.",
                            ch =>
                            {
                                ch.Add("ACT0_CONTINUE", "Go on.", "SEPARATION_1");
                                ch.Add(ACT0_CH_LEAVE, "Not right now.", "EXIT");
                            });

                        c.AddNode("SEPARATION_1",
                            "You already know how to run a property",
                            ch => ch.Add("ACT0_CONTINUE3", "Yeah", "SEPARATION_2"));

                        c.AddNode("SEPARATION_2",
                            "Those places are built for production, controlled environments, predictable flow.",
                            ch => ch.Add("ACT0_CONTINUE4", "Alright", "SEPARATION_3"));

                        c.AddNode("SEPARATION_3",
                            "But weapons are different. They move faster. They attract attention earlier. \n" +
                            "If you run both out of the same places, you cross signals. That’s how patterns form.",
                            ch => ch.Add("ACT0_CONTINUE5", "So what’s the fix?", "FIX"));

                        c.AddNode("FIX",
                            "You need some separation.\n" +
                            "Different storage. Different routes. Different places. That’s where I come in\n" +
                            "Supplies are your problem. What happens after they arrive is mine.",
                            ch => ch.Add("ACT0_CONTINUE6", "Understood.", "WAREHOUSE_1"));

                        c.AddNode("WAREHOUSE_1",
                            "For the first step, you need a base that isn’t tied to your other work.",
                            ch =>
                            {
                                ch.Add("ACT0_COSTS", "Where would i get that?", "WAREHOUSE_2");
                            });

                        c.AddNode("WAREHOUSE_2",
                            "I already own a warehouse that fits what you need.",
                            ch =>
                            {
                                ch.Add("ACT0_COSTS", "How much?", "COSTS");
                                ch.Add("ACT0_COSTS2", "Im assuming its not free?", "COSTS");
                            });

                        c.AddNode("COSTS",
                            "A place like this would go for 8 grand.\n" +
                            $"But you can have it for ${ACT0_WAREHOUSE_PRICE:N0}",
                            ch =>
                            {
                                ch.Add(ACT0_CH_PAYNOW, "Pay now.", "COMPLETE");
                                ch.Add(ACT0_CH_NOTYET, "Not yet.", "NOTYET");
                            });

                        c.AddNode("PAYING",
                            "Good.\n\n" +
                            "You transfer the money.\n" +
                            "I handle the purchase and the hire.\n" +
                            "Once it’s done, you’ll have a base and someone to keep it stable.");

                        c.AddNode("NOT_ENOUGH",
                            "Then you’re not ready.\n" +
                            "Come back when you have the cash. Don’t waste my time.");

                        c.AddNode("NOTYET",
                            "Then we’re done here.\n" +
                            "When you’re ready to move, you know where to find me.");

                        c.AddNode("COMPLETE",
                            "The warehouse is secured.\n" +
                            "Go get some rest. I'll handle the paperwork. Come see me at the warehouse once you're up.");

                        c.AddNode("EXIT",
                            "Then don’t stand here.\n" +
                            "Walk away before you make this messy.");
                    }
                });

                // Pay for warehouse in dialogue: completes Quest 1 and starts Quest 2 (same as before).
                Dialogue.OnChoiceSelected(ACT0_CH_PAYNOW, () =>
                {
                    var data = WSSaveData.Instance?.Data;
                    if (data == null)
                        return;
                    if (data.Properties.Warehouse.Owned)
                        return;

                    int wPrice = BusinessConfig.WarehousePrice;
                    float balance = Money.GetCashBalance();
                    if (balance < wPrice)
                    {
                        Dialogue.JumpTo(ACT0_CONTAINER, "NOT_ENOUGH");
                        return;
                    }

                    Money.ChangeCashBalance(-wPrice, visualizeChange: true, playCashSound: true);
                    data.Properties.Warehouse.Owned = true;

                    MelonCoroutines.Start(DeferredWarehouseDoorReplaceCoroutine());

                    WeaponShipments.Quests.QuestManager.CompleteDialogueAndUnlockProperty();

                    ActivateDefaultDialogue();
                    Dialogue.JumpTo(ACT0_CONTAINER, "COMPLETE");
                });
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Act0] RegisterMeetupDialogue failed: {ex}");
                if (ex.InnerException != null)
                    MelonLogger.Error($"[Act0] Inner: {ex.InnerException}");
            }
        }

        protected override void OnCreated()
        {
            try
            {
                Instance = this;
                ConversationCanBeHidden = true;

                base.OnCreated();
                Appearance.Build();
                SetDialogueFromWarehouseState();

                Aggressiveness = 1f;
                Region = Region.Northtown;

                Schedule.Enable();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Agent28 OnCreated failed: {ex.Message}");
                MelonLogger.Error($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}
