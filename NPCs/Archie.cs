using MelonLoader;
using S1API.Entities;
using S1API.Entities.Schedule;
using S1API.Map;
using S1API.Map.ParkingLots;
using S1API.Money;
using S1API.Economy;
using S1API.Entities.NPCs.Northtown;
using S1API.GameTime;
using S1API.Growing;
using S1API.Map.Buildings;
using S1API.Products;
using S1API.Properties;
using S1API.Vehicles;
using UnityEngine;
using System.Linq;

namespace CustomNPCTest.NPCs
{
    /// <summary>
    /// An example S1API NPC that opts into a physical rig.
    /// Demonstrates movement and inventory usage.
    /// </summary>
    public sealed class ExamplePhysicalNPC1 : NPC
    {
        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            var manorParking = ParkingLotRegistry.Get<ManorParking>();
            var northApartments = Building.Get<NorthApartments>();
            MelonLogger.Msg("Configuring prefab for NPC 1");
            Vector3 posA = new Vector3(-28.060f, 1.065f, 62.070f);
            Vector3 spawnPos = new Vector3(-53.5701f, 1.065f, 67.7955f);
            builder.WithIdentity("ArchieWS", "Archie", "")
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0.0f;
                    av.Height = 1.0f;
                    av.Weight = 0.68f;
                    var skinColor = new Color(0.61f, 0.49f, 0.39f);
                    av.SkinColor = skinColor;
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = Color.white;
                    av.PupilDilation = 0.75f;
                    av.EyebrowScale = 1.22f;
                    av.EyebrowThickness = 1.45f;
                    av.EyebrowRestingHeight = -0.3f;
                    av.EyebrowRestingAngle = 1.75f;
                    av.LeftEye = (0.18f, 0.5f);
                    av.RightEye = (0.18f, 0.5f);
                    av.HairColor = new Color(0.31f, 0.2f, 0.12f);
                    av.HairPath = "Avatar/Hair/Peaked/Peaked";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_NeutralPout", Color.black);
                    av.WithFaceLayer("Avatar/Layers/Face/FacialHair_Goatee", new Color(0.31f, 0.2f, 0.12f));
                    av.WithBodyLayer("Avatar/Layers/Top/Tucked T-Shirt", new Color(0.151f, 0.151f, 0.151f));
                    av.WithBodyLayer("Avatar/Layers/Bottom/CargoPants", new Color(0.151f, 0.151f, 0.151f));
                    av.WithAccessoryLayer("Avatar/Accessories/Waist/Belt/Belt", new Color(0.151f, 0.151f, 0.151f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/CombatBoots/CombatBoots", new Color(0.151f, 0.151f, 0.151f));
                    av.WithAccessoryLayer("Avatar/Accessories/Head/Oakleys/Oakleys", new Color(0.151f, 0.151f, 0.151f));
                })
                .WithSpawnPosition(spawnPos)
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(1.5f)
                        .SetUnlocked(false)
                        .SetUnlockType(NPCRelationship.UnlockType.DirectApproach)
                        .WithConnections<KyleCooley, LudwigMeyer, AustinSteiner>();
                })
                .WithSchedule(plan =>
                {
                    plan.EnsureDealSignal()
                        .UseVendingMachine(900)
                        .WalkTo(posA, 925, faceDestinationDir: true)
                        .StayInBuilding(northApartments, 1100)
                        .LocationDialogue(posA, 1300)
                        .UseVendingMachine(1400)
                        .StayInBuilding(northApartments, 1425, 60)
                        // .DriveToCarParkByName(ParkingLots.Get<ManorParking>().GameObjectName, "shitbox", 1500, ParkingAlignment.FrontToKerb);
                        // .DriveToCarPark(ParkingLots.Get<ManorParking>(), new LandVehicle("shitbox"), 1500);
                        .DriveToCarParkWithCreateVehicle(manorParking.GameObjectName, "cheetah",
                            1550, new Vector3(-66.189f, -3.025f, 124.795f), Quaternion.Euler(0f, 90f, 0f), ParkingAlignment.FrontToKerb);
                })
                .WithInventoryDefaults(inv =>
                {
                    // Startup items that will always be in inventory when spawned
                    inv.WithStartupItems("banana", "baseballbat", "cuke")
                        // Random cash between $50 and $500
                        .WithRandomCash(min: 50, max: 500)
                        // Preserve inventory across sleep cycles
                        .WithClearInventoryEachNight(false);
                });
        }

        /*
        public ExamplePhysicalNPC1() : base()
        {
        }
        */

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();
                Appearance.Build();

                SendTextMessage("Hello from physical NPC 1!");

                Dialogue.BuildAndSetDatabase(db => {
                    db.WithModuleEntry("Reactions", "GREETING", "Welcome.");
                });
                Dialogue.BuildAndRegisterContainer("AlexShop", c => {
                    c.AddNode("ENTRY", "Want some info for $100?", ch => {
                        ch.Add("PAY_FOR_INFO", "Pay $100", "INFO_NODE")
                            .Add("NO_THANKS", "No thanks", "EXIT");
                    });
                    c.AddNode("INFO_NODE", "Get scammed nerd.", ch => {
                        ch.Add("BYE", "Thanks", "EXIT");
                    });
                    c.AddNode("NOT_ENOUGH", "You don't have enough cash.", ch => {
                        ch.Add("BACK", "I'll come back.", "ENTRY");
                    });
                    c.AddNode("EXIT", "See you.");
                });

                Dialogue.OnChoiceSelected("PAY_FOR_INFO", () =>
                {
                    const float price = 100f;
                    var balance = Money.GetCashBalance();
                    if (balance >= price)
                    {
                        Money.ChangeCashBalance(-price, visualizeChange: true, playCashSound: true);
                        Dialogue.JumpTo("AlexShop", "INFO_NODE");
                    }
                    else
                    {
                        Dialogue.JumpTo("AlexShop", "NOT_ENOUGH");
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