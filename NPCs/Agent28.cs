using MelonLoader;
using S1API.Economy;
using S1API.Entities;
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
using S1API.Vehicles;
using System.Linq;
using UnityEngine;

namespace WeaponShipments.NPCs
{
    /// <summary>
    /// An example S1API NPC that opts into a physical rig.
    /// Demonstrates movement and inventory usage.
    /// </summary>
    public sealed class Agent28 : NPC
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

        /// <summary>
        /// Called when a steal-supplies job is started and the pickup location is active.
        /// </summary>
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

        /// <summary>
        /// Intended for when the pickup is done and the player is sent to the dropoff.
        /// Call this from your crate pickup logic when you swap to the delivery phase.
        /// </summary>
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
            // 0–1 -> 1–100
            int percent = Mathf.Clamp(Mathf.RoundToInt(lostFraction * 100f), 1, 100);

            // Convert to "k" value (e.g. 7800 -> 7.8k)
            float valueK = Mathf.Max(0f, lostValue / 1000f);

            // --- Openers talking about % lost ---
            string[] percentLines =
            {
                $"We just got hit. Around {percent}% of our stock is gone.",
                $"Cops swept the place. Looks like about {percent}% of our stash is missing.",
                $"Bad news. Raid went through the warehouse – roughly {percent}% gone.",
                $"Warehouse got tossed. We're down about {percent}% of product.",
                $"{percent}% of what we had just vanished. Raid hit harder than expected."
            };

            // --- Follow-ups talking about value lost ---
            string[] valueLines =
            {
                $"Numbers came in... missing roughly {valueK:0.#}k worth.",
                $"Accounting ran it – about {valueK:0.#}k in product is gone.",
                $"Rough estimate puts the loss at ~{valueK:0.#}k.",
                $"On paper, that's around {valueK:0.#}k down the drain.",
                $"Call it {valueK:0.#}k gone, give or take."
            };

            // --- Combined one-shot messages (% + value) ---
            string[] combinedLines =
            {
                $"We got raided. Value loss is {valueK:0.#}k, around {percent}% gone.",
                $"Raid report: about {percent}% of stock wiped, roughly {valueK:0.#}k in losses.",
                $"Warehouse hit. Estimate {valueK:0.#}k missing, close to {percent}% of what we had.",
                $"Police tore through. Roughly {percent}% gone – around {valueK:0.#}k in product.",
                $"Summary: {percent}% of inventory, about {valueK:0.#}k, is off the books."
            };

            // Decide: combined vs double-text
            bool combined = UnityEngine.Random.value < 0.4f; // ~40% of the time, single combined msg

            if (combined)
            {
                int i = UnityEngine.Random.Range(0, combinedLines.Length);
                Instance.SendTextMessage(combinedLines[i]);
                yield break;
            }

            // Otherwise, two separate texts with delay
            int firstIndex = UnityEngine.Random.Range(0, percentLines.Length);
            Instance.SendTextMessage(percentLines[firstIndex]);

            float delay = UnityEngine.Random.Range(4f, 9f);
            yield return new WaitForSeconds(delay);

            int secondIndex = UnityEngine.Random.Range(0, valueLines.Length);
            Instance.SendTextMessage(valueLines[secondIndex]);
        }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Vector3 spawnPos = new Vector3(72.263f, -4.535f, 30.9708f);
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
                .EnsureCustomer()
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(minWeekly: 0f, maxWeekly: 1f)
                        .WithOrdersPerWeek(0, 0)
                        .WithPreferredOrderDay(Day.Thursday)
                        .WithOrderTime(0530)
                        .WithStandards(CustomerStandard.Moderate)
                        .AllowDirectApproach(true)
                        .GuaranteeFirstSample(false)
                        .WithMutualRelationRequirement(minAt50: 2.5f, maxAt100: 4.0f)
                        .WithCallPoliceChance(0.15f)
                        .WithDependence(baseAddiction: 0.25f, dependenceMultiplier: 1.0f)
                        .WithAffinities(new[]
                        {
                            (DrugType.Marijuana, 0.52f), (DrugType.Methamphetamine, 0.73f), (DrugType.Cocaine, 0.14f)
                        })
                        .WithPreferredProperties(Property.AntiGravity, Property.Spicy, Property.CalorieDense);
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(4.0f)
                        .SetUnlocked(false)
                        .SetUnlockType(NPCRelationship.UnlockType.DirectApproach)
                        .WithConnectionsById();
                })
                .WithSchedule(plan =>
                {
                });
        }

        public Agent28() : base()
        {
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();
                Appearance.Build();

                Instance = this;

                Dialogue.BuildAndSetDatabase(db => {
                    db.WithModuleEntry("Reactions", "GREETING", "Welcome.");
                });
                Dialogue.BuildAndRegisterContainer("AlexShop", c => {
                    c.AddNode("ENTRY", "What do you want?", ch => {
                        ch.Add("PAY_FOR_INFO", "I want to start smuggling weapons.", "INFO_NODE")
                            .Add("NO_THANKS", "Nothing.", "EXIT");
                    });
                    c.AddNode("INFO_NODE", "I sent you the app", ch => {
                        ch.Add("BYE", "Thanks", "EXIT");
                    });
                    c.AddNode("NOT_ENOUGH", "Who are you?", ch => {
                        ch.Add("BACK", "I'll come back.", "ENTRY");
                    });
                    c.AddNode("EXIT", "See you.");
                });

                Dialogue.OnChoiceSelected("PAY_FOR_INFO", () =>
                {
                    var fr = LevelManager.CurrentRank;   // FullRank

                    MelonLogger.Msg($"[RankCheck] Rank = {fr.Rank}, Tier = {fr.Tier}");

                    const Rank RequiredRank = Rank.Hoodlum;

                    bool pass = fr.Rank >= RequiredRank;

                    MelonLogger.Msg($"[RankCheck] Required = {RequiredRank}, Pass = {pass}");

                    if (pass)
                    {
                        MelonLogger.Msg("[RankCheck] JUMP -> INFO_NODE");
                        Dialogue.JumpTo("RankCheck", "INFO_NODE");
                    }
                    else
                    {
                        MelonLogger.Msg("[RankCheck] JUMP -> NOT_ENOUGH");
                        Dialogue.JumpTo("RankCheck", "NOT_ENOUGH");
                    }
                });


                Dialogue.OnNodeDisplayed("INFO_NODE", () => {
                    // Ran when "Get scammed nerd." is shown
                });

                Dialogue.OnChoiceSelected("BYE", () =>
                {
                    Dialogue.StopOverride();
                    SendTextMessage("You got scammed");
                });

                Dialogue.UseContainerOnInteract("AlexShop");
                Aggressiveness = 3f;
                Region = Region.Northtown;

                // Customer.RequestProduct();

                Schedule.Enable();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"ExamplePhysicalNPC OnCreated failed: {ex.Message}");
                MelonLogger.Error($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}
