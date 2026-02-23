using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.NPCs.Northtown;
using S1API.Entities.Schedule;
using S1API.GameTime;
using S1API.Growing;
using S1API.Map;
using S1API.Map.Buildings;
using S1API.Map.ParkingLots;
using S1API.Money;
using S1API.Products;
using S1API.Properties;
using S1API.Vehicles;
using System.Linq;
using UnityEngine;
using WeaponShipments.Data;
using WeaponShipments.Quests;
using static MelonLoader.MelonLogger;

namespace CustomNPCTest.NPCs
{
    /// <summary>
    /// An example S1API NPC that opts into a physical rig.
    /// Demonstrates movement and inventory usage.
    /// </summary>
    public sealed class Manny : NPC
    {
        public override bool IsPhysical => true;
        public static Manny Instance { get; private set; }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            var manorParking = ParkingLotRegistry.Get<ManorParking>();
            var northApartments = Building.Get<NorthApartments>();
            MelonLogger.Msg("Configuring prefab for NPC 1");
            Vector3 spawnPos = new Vector3(0f, 500f, 0f);
            builder.WithIdentity("MannyWS", "Manny", "")
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0.0f;
                    av.Height = 1.04f;
                    av.Weight = 0.625f;
                    var skinColor = new Color(0.4018f, 0.3207f, 0.2549f);
                    av.SkinColor = skinColor;
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = Color.white;
                    av.PupilDilation = 0.6844f;
                    av.EyebrowScale = 1.35f;
                    av.EyebrowThickness = 1.62f;
                    av.EyebrowRestingHeight = -0.6313f;
                    av.EyebrowRestingAngle = 0f;
                    av.LeftEye = (0.2469f, 0.3344f);
                    av.RightEye = (0.2469f, 0.3344f);
                    av.HairColor = new Color(0.2453f, 0.1921f, 0.14f);
                    av.WithFaceLayer("Avatar/Layers/Face/Face_NeutralPout", Color.black);
                    av.WithFaceLayer("Avatar/Layers/Face/OldPersonWrinkles", new Color32(0, 0, 0, 1));
                    av.WithFaceLayer("Avatar/Layers/Face/FacialHair_Goatee", new Color(0.2453f, 0.1921f, 0.14f));
                    av.WithBodyLayer("Avatar/Layers/Top/T-Shirt", new Color(0.151f, 0.151f, 0.151f));
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.151f, 0.151f, 0.151f));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/Blazer/Blazer", new Color(0.3864f, 0.2881f, 0.5268f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/DressShoes/DressShoes", new Color(0.0179f, 0.0134f, 0.0134f));
                })
                .WithSpawnPosition(spawnPos)
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(minWeekly: 1f, maxWeekly: 1f)
                        .WithOrdersPerWeek(1, 1)
                        .WithPreferredOrderDay(Day.Saturday)
                        .WithOrderTime(0500)
                        .WithStandards(CustomerStandard.VeryHigh)
                        .AllowDirectApproach(true)
                        .GuaranteeFirstSample(false)
                        .WithMutualRelationRequirement(minAt50: 5.0f, maxAt100: 5.0f)
                        .WithCallPoliceChance(0.0f)
                        .WithDependence(baseAddiction: 0.0f, dependenceMultiplier: 0.0f)
                        .WithAffinities(new[]
                        {
                            (DrugType.Marijuana, -1f), (DrugType.Methamphetamine, -1f), (DrugType.Shrooms, -1), (DrugType.Cocaine, -1f)
                        })
                        .WithPreferredProperties();
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(5.0f)
                        .SetUnlocked(false)
                        .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
                })
                .WithSchedule(plan =>
                {
                }
                );
        }

        public Manny() : base()
        {
        }

        private const string DEFAULT_CONTAINER = "Manny_DefaultBusy";

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

        private const string ACT0_CONTAINER = "Act0_Manny_FirstMeet";

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

                Dialogue.BuildAndRegisterContainer(ACT0_CONTAINER, c =>
                {
                    {
                        c.AddNode("ENTRY",
                            "Agent told me your looking for someone to manufacture weapons.",
                            ch =>
                            {
                                ch.Add("ACT0_CONTINUE", "Yeah, im assuming you know someone.", "SEPARATION_1");
                            });

                        c.AddNode("SEPARATION_1",
                            "Yes i do, this guy is very experienced.",
                            ch => ch.Add("ACT0_CONTINUE3", "How come?", "SEPARATION_2"));

                        c.AddNode("SEPARATION_2",
                            "He worked for the Benzies for 6 years",
                            ch => ch.Add("ACT0_CONTINUE4", "Why did they just let him go?", "SEPARATION_3"));

                        c.AddNode("SEPARATION_3",
                            "They didnt. He's on the run and needs some place to stay. \n" +
                            "That means he keeps his head down and does the work.",
                            ch => ch.Add("ACT0_CONTINUE5", "And that's him?", "REFERRAL_1"));

                        c.AddNode("REFERRAL_1",
                            "Yeah. If you're serious, talk to him. Not me.",
                            ch => ch.Add("ACT0_HANDOFF", "Alright. I'll talk to him.", "HANDOFF_1"));

                        c.AddNode("HANDOFF_1",
                            "Just remember, he doesn't like his time wasted.");
                    }
                });

                Dialogue.OnChoiceSelected("ACT0_HANDOFF", () =>
                {
                    QuestManager.HireArchie();
                    ActivateDefaultDialogue();
                });
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Act0] RegisterMeetupDialogue failed: {ex}");
                if (ex.InnerException != null)
                    MelonLogger.Error($"[Act0] Inner: {ex.InnerException}");
            }
        }

        private const string GO_NAME = "MannyWS";

        private void RenameSpawnedGameObject()
        {
            if (gameObject != null && gameObject.name != GO_NAME)
                gameObject.name = GO_NAME;
        }

        protected override void OnCreated()
        {
            try
            {
                Instance = this;

                base.OnCreated();
                RenameSpawnedGameObject();
                Appearance.Build();
                ActivateDefaultDialogue();

                Aggressiveness = 1f;
                Region = Region.Northtown;

                Schedule.Enable();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Manny OnCreated failed: {ex.Message}");
                MelonLogger.Error($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}