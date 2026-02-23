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
using System.Reflection;
using UnityEngine;
using WeaponShipments.Data;

namespace WeaponShipments.NPCs
{
    public sealed class Mechanic : NPC
    {
        public override bool IsPhysical => true;
        public static Mechanic Instance { get; private set; }

        private static readonly Vector3 RepaintCamPos = new Vector3(9.4409f, 2.0417f, -12.3973f);
        private static readonly Vector3 RepaintVanPos = new Vector3(4.5729f, 0.6111f, -12.7229f);
        private static readonly Quaternion RepaintVanRot = Quaternion.Euler(0f, 126f, 0f);

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Vector3 spawnPos = new Vector3(-42f, -3.5f, 40f);
            builder.WithIdentity("MechanicWS", "Rusty", "")
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0.0f;
                    av.Height = 1.02f;
                    av.Weight = 0.82f;
                    av.SkinColor = new Color(0.71f, 0.58f, 0.48f);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = Color.white;
                    av.PupilDilation = 0.7f;
                    av.EyebrowScale = 1.2f;
                    av.EyebrowThickness = 1.1f;
                    av.EyebrowRestingHeight = -0.4f;
                    av.EyebrowRestingAngle = 0f;
                    av.LeftEye = (0.3f, 0.4f);
                    av.RightEye = (0.3f, 0.4f);
                    av.HairColor = new Color(0.2f, 0.2f, 0.2f);
                    av.HairPath = "Avatar/Hair/Receding/Receding";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_SmugPout", Color.black);
                    av.WithFaceLayer("Avatar/Layers/Face/Facialhair_Stubble", Color.black);
                    av.WithBodyLayer("Avatar/Layers/Top/T-Shirt", new Color(0.25f, 0.25f, 0.25f));
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.15f, 0.18f, 0.25f));
                    av.WithAccessoryLayer("Avatar/Accessories/Waist/Belt/Belt", new Color(0.35f, 0.25f, 0.15f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/Sneakers/Sneakers", new Color(0.3f, 0.3f, 0.3f));
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
                .WithSchedule(plan => { });
        }

        public Mechanic() : base() { }

        private const string DEFAULT_CONTAINER = "Mechanic_Default";
        private const string REPAINT_CONTAINER = "Mechanic_Repaint";
        private static bool _defaultRegistered;
        private static bool _repaintRegistered;

        private void RegisterDefaultDialogue()
        {
            if (_defaultRegistered) return;
            _defaultRegistered = true;
            Dialogue.BuildAndRegisterContainer(DEFAULT_CONTAINER, c =>
            {
                c.AddNode("ENTRY", "What do you need?", ch =>
                {
                    ch.Add("REPAINT", "Repaint the car.", "REPAINT_OFFER");
                    ch.Add("LEAVE", "Nothing.", "EXIT");
                });
                c.AddNode("REPAINT_OFFER", "Bring it around back. I'll get the booth ready.", ch => ch.Add("OK", "Alright.", "EXIT"));
                c.AddNode("EXIT", "");
            });
        }

        private void RegisterRepaintDialogue()
        {
            if (_repaintRegistered) return;
            _repaintRegistered = true;

            Dialogue.BuildAndRegisterContainer(REPAINT_CONTAINER, c =>
            {
                c.AddNode("ENTRY", "You want a respray?", ch =>
                {
                    ch.Add("YES", "Yeah, repaint the car.", "DO_REPAINT");
                    ch.Add("NO", "Not right now.", "EXIT");
                });
                c.AddNode("DO_REPAINT", "Pull it in.");
                c.AddNode("EXIT", "");
            });

            Dialogue.OnChoiceSelected("YES", () =>
            {
                TryOpenRepaintMenu();
            });
        }

        private void TryOpenRepaintMenu()
        {
            var data = WSSaveData.Instance?.Data;
            if (data == null || !data.Properties.Warehouse.SetupComplete)
            {
                ActivateDefaultDialogue();
                return;
            }

            var equipmentGo = GameObject.Find("equipmentvan") ?? GameObject.Find("equipmentcar");
            if (equipmentGo == null)
            {
                MelonLogger.Warning("[Mechanic] No equipmentvan found.");
                ActivateDefaultDialogue();
                return;
            }

            var root = equipmentGo.transform.root != null ? equipmentGo.transform.root.gameObject : equipmentGo;
            root.transform.position = RepaintVanPos;
            root.transform.rotation = RepaintVanRot;

            var landVehicle = GetLandVehicleFromGameObject(root);
            if (landVehicle == null)
            {
                MelonLogger.Warning("[Mechanic] Could not get LandVehicle from equipmentvan.");
                ActivateDefaultDialogue();
                return;
            }

            var camObj = new GameObject("WeaponShipments_RepaintCam");
            camObj.transform.position = RepaintCamPos;
            var cam = camObj.AddComponent<Camera>();
            cam.enabled = true;

            var vehicleModMenu = FindVehicleModMenu();
            if (vehicleModMenu != null)
            {
                var canvas = vehicleModMenu.GetComponent<UnityEngine.Canvas>();
                if (canvas != null) canvas.enabled = true;

                foreach (var typeName in new[] { "ScheduleOne.UI.VehicleModMenu", "VehicleModMenu" })
                {
                    var menuType = GetTypeByName(typeName);
                    if (menuType == null) continue;
                    var menuComp = vehicleModMenu.GetComponent(menuType);
                    if (menuComp != null)
                    {
                        foreach (var propName in new[] { "currentVehicle", "CurrentVehicle", "Vehicle" })
                        {
                            var prop = menuComp.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                            if (prop != null && prop.CanWrite)
                            {
                                try
                                {
                                    prop.SetValue(menuComp, landVehicle);
                                    MelonLogger.Msg("[Mechanic] Set {0} on VehicleModMenu.", propName);
                                    break;
                                }
                                catch (System.Exception ex)
                                {
                                    MelonLogger.Warning("[Mechanic] Failed to set {0}: {1}", propName, ex.Message);
                                }
                            }
                        }
                        break;
                    }
                }
                vehicleModMenu.SetActive(true);
            }
            else
            {
                MelonLogger.Warning("[Mechanic] VehicleModMenu not found.");
            }

            ActivateDefaultDialogue();
            Dialogue.JumpTo(REPAINT_CONTAINER, "DO_REPAINT");
        }

        private static object GetLandVehicleFromGameObject(GameObject go)
        {
            if (go == null) return null;
            foreach (var name in new[] { "ScheduleOne.Vehicles.LandVehicle", "LandVehicle", "ScheduleOne.Vehicles.LandVehicleBridge" })
            {
                var t = GetTypeByName(name);
                if (t == null) continue;
                var lv = go.GetComponent(t);
                if (lv != null) return lv;
                var child = go.GetComponentInChildren(t, true);
                if (child != null) return child;
            }
            return null;
        }

        private static System.Type GetTypeByName(string name)
        {
            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(name);
                if (t != null) return t;
            }
            return null;
        }

        private static GameObject FindVehicleModMenu()
        {
            var byName = GameObject.Find("VehicleModMenu");
            if (byName != null) return byName;
            var ui = GameObject.Find("UI");
            if (ui == null) return null;
            var child = ui.transform.Find("VehicleModMenu");
            if (child != null) return child.gameObject;
            foreach (Transform t in ui.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.name.IndexOf("VehicleMod", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return t.gameObject;
            return null;
        }

        private void ActivateDefaultDialogue()
        {
            RegisterDefaultDialogue();
            Dialogue.UseContainerOnInteract(DEFAULT_CONTAINER);
        }

        private void ActivateRepaintDialogue()
        {
            RegisterRepaintDialogue();
            Dialogue.UseContainerOnInteract(REPAINT_CONTAINER);
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

                var data = WSSaveData.Instance?.Data;
                if (data != null && data.Properties.Warehouse.SetupComplete)
                    ActivateRepaintDialogue();
                else
                    ActivateDefaultDialogue();

                Schedule.Enable();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[Mechanic] OnCreated failed: {ex.Message}");
            }
        }
    }
}
