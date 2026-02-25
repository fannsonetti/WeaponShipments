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
using UnityEngine;
using System.Linq;
using WeaponShipments.Data;
using WeaponShipments.Quests;
using WeaponShipments.Saveables;

namespace WeaponShipments.NPCs
{
    /// <summary>
    /// Landlord of the north warehouse building. Sells the garage space when Act 3 starts.
    /// Based on Moe Lester structure.
    /// </summary>
    public sealed class NorthWarehouseLandlord : NPC
    {
        public override bool IsPhysical => true;
        public static NorthWarehouseLandlord Instance { get; private set; }

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            var northApartments = Building.Get<NorthApartments>();
            Vector3 spawnPos = new Vector3(-35f, -3.5f, 168f);
            Vector3 posA = new Vector3(-38f, -3.5f, 165f);

            builder.WithIdentity("NorthWarehouseLandlord", "Vern", "Shaw")
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0.0f;
                    av.Height = 1.0f;
                    av.Weight = 0.9f;
                    av.SkinColor = new Color(0.784f, 0.654f, 0.545f);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = new Color(1.0f, 0.8f, 0.8f);
                    av.PupilDilation = 0.75f;
                    av.EyebrowScale = 1.0f;
                    av.EyebrowThickness = 1.31f;
                    av.EyebrowRestingHeight = -0.432f;
                    av.EyebrowRestingAngle = 0.0f;
                    av.LeftEye = (0.38f, 0.42f);
                    av.RightEye = (0.38f, 0.42f);
                    av.HairColor = new Color(0.509f, 0.375f, 0.161f);
                    av.HairPath = "Avatar/Hair/Receding/Receding";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_SmugPout", Color.black);
                    av.WithFaceLayer("Avatar/Layers/Face/Facialhair_Stubble", Color.black);
                    av.WithFaceLayer("Avatar/Layers/Face/OldPersonWrinkles", Color.black);
                    av.WithBodyLayer("Avatar/Layers/Top/FlannelButtonUp", new Color(0.803f, 0.947f, 0.657f));
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.178f, 0.217f, 0.406f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/Sneakers/Sneakers", new Color(1.0f, 1.0f, 1.0f));
                    av.WithAccessoryLayer("Avatar/Accessories/Waist/Belt/Belt", new Color(0.481f, 0.331f, 0.225f));
                    av.WithAccessoryLayer("Avatar/Accessories/Head/RectangleFrameGlasses/RectangleFrameGlasses", new Color(0.151f, 0.151f, 0.151f));
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
                    plan.EnsureDealSignal()
                        .WalkTo(posA, 900, faceDestinationDir: true)
                        .LocationDialogue(posA, 1000)
                        .StayInBuilding(northApartments, 1200, 120)
                        .WalkTo(spawnPos, 1400, faceDestinationDir: false)
                        .StayInBuilding(northApartments, 1500, 480);
                });
        }

        public NorthWarehouseLandlord() : base() { }

        private const string DEFAULT_CONTAINER = "NorthWarehouseLandlord_Default";
        private const string GARAGE_CONTAINER = "NorthWarehouseLandlord_Garage";
        private const string GARAGE_CH_PAY = "GARAGE_PAY";
        private const string GARAGE_CH_LEAVE = "GARAGE_LEAVE";

        private static int GaragePrice => BusinessConfig.GaragePrice;

        private static bool _defaultRegistered;
        private static bool _garageRegistered;

        private void RegisterDefaultDialogue()
        {
            if (_defaultRegistered) return;
            _defaultRegistered = true;

            Dialogue.BuildAndRegisterContainer(DEFAULT_CONTAINER, c =>
            {
                c.AddNode("ENTRY", "I'm busy right now.", ch => ch.Add("OK", "Alright.", "EXIT"));
                c.AddNode("EXIT", "");
            });
        }

        private void RegisterGarageDialogue()
        {
            if (_garageRegistered) return;
            _garageRegistered = true;

            Dialogue.BuildAndRegisterContainer(GARAGE_CONTAINER, c =>
            {
                c.AddNode("ENTRY",
                    "You the one looking at the garage? It's been sitting empty. Good bones.",
                    ch =>
                    {
                        ch.Add("ASK_PRICE", "How much?", "PRICE");
                        ch.Add(GARAGE_CH_LEAVE, "Not interested.", "EXIT");
                    });

                c.AddNode("PRICE",
                    $"The place would run you ${GaragePrice:N0}. Cash. No paper trail.",
                    ch =>
                    {
                        ch.Add(GARAGE_CH_PAY, "I'll take it.", "PAYING");
                        ch.Add(GARAGE_CH_LEAVE, "Too rich for me.", "EXIT");
                    });

                c.AddNode("PAYING",
                    "Keys are yours. Do what you want with it.");

                c.AddNode("NOT_ENOUGH",
                    "Come back when you've got the cash.");

                c.AddNode("EXIT", "");
            });

            Dialogue.OnChoiceSelected(GARAGE_CH_PAY, () =>
            {
                var data = WSSaveData.Instance?.Data;
                if (data == null) return;
                if (data.Properties.Garage.Owned) return;

                float balance = Money.GetCashBalance();
                if (balance < GaragePrice)
                {
                    Dialogue.JumpTo(GARAGE_CONTAINER, "NOT_ENOUGH");
                    return;
                }

                Money.ChangeCashBalance(-GaragePrice, visualizeChange: true, playCashSound: true);
                data.Properties.Garage.Owned = true;

                GarageLoader.LoadGarageAdditiveOnce();
                QuestManager.PurchaseGarage();
                ActivateDefaultDialogue();
                Dialogue.JumpTo(GARAGE_CONTAINER, "PAYING");
                MelonLogger.Msg("[Landlord] Garage purchased for ${0:N0}.", GaragePrice);
            });
        }

        private void ActivateDefaultDialogue()
        {
            RegisterDefaultDialogue();
            Dialogue.UseContainerOnInteract(DEFAULT_CONTAINER);
        }

        private void ActivateGarageDialogue()
        {
            RegisterGarageDialogue();
            Dialogue.UseContainerOnInteract(GARAGE_CONTAINER);
        }

        public static void SetDialogueFromAct3State()
        {
            if (Instance == null) return;

            var quest = QuestManager.GetMovingUpQuest();
            var data = WSSaveData.Instance?.Data;
            bool act3Active = quest != null && quest.Stage >= 1;
            bool garageNotOwned = data != null && !data.Properties.Garage.Owned;

            if (act3Active && garageNotOwned)
                Instance.ActivateGarageDialogue();
            else
                Instance.ActivateDefaultDialogue();
        }

        protected override void OnCreated()
        {
            try
            {
                Instance = this;
                base.OnCreated();
                Appearance.Build();

                Aggressiveness = 2f;
                Region = Region.Northtown;

                SetDialogueFromAct3State();
                Schedule.Enable();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[NorthWarehouseLandlord] OnCreated failed: {ex.Message}");
            }
        }
    }
}
