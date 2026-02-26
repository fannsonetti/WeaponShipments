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
using ScheduleOne.Audio;
using System.Linq;
using UnityEngine;
using WeaponShipments.Data;
using WeaponShipments.NPCs;
using WeaponShipments.Quests;
using static MelonLoader.MelonLogger;

namespace CustomNPCTest.NPCs
{
    /// <summary>
    /// An example S1API NPC that opts into a physical rig.
    /// Demonstrates movement and inventory usage.
    /// </summary>
    public sealed class Archie : NPC
    {
        public override bool IsPhysical => true;
        public static Archie Instance { get; private set; }


        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            var manorParking = ParkingLotRegistry.Get<ManorParking>();
            var northApartments = Building.Get<NorthApartments>();
            MelonLogger.Msg("Configuring prefab for NPC 1");
            Vector3 posA = new Vector3(-28.060f, 1.065f, 62.070f);
            Vector3 spawnPos = new Vector3(0f, 500f, 0f);
            builder.WithIdentity("ArchieWS", "Archie", "")
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0.0f;
                    av.Height = 1.07f;
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
                    av.WithFaceLayer("Avatar/Layers/Face/OldPersonWrinkles", new Color(0f, 0f, 0f, 0.5f));
                    av.WithBodyLayer("Avatar/Layers/Top/Tucked T-Shirt", new Color(0.151f, 0.151f, 0.151f));
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.296f, 0.296f, 0.296f));
                    av.WithBodyLayer("Avatar/Layers/Accessories/FingerlessGloves", new Color(0.1205f, 0.1205f, 0.1205f));
                    av.WithAccessoryLayer("Avatar/Accessories/Waist/Belt/Belt", new Color(0.481f, 0.331f, 0.225f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/CombatBoots/CombatBoots", new Color(0.201f, 0.201f, 0.201f));
                    av.WithAccessoryLayer("Avatar/Accessories/Head/Oakleys/Oakleys", new Color(0.151f, 0.151f, 0.151f));
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

        public Archie() : base()
        {
        }

        private const string DEFAULT_CONTAINER = "Archie_DefaultBusy";

        private static bool _defaultDialogueRegistered = false;
        private static bool _meetupDialogueRegistered = false;
        private static bool _unpackingDialogueRegistered = false;

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

        private const string MOVINGUP_CONTAINER = "Archie_MovingUp";
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
                    "More space means more throughput. I'll help get things running once the equipment's in place.",
                    ch => ch.Add("OK", "Alright.", "EXIT"));
                c.AddNode("EXIT", "");
            });
            Dialogue.OnChoiceSelected("OK", () =>
            {
                var quest = QuestManager.GetMovingUpQuest();
                if (quest != null && quest.QuestEntries.Count >= 3)
                    quest.QuestEntries[2].Complete();
                ActivateDefaultDialogue();
            });
        }

        private void ActivateMovingUpDialogue()
        {
            RegisterMovingUpDialogue();
            Dialogue.UseContainerOnInteract(MOVINGUP_CONTAINER);
        }

        public static void SetDialogueFromUnpackingState()
        {
            if (Instance == null) return;

            var movingUp = QuestManager.GetMovingUpQuest();
            if (movingUp != null && movingUp.Stage >= 2)
            {
                var entries = movingUp.QuestEntries;
                if (entries != null && entries.Count >= 3)
                {
                    SetMovingUpDialogueActive();
                    return;
                }
            }

            var quest = QuestManager.GetUnpackingQuest();
            if (quest == null)
            {
                Instance.ActivateDefaultDialogue();
                return;
            }
            if (quest.Stage == 1)
                Instance.ActivateUnpackingIntroDialogue();
            else if (quest.Stage == 5)
                Instance.ActivateUnpackingSellBriefingDialogue();
            else
                Instance.ActivateDefaultDialogue();
        }

        private const string ACT0_CONTAINER = "Act0_Archie_FirstMeet";
        private const string ACT0_CH_PAYNOW = "ACT0_PAY_NOW";
        private const string ACT0_CH_NOTYET = "ACT0_NOT_YET";
        private const string ACT0_CH_LEAVE = "ACT0_LEAVE";

        private static int ACT0_SIGNING_BONUS => BusinessConfig.SigningBonus;

        private void RegisterMeetupDialogue()
        {
            if (_meetupDialogueRegistered)
                return;

            _meetupDialogueRegistered = true;

            try
            {
                int signingBonus = BusinessConfig.SigningBonus;

                Dialogue.BuildAndRegisterContainer(ACT0_CONTAINER, c =>
                {
                    {
                        c.AddNode("ENTRY",
                            "So you’re the one Manny mentioned.",
                            ch => { ch.Add("ACT0_CONTINUE", "...", "SEPARATION_1");
                            });

                        c.AddNode("SEPARATION_1",
                            "You don’t look like someone who panics.\n" +
                            "That helps.",
                            ch => ch.Add("ACT0_CONTINUE3", "What do you need to start?", "SEPARATION_2"));

                        c.AddNode("SEPARATION_2",
                            "A workplace, supplies and equipment",
                            ch => ch.Add("ACT0_CONTINUE4", "How do i get the equipment?", "SEPARATION_3"));

                        c.AddNode("SEPARATION_3",
                            "The Benzies should be getting new equipment today.\n" +
                            "They usually transport them in green Veepers",
                            ch => ch.Add("ACT0_CONTINUE5", "Where would i find them?", "SEPARATION_4"));

                        c.AddNode("SEPARATION_4",
                            "The Veepers should be ready in the Manor at 2:00",
                            ch => ch.Add("ACT0_CONTINUE6", "I can get in no problem.", "PAYMENT_1"));

                        c.AddNode("PAYMENT_1",
                            $"Ok good. but i dont work for free, i need ${ACT0_SIGNING_BONUS:N0} upfront",
                            ch =>
                            {
                                ch.Add(ACT0_CH_PAYNOW, "Pay now.", "COMPLETE");
                                ch.Add(ACT0_CH_NOTYET, "Not yet.", "NOTYET");
                            });

                        c.AddNode("NOT_ENOUGH",
                            "Then you’re not ready.\n" +
                            "Come back when you have the cash. Don’t waste my time.");

                        c.AddNode("NOTYET",
                            "Then we’re done here.\n" +
                            "When you’re ready to move, you know where to find me.");

                        c.AddNode("COMPLETE",
                            "The next time you see me i'll be working in the warehouse.");
                    }
                });

                Dialogue.OnChoiceSelected(ACT0_CH_PAYNOW, () =>
                {
                    int wPrice = BusinessConfig.SigningBonus;

                    float balance = Money.GetCashBalance();
                    if (balance < wPrice)
                    {
                        Dialogue.JumpTo(ACT0_CONTAINER, "NOT_ENOUGH");
                        return;
                    }

                    Money.ChangeCashBalance(-wPrice, visualizeChange: true, playCashSound: true);

                    QuestManager.CompleteHireArchie();

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

        private const string UNPACKING_CONTAINER = "Archie_Unpacking";
        private const string UNPACKING_CH_GOTIT = "UNPACKING_GOT_IT";

        private void RegisterUnpackingDialogue()
        {
            if (_unpackingDialogueRegistered) return;
            _unpackingDialogueRegistered = true;

            Dialogue.BuildAndRegisterContainer(UNPACKING_CONTAINER, c =>
            {
                c.AddNode("ENTRY",
                    "First thing – supplies. You need them to run production.",
                    ch => ch.Add("U1_NEXT", "Go on.", "STEAL_1"));
                c.AddNode("STEAL_1",
                    "Use the app to steal. Pick a spot, grab the crate, bring it back here.",
                    ch => ch.Add("U1_NEXT", "And then?", "PRODUCTION"));
                c.AddNode("PRODUCTION",
                    "Production runs automatic. Supplies turn into stock. Nothing you need to do.",
                    ch => ch.Add("U1_NEXT", "Selling?", "SELL_1"));
                c.AddNode("SELL_1",
                    "When you have stock, sell through the app. They send a van. You load it, deliver to the drop.",
                    ch =>
                    {
                        ch.Add(UNPACKING_CH_GOTIT, "Got it.", "EXIT");
                    });
                c.AddNode("EXIT", "");
            });

            Dialogue.OnChoiceSelected(UNPACKING_CH_GOTIT, () =>
            {
                QuestManager.GetUnpackingQuest()?.AdvanceFromTalkToArchie();
                SetDialogueFromUnpackingState();
            });
        }

        private void ActivateUnpackingIntroDialogue()
        {
            RegisterUnpackingDialogue();
            Dialogue.UseContainerOnInteract(UNPACKING_CONTAINER);
        }

        private const string UNPACKING_SELL_CONTAINER = "Archie_UnpackingSell";
        private const string UNPACKING_SELL_CH_GOTIT = "UNPACKING_SELL_GOT_IT";

        private static bool _unpackingSellDialogueRegistered;

        private void RegisterUnpackingSellDialogue()
        {
            if (_unpackingSellDialogueRegistered) return;
            _unpackingSellDialogueRegistered = true;

            Dialogue.BuildAndRegisterContainer(UNPACKING_SELL_CONTAINER, c =>
            {
                c.AddNode("ENTRY",
                    "You’ve got stock. Time to move it.",
                    ch => ch.Add("S1_NEXT", "How?", "SELL_2"));
                c.AddNode("SELL_2",
                    "Open the app, hit Sell Stock. They’ll send a van with a load point.",
                    ch => ch.Add("S1_NEXT", "Then?", "SELL_3"));
                c.AddNode("SELL_3",
                    "Fill it, drive to the drop. Drop the load, get paid.",
                    ch =>
                    {
                        ch.Add(UNPACKING_SELL_CH_GOTIT, "Alright.", "EXIT");
                    });
                c.AddNode("EXIT", "");
            });

            Dialogue.OnChoiceSelected(UNPACKING_SELL_CH_GOTIT, () =>
            {
                QuestManager.GetUnpackingQuest()?.AdvanceFromSellBriefing();
            });
        }

        private void ActivateUnpackingSellBriefingDialogue()
        {
            RegisterUnpackingSellDialogue();
            Dialogue.UseContainerOnInteract(UNPACKING_SELL_CONTAINER);
        }

        private const string GO_NAME = "ArchieWS";

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
                ConversationCanBeHidden = true;

                base.OnCreated();
                RenameSpawnedGameObject();
                Appearance.Build();
                SetDialogueFromUnpackingState();

                Aggressiveness = 1f;
                Region = Region.Northtown;

                Schedule.Enable();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Archie OnCreated failed: {ex.Message}");
                MelonLogger.Error($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}