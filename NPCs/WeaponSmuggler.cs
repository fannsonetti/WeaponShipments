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
    public sealed class WeaponSmuggler : NPC
    {
        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Vector3 spawnPos = new Vector3(72.263f, -4.535f, 30.9708f);
            builder.WithIdentity("WeaponSmuggler", "Weapon Smuggler", "")
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
                        .WithOrdersPerWeek(1, 4)
                        .WithPreferredOrderDay(Day.Thursday)
                        .WithOrderTime(2300)
                        .WithStandards(CustomerStandard.Low)
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
                    r.WithDelta(2.0f)
                        .SetUnlocked(false)
                        .SetUnlockType(NPCRelationship.UnlockType.DirectApproach)
                        .WithConnectionsById();
                })
                .WithSchedule(plan =>
                {
                });
        }

        public WeaponSmuggler() : base()
        {
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();
                Appearance.Build();

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

