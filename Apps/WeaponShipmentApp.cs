using MelonLoader;
using S1API.Input;
using S1API.Internal.Abstraction;
using S1API.Internal.Utils;
using S1API.PhoneApp;
using S1API.UI;
using S1API.Money;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeaponShipments.Services;
using static MelonLoader.MelonLogger;

namespace WeaponShipments.Apps
{
    /// <summary>
    /// Weapon shipment contracts app.
    /// Status flow:
    ///   Pending     (white)    – button: "Accept Deal" (green)
    ///   In Progress (orange)   – no button
    ///   Completed   (green)    – button: "Finish" (blue) removes shipment
    /// </summary>
    public class WeaponShipmentApp : PhoneApp
    {
        public static WeaponShipmentApp Instance { get; private set; }

        private RectTransform _listContent;
        private GameObject _detailsPanel;

        private Text _gunTypeValue;
        private Text _productFormValue;
        private Text _originValue;
        private Text _destinationValue;
        private Text _statusValue;
        private Text _timerText;

        private Button _actionButton; // reused Accept / Finish

        private string _selectedShipmentId = string.Empty;

        private static readonly Color StatusColorPending = Color.white;
        private static readonly Color StatusColorInProgress = new Color(1f, 0.64f, 0.1f, 1f); // orange
        private static readonly Color StatusColorCompleted = new Color(0.3f, 1f, 0.3f, 1f);   // green

        private static readonly Color ActionColorAccept = new Color(0.2f, 0.7f, 0.3f, 1f); // green
        private static readonly Color ActionColorFinish = new Color(0.2f, 0.4f, 1f, 1f);   // blue

        protected override string AppName => "WeaponShipmentApp";
        protected override string AppTitle => "Shipments";
        protected override string IconLabel => "Shipments";
        protected override string IconFileName => string.Empty;

        protected override void OnCreated()
        {
            Instance = this;

            var iconSprite = LoadEmbeddedShipmentIcon();
            if (iconSprite != null)
                SetIconSprite(iconSprite);
        }

        protected override void OnDestroyed()
        {
            if (Instance == this)
                Instance = null;
        }


        public void OnExternalShipmentChanged(string shipmentId)
        {
            RefreshList();

            if (!string.IsNullOrEmpty(_selectedShipmentId) && _selectedShipmentId == shipmentId)
            {
                var shipment = ShipmentManager.Instance.GetShipment(shipmentId);
                if (shipment != null)
                {
                    _statusValue.text = shipment.Status;
                    UpdateStatusColor(shipment.Status);
                    UpdateActionButtonForStatus(shipment.Status);
                }
            }
        }

        protected override void OnCreatedUI(GameObject container)
        {
            // Background + top bar
            var bg = UIFactory.Panel("MainBG", container.transform, Color.black, fullAnchor: true);
            UIFactory.TopBar("TopBar", bg.transform, "Weapon Shipments", 0.82f, 75, 75, 0, 35);

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

            // Timer text bottom-left on bg
            _timerText = UIFactory.Text(
                "OfferTimerText",
                string.Empty,
                bg.transform,
                14, TextAnchor.LowerLeft
            );
            var timerRT = _timerText.GetComponent<RectTransform>();
            timerRT.anchorMin = new Vector2(0f, 0f);
            timerRT.anchorMax = new Vector2(0f, 0f);
            timerRT.pivot = new Vector2(0f, 0f);
            timerRT.anchoredPosition = new Vector2(10f, 10f);

            var updaterGO = new GameObject("OfferTimerUpdater");
            updaterGO.transform.SetParent(bg.transform, false);
            var updater = updaterGO.AddComponent<OfferTimerUI>();
            updater.Init(_timerText);

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

            UIFactory.VerticalLayoutOnGO(panel, spacing: 6, padding: new RectOffset(16, 16, 16, 16));

            UIFactory.Text("Header", "Shipment Details", panel.transform, 20, TextAnchor.MiddleLeft, FontStyle.Bold);

            CreateInfoRow(panel.transform, "Gun Type", out _gunTypeValue);
            CreateInfoRow(panel.transform, "Form", out _productFormValue);
            CreateInfoRow(panel.transform, "Origin", out _originValue);
            CreateInfoRow(panel.transform, "Destination", out _destinationValue);
            CreateInfoRow(panel.transform, "Status", out _statusValue);

            // Single action button (Accept OR Finish depending on status)
            var buttonsRow = UIFactory.ButtonRow("DetailsButtons", panel.transform, spacing: 12f, alignment: TextAnchor.MiddleRight);

            var (_, actionBtn, _) = UIFactory.RoundedButtonWithLabel(
                "ActionBtn", "Accept Deal",
                buttonsRow.transform,
                ActionColorAccept,
                140, 36, 16, Color.white
            );
            _actionButton = actionBtn;
            ButtonUtils.AddListener(_actionButton, OnActionButtonClicked);

            return panel;
        }

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

