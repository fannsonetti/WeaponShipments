using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;

using S1API.PhoneApp;
using S1API.UI;
using S1API.Internal.Utils;
using S1API.Internal.Abstraction;
using S1API.Input;

using WeaponShipments.Services; // ShipmentManager, WeaponShipmentSpawner

namespace WeaponShipments.Apps
{
    /// <summary>
    /// Weapon shipment contracts app.
    /// - Shows up to 3 active offers (Pending / Accepted).
    /// - You can only accept 1 at a time until it is delivered.
    /// - Accepting spawns a crate and changes status, but keeps the details visible.
    /// </summary>
    public class WeaponShipmentApp : PhoneApp
    {
        private RectTransform _listContent;
        private GameObject _detailsPanel;

        private Text _gunTypeValue;
        private Text _productFormValue;
        private Text _originValue;
        private Text _destinationValue;
        private Text _statusValue;

        private Button _acceptButton;

        private string _selectedShipmentId = string.Empty;

        protected override string AppName => "WeaponShipmentApp";
        protected override string AppTitle => "Shipments";
        protected override string IconLabel => "Shipments";
        protected override string IconFileName => string.Empty;

        protected override void OnCreated()
        {
            var iconSprite = LoadEmbeddedShipmentIcon();
            if (iconSprite != null)
                SetIconSprite(iconSprite);
        }

        protected override void OnDestroyed()
        {
        }

        protected override void OnCreatedUI(GameObject container)
        {
            // Background + top bar
            var bg = UIFactory.Panel("MainBG", container.transform, Color.black, fullAnchor: true);
            var topBar = UIFactory.TopBar("TopBar", bg.transform, "Weapon Shipments", 0.82f, 75, 75, 0, 35);

            // No "New" button (offers are generated automatically)

            // Left list panel
            var leftPanel = UIFactory.Panel(
                "ShipmentListPanel", bg.transform,
                new Color(0.1f, 0.1f, 0.1f),
                new Vector2(0.02f, 0.05f),
                new Vector2(0.49f, 0.82f)
            );

            _listContent = UIFactory.ScrollableVerticalList("ShipmentListScroll", leftPanel.transform, out _);
            UIFactory.FitContentHeight(_listContent);

            // Right detail panel
            var rightPanel = UIFactory.Panel(
                "DetailPanel", bg.transform,
                new Color(0.12f, 0.12f, 0.12f),
                new Vector2(0.49f, 0f),
                new Vector2(0.98f, 0.82f)
            );
            // Smaller spacing so things are closer together
            UIFactory.VerticalLayoutOnGO(rightPanel, spacing: 6, padding: new RectOffset(24, 50, 15, 40));

            var welcomeText = UIFactory.Text(
                "WelcomeText",
                "Select a shipment from the list to view details.",
                rightPanel.transform,
                18, TextAnchor.MiddleCenter, FontStyle.Italic
            );
            welcomeText.color = new Color(0.7f, 0.7f, 0.7f, 1f);

            _detailsPanel = CreateDetailsPanel(rightPanel.transform);
            _detailsPanel.SetActive(false);

            RefreshList();
        }

