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
    public sealed class Igor : NPC
    {
        public override bool IsPhysical => true;
        public static Igor Instance { get; private set; }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            var manorParking = ParkingLotRegistry.Get<ManorParking>();
            var northApartments = Building.Get<NorthApartments>();
            MelonLogger.Msg("Configuring prefab for NPC 1");
            Vector3 spawnPos = new Vector3(-36.5022f, 1.89f, 26.8121f);
            builder.WithIdentity("IgorWS", "Igor", "")
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0.0f;
                    av.Height = 1.1f;
                    av.Weight = 0.4375f;
                    var skinColor = new Color(0.8398f, 0.7045f, 0.5947f);
                    av.SkinColor = skinColor;
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = Color.white;
                    av.PupilDilation = 0.6844f;
                    av.EyebrowScale = 1.5688f;
                    av.EyebrowThickness = 1f;
                    av.EyebrowRestingHeight = -1f;
                    av.EyebrowRestingAngle = 6.8437f;
                    av.LeftEye = (0.1f, 0.3375f);
                    av.RightEye = (0.1f, 0.3375f);
                    av.HairColor = new Color(0.2863f, 0.251f, 0.2196f);
                    av.HairPath = "Avatar/Hair/ShoulderLength/ShoulderLength";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_NeutralPout", Color.black);
                    av.WithFaceLayer("Avatar/Layers/Face/OldPersonWrinkles", new Color(0f, 0f, 0f, 0.7567f));
                    av.WithFaceLayer("Avatar/Layers/Face/TiredEyes", new Color(0f, 0f, 0f, 1f));
                    av.WithFaceLayer("Avatar/Layers/Face/EyeShadow", new Color(0f, 0f, 0f, 1f));
                    av.WithFaceLayer("Avatar/Layers/Face/FacialHair_Goatee", new Color(0.2863f, 0.251f, 0.2196f));
                    av.WithBodyLayer("Avatar/Layers/Top/Overalls", new Color(0.2148f, 0.2148f, 0.2148f));
                    av.WithBodyLayer("Avatar/Layers/Accessories/FingerlessGloves", new Color(0.1205f, 0.1205f, 0.1205f));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/BulletProofVest/BulletProofVest", new Color(0.1964f, 0.1964f, 0.1964f));
                    av.WithAccessoryLayer("Avatar/Accessories/Waist/Belt/Belt", new Color(0.0714f, 0.0714f, 0.0714f));
                    av.WithAccessoryLayer("Avatar/Accessories/FacialHair/Chevron/Chevron", new Color(0.2863f, 0.251f, 0.2196f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/CombatBoots/CombatBoots", new Color(0.2321f, 0.2321f, 0.2321f));
                    av.WithAccessoryLayer("Avatar/Accessories/Head/LegendSunglasses/LegendSunglasses", new Color(0f, 0f, 0f));
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
                });
        }

        public Igor() : base()
        {
        }

        private const string DEFAULT_CONTAINER = "Igor_DefaultBusy";

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

        public static void SetDefaultDialogueActive()
        {
            if (Instance == null)
                return;

            Instance.ActivateDefaultDialogue();
        }

        private const string GO_NAME = "IgorWS";

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
                MelonLogger.Error($"Igor OnCreated failed: {ex.Message}");
                MelonLogger.Error($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}