                bool inProgress = shipment.Status == "In Progress";
                bool completed = shipment.Status == "Completed";

                // Tint completed rows green
                var rowImage = row.GetComponent<Image>();
                if (rowImage != null)
                {
                    if (completed)
                        rowImage.color = new Color(0.08f, 0.2f, 0.08f, 0.9f);  // dark green tint
                    else
                        rowImage.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
                }

                CreateShipmentIcon(iconPanel.transform, shipment.GunType, inProgress, completed);

                string title = $"{shipment.GunType} x{shipment.Quantity} ({shipment.ProductForm})";
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

            UpdateStatusColor(shipment.Status);
            UpdateActionButtonForStatus(shipment.Status);

            var welcomeText = _detailsPanel.transform.parent.Find("WelcomeText");
            if (welcomeText != null)
                welcomeText.gameObject.SetActive(false);

            _detailsPanel.SetActive(true);
        }

        private void UpdateStatusColor(string status)
        {
            if (_statusValue == null)
                return;

            switch (status)
            {
                case "In Progress":
                    _statusValue.color = StatusColorInProgress;
                    break;
                case "Completed":
                    _statusValue.color = StatusColorCompleted;
                    break;
                default:
                    _statusValue.color = StatusColorPending;
                    break;
            }
        }

        private void UpdateActionButtonForStatus(string status)
        {
            if (_actionButton == null)
                return;

            var text = _actionButton.GetComponentInChildren<Text>();
            var img = _actionButton.GetComponent<Image>();

            switch (status)
            {
                case "In Progress":
                    _actionButton.gameObject.SetActive(false);
                    break;

                case "Completed":
                    _actionButton.gameObject.SetActive(true);
                    if (text != null) text.text = "Finish";
                    if (img != null) img.color = ActionColorFinish;
                    break;

                default: // Pending
                    _actionButton.gameObject.SetActive(true);
                    if (text != null) text.text = "Accept Deal";
                    if (img != null) img.color = ActionColorAccept;
                    break;
            }
        }

        private void OnActionButtonClicked()
        {
            if (string.IsNullOrEmpty(_selectedShipmentId))
                return;

            var shipment = ShipmentManager.Instance.GetShipment(_selectedShipmentId);
            if (shipment == null)
                return;

            switch (shipment.Status)
            {
                case "Pending":
                    HandleAcceptShipment(shipment);
                    break;

                case "Completed":
                    HandleFinishShipment(shipment);
                    break;

                default:
                    break;
            }
        }

        private void HandleAcceptShipment(ShipmentManager.ShipmentEntry shipment)
        {
            if (shipment.Delivered)
            {
                MelonLogger.Warning("[WeaponShipmentApp] Shipment already delivered.");
                return;
            }

            bool accepted = ShipmentManager.Instance.AcceptShipment(shipment.Id);
            if (!accepted)
            {
                MelonLogger.Warning("[WeaponShipmentApp] Cannot accept: another deal is already In Progress.");
                return;
            }

            // Spawn crate + delivery area
            WeaponShipmentSpawner.SpawnShipmentCrate(shipment);
            DeliveryAreaSpawner.SpawnDeliveryArea(shipment.Id);

            // Refresh data and UI
            shipment = ShipmentManager.Instance.GetShipment(shipment.Id);
            if (shipment != null)
            {
                _statusValue.text = shipment.Status;
                UpdateStatusColor(shipment.Status);
                UpdateActionButtonForStatus(shipment.Status);
            }

            RefreshList();
        }