        protected override void OnPhoneClosed()
        {
            Controls.IsTyping = false;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private GameObject CreateDetailsPanel(Transform parent)
        {
            var panel = UIFactory.Panel("DetailsPanel", parent, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Compact vertical layout
            UIFactory.VerticalLayoutOnGO(panel, spacing: 6, padding: new RectOffset(16, 16, 16, 16));

            UIFactory.Text("Header", "Shipment Details", panel.transform, 20, TextAnchor.MiddleLeft, FontStyle.Bold);

            // Compact rows: "Label: Value" on a single line
            CreateInfoRow(panel.transform, "Gun Type", out _gunTypeValue);
            CreateInfoRow(panel.transform, "Form", out _productFormValue);
            CreateInfoRow(panel.transform, "Origin", out _originValue);
            CreateInfoRow(panel.transform, "Destination", out _destinationValue);
            CreateInfoRow(panel.transform, "Status", out _statusValue);

            var buttonsRow = UIFactory.ButtonRow("DetailsButtons", panel.transform, spacing: 12f, alignment: TextAnchor.MiddleRight);

            var (_, acceptBtn, _) = UIFactory.RoundedButtonWithLabel(
                "AcceptBtn", "Accept Deal",
                buttonsRow.transform,
                new Color(0.2f, 0.7f, 0.3f),
                140, 36, 16, Color.white
            );
            _acceptButton = acceptBtn;
            ButtonUtils.AddListener(_acceptButton, AcceptSelectedShipment);

            return panel;
        }

        /// <summary>
        /// Creates a compact horizontal row: "Label: Value".
        /// This keeps the value text close to the label (no big vertical gap).
        /// </summary>
        private void CreateInfoRow(Transform parent, string label, out Text valueText)
        {
            var row = UIFactory.Panel(label + "Row", parent, new Color(0, 0, 0, 0));
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 24f);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            UIFactory.Text(label + "Label", label + ":", row.transform, 16, TextAnchor.MiddleLeft, FontStyle.Bold);
            valueText = UIFactory.Text(label + "Value", "-", row.transform, 16, TextAnchor.MiddleLeft);
        }

        private void RefreshList()
        {
            if (_listContent == null)
                return;

            UIFactory.ClearChildren(_listContent);

            IReadOnlyList<ShipmentManager.ShipmentEntry> shipments = ShipmentManager.Instance.GetAllShipments();

            for (int i = 0; i < shipments.Count; i++)
            {
                var shipment = shipments[i];

                var row = UIFactory.CreateQuestRow($"Shipment_{shipment.Id}", _listContent, out var iconPanel, out var textPanel);

                CreateShipmentIcon(iconPanel.transform, shipment.Status == "Accepted");

                string title = $"{shipment.GunType} ({shipment.ProductForm})";
                string subtitle = $"{shipment.Origin} → {shipment.Destination} • {shipment.Status}";

                UIFactory.CreateTextBlock(textPanel.transform, title, subtitle, false);

                var rowBtn = row.GetComponent<Button>();
                ButtonUtils.ClearListeners(rowBtn);
                ButtonUtils.AddListener(rowBtn, () => ShowDetails(shipment.Id));
            }
        }

        private void ShowDetails(string shipmentId)
        {
            var shipment = ShipmentManager.Instance.GetShipment(shipmentId);
            if (shipment == null)
                return;

            _selectedShipmentId = shipmentId;

            _gunTypeValue.text = shipment.GunType;
            _productFormValue.text = shipment.ProductForm;
            _originValue.text = shipment.Origin;
            _destinationValue.text = shipment.Destination;
            _statusValue.text = shipment.Status;

            var welcomeText = _detailsPanel.transform.parent.Find("WelcomeText");
            if (welcomeText != null)
                welcomeText.gameObject.SetActive(false);

            _detailsPanel.SetActive(true);
        }

        /// <summary>
        /// Accept the currently selected shipment, if allowed.
        /// Only one deal can be Accepted at a time (until delivered).
        /// Details stay open; status text updates.
        /// </summary>
        private void AcceptSelectedShipment()
        {
            if (string.IsNullOrEmpty(_selectedShipmentId))
                return;

            var shipment = ShipmentManager.Instance.GetShipment(_selectedShipmentId);
            if (shipment == null)
                return;

            if (shipment.Delivered)
            {
                MelonLogger.Warning("[WeaponShipmentApp] Shipment already delivered.");
                return;
            }

            bool accepted = ShipmentManager.Instance.AcceptShipment(_selectedShipmentId);
            if (!accepted)
            {
                MelonLogger.Warning("[WeaponShipmentApp] Cannot accept: another deal is active.");
                return;
            }

            // 1. Spawn crate
            WeaponShipmentSpawner.SpawnShipmentCrate();

            // 2. Spawn delivery area
            DeliveryAreaSpawner.SpawnDeliveryArea(_selectedShipmentId);

            // Refresh UI
            shipment = ShipmentManager.Instance.GetShipment(_selectedShipmentId);
            if (shipment != null)
                _statusValue.text = shipment.Status;

            RefreshList();
            Saveable.RequestGameSave(true);
        }


        /// <summary>
        /// Loads embedded icon (e.g. shipments.png) from assembly resources.
        /// </summary>
        private Sprite LoadEmbeddedShipmentIcon()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string[] possibleNames =
                {
                    "WeaponShipments.shipments.png",
                    "WeaponShipments.Apps.shipments.png",
                    "shipments.png"
                };

                foreach (string resourceName in possibleNames)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            byte[] data = new byte[stream.Length];
                            stream.Read(data, 0, data.Length);
                            return ImageUtils.LoadImageRaw(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg($"Failed to load embedded shipment icon: {ex.Message}");
            }

            return null;
        }

        private void CreateShipmentIcon(Transform parent, bool accepted)
        {
            var iconGO = new GameObject("ShipmentIcon");
            iconGO.transform.SetParent(parent, false);

            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;

            var iconImage = iconGO.AddComponent<Image>();

            var iconSprite = LoadEmbeddedShipmentIcon();
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
            }
            else
            {
                // Fallback emoji if sprite missing
                iconImage.enabled = false;
                var iconText = UIFactory.Text("ShipmentIconText", "📦", parent, 24, TextAnchor.MiddleCenter, FontStyle.Bold);
                iconText.color = accepted
                    ? new Color(0.4f, 1f, 0.4f, 1f)
                    : new Color(0.8f, 0.8f, 0.8f, 1f);
                var textRT = iconText.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
            }

            iconImage.color = accepted
                ? new Color(0.4f, 1f, 0.4f, 1f)
                : Color.white;
        }
    }
}
