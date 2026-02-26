using MelonLoader;
using S1API.Entities;
using S1API.Messaging;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WeaponShipments.Quests;
using WeaponShipments.Utils;

namespace WeaponShipments.NPCs
{
    public sealed class UnknownContact : NPC
    {
        public static UnknownContact? Instance { get; private set; }
        public override bool IsPhysical => false;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            var icon = QuestIconLoader.Load("unknown_contact.png");
            builder.WithIdentity("ws_unknown_contact", "Unknown", "")
                .WithIcon(icon);
        }

        protected override void OnCreated()
        {
            base.OnCreated();
            Instance = this;
            ConversationCanBeHidden = true;
            MelonCoroutines.Start(ApplyUnknownContactStyleOnce());
        }

        private IEnumerator ApplyUnknownContactStyleOnce()
        {
            yield return new WaitForSeconds(2f);
            try
            {
                ClearConversationCategoriesViaReflection();
                var icon = QuestIconLoader.Load("unknown_contact.png");
                if (icon != null)
                {
                    Icon = icon;
                    RefreshMessagingIcons();
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[UnknownContact] ApplyUnknownContactStyle: {ex.Message}");
            }
        }

        private void ClearConversationCategoriesViaReflection()
        {
            try
            {
                var s1NpcField = typeof(NPC).GetField("S1NPC", BindingFlags.NonPublic | BindingFlags.Instance);
                var s1Npc = s1NpcField?.GetValue(this);
                if (s1Npc != null)
                {
                    var catsField = s1Npc.GetType().GetField("ConversationCategories", BindingFlags.Public | BindingFlags.Instance);
                    var cats = catsField?.GetValue(s1Npc) as IList;
                    cats?.Clear();

                    var msgConvField = s1Npc.GetType().GetField("MSGConversation", BindingFlags.Public | BindingFlags.Instance);
                    var msgConv = msgConvField?.GetValue(s1Npc);
                    if (msgConv != null)
                    {
                        var t = msgConv.GetType();
                        var convCats = t.GetProperty("Categories", BindingFlags.Public | BindingFlags.Instance)?.GetValue(msgConv) as IList
                            ?? t.GetField("Categories", BindingFlags.Public | BindingFlags.Instance)?.GetValue(msgConv) as IList;
                        convCats?.Clear();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[UnknownContact] ClearConversationCategoriesViaReflection: {ex.Message}");
            }
        }

        public void SendIntro()
        {
            var listenResponse = new Response
            {
                Label = "listen_response",
                Text = "I’m listening.",
                OnTriggered = SendMeetup
            };

            var tellResponse = new Response
            {
                Label = "tell_response",
                Text = "Everyone knows a way.",
                OnTriggered = SendMeetup
            };

            SendTextMessage(
                "You looking to make serious money? I know a way. Interested?",
                new[] { listenResponse, tellResponse }
                );
        }
        public void SendMeetup()
        {
            var whoResponse = new Response
            {
                Label = "who_response",
                Text = "Who is this?",
                OnTriggered = SendWho
            };

            var okResponse = new Response
            {
                Label = "ok_response",
                Text = "Don’t waste my time.",
                OnTriggered = () => WeaponShipments.Quests.QuestManager.AgentMeetup(),
            };

            SendTextMessage(
                "Meet me at the Black Market. Come alone.",
                new[] { whoResponse, okResponse }
                );
        }

            public void SendWho()
        {
            SendTextMessage("Thats none of your concern.");
            WeaponShipments.Quests.QuestManager.AgentMeetup();
        }
    }
}
