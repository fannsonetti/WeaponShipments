using S1API.Entities;
using S1API.Messaging;
using ScheduleOne.NPCs.CharacterClasses;
using System.Net;
using UnityEngine;

public sealed class  UnknownContact : NPC
{
    public static UnknownContact? Instance { get; private set; }
    public override bool IsPhysical => false;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity("ws_unknown_contact", "Unknown", "Contact")
               .WithIcon(null);
    }

    protected override void OnCreated()
    {
        base.OnCreated();
        Instance = this;
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
            OnTriggered = () => Act0ContactQuestManager.AgentMeetup(),
        };

        SendTextMessage(
            "Meet me at the Black Market. Come alone.",
            new[] { whoResponse, okResponse }
            );
    }

        public void SendWho()
    {
        SendTextMessage("Thats none of your concern.");
        Act0ContactQuestManager.AgentMeetup();
    }
}