        private void HandleFinishShipment(ShipmentManager.ShipmentEntry shipment)
        {
            // 1) Compute reward: base value * quantity
            float baseReward = ShipmentPayouts.GetReward(shipment.GunType);
            float totalReward = baseReward * Mathf.Max(1, shipment.Quantity);

            // 2) Pay instantly (visual + sound)
            if (totalReward > 0f)
            {
                Money.ChangeCashBalance(totalReward, visualizeChange: true, playCashSound: true);
                MelonLogger.Msg("[WeaponShipmentApp] Paid ${0} for {1} x{2}", totalReward, shipment.GunType, shipment.Quantity);
            }

            // 3) Remove shipment from list
            ShipmentManager.Instance.RemoveShipment(shipment.Id);

            // 4) Clear selection + refresh UI (no save here)
            _selectedShipmentId = string.Empty;
            _detailsPanel.SetActive(false);

            var welcomeText = _detailsPanel.transform.parent.Find("WelcomeText");
            if (welcomeText != null)
                welcomeText.gameObject.SetActive(true);

            RefreshList();
        }


        // ---------------- ICONS ----------------

        private Sprite LoadEmbeddedShipmentIcon()
        {
            // fallback generic icon if per-gun icon is missing
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string[] candidates =
                {
                    "WeaponShipments.Recources.shipments.png",
                    "WeaponShipments.Resources.shipments.png",
                    "shipments.png"
                };

                foreach (var name in candidates)
                {
                    using (var stream = assembly.GetManifestResourceStream(name))
                    {
                        if (stream == null) continue;
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        return ImageUtils.LoadImageRaw(data);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[WeaponShipmentApp] Failed to load generic icon: {ex.Message}");
            }

            return null;
        }

        private static string[] _resourceNames;

        private Sprite LoadGunIcon(string gunType)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                if (_resourceNames == null)
                    _resourceNames = assembly.GetManifestResourceNames();

                // Simplify gun type for matching
                // e.g. "AK-47" -> "ak47", "Baseball Bat" -> "baseballbat"
                string key = gunType
                    .Replace(" ", "")
                    .Replace("-", "")
                    .ToLowerInvariant();

                foreach (var resName in _resourceNames)
                {
                    string lower = resName.ToLowerInvariant();

                    // Only consider things that look like icon images
                    if (!lower.Contains("__icon") || !lower.EndsWith(".png"))
                        continue;

                    // Simplify for match (remove dots/underscores too)
                    string simple = lower
                        .Replace(".", "")
                        .Replace("_", "")
                        .Replace("-", "");

                    if (simple.Contains(key))
                    {
                        using (var stream = assembly.GetManifestResourceStream(resName))
                        {
                            if (stream == null) continue;
                            byte[] data = new byte[stream.Length];
                            stream.Read(data, 0, data.Length);
                            return ImageUtils.LoadImageRaw(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[WeaponShipmentApp] Failed to load gun icon for {gunType}: {ex.Message}");
            }

            return null;
        }


        private void CreateShipmentIcon(Transform parent, string gunType, bool inProgress, bool completed)
        {
            var iconGO = new GameObject("ShipmentIcon");
            iconGO.transform.SetParent(parent, false);

            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;

            var iconImage = iconGO.AddComponent<Image>();

            var iconSprite = LoadGunIcon(gunType) ?? LoadEmbeddedShipmentIcon();
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                if (completed)
                    iconImage.color = StatusColorCompleted;
                else if (inProgress)
                    iconImage.color = StatusColorInProgress;
                else
                    iconImage.color = Color.white;
            }
            else
            {
                iconImage.enabled = false;
                var iconText = UIFactory.Text("ShipmentIconText", "📦", parent, 24, TextAnchor.MiddleCenter, FontStyle.Bold);
                var textRT = iconText.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;

                if (completed)
                    iconText.color = StatusColorCompleted;
                else if (inProgress)
                    iconText.color = StatusColorInProgress;
                else
                    iconText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        // ---------------- TIMER MONOBEH ----------------

        private class OfferTimerUI : MonoBehaviour
        {
            private Text _text;

            public void Init(Text text)
            {
                _text = text;
            }

            private void Update()
            {
                if (_text == null)
                    return;

                TimeSpan remaining;
                bool offersFull;
                if (ShipmentManager.Instance.TryGetTimeToNextOffer(out remaining, out offersFull))
                {
                    if (offersFull)
                    {
                        _text.text = "Offers full (3/3)";
                    }
                    else
                    {
                        if (remaining.TotalSeconds < 0)
                            remaining = TimeSpan.Zero;

                        _text.text = "Next offer in " + remaining.ToString(@"mm\:ss");
                    }
                }
                else
                {
                    _text.text = string.Empty;
                }
            }
        }
    }
}
