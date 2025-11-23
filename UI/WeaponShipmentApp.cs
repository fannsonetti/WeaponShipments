using MelonLoader;
using S1API.Input;
using S1API.Internal.Abstraction;
using S1API.Internal.Utils;
using S1API.Money;
using S1API.PhoneApp;
using S1API.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WeaponShipments.Data;
using WeaponShipments.Logic;
using WeaponShipments.NPCs;
using WeaponShipments.Services;
using static MelonLoader.MelonLogger;

namespace WeaponShipments.UI
{
    public class WeaponShipmentApp : PhoneApp
    {
        public static WeaponShipmentApp Instance { get; private set; }

        private static bool _conversionRoutineStarted;
        private static bool _raidRoutineStarted;

        private GameObject _loginPanel;
        private GameObject _homePanel;

        private GameObject _homePage;
        private GameObject _resupplyPage;
        private GameObject _researchPage;
        private GameObject _sellStockPage;
        private GameObject _upgradesPage;

        private GameObject _alertPanel;
        private Text _alertMainText;
        private Text _alertDetailText;


        protected override string AppName => "WeaponShipmentApp";
        protected override string AppTitle => "Disruption Logistics";
        protected override string IconLabel => "Logistics";
        protected override string IconFileName => string.Empty;

        // Status bar UI references (all pages)
        private readonly List<RectTransform> _stockLevelFills = new();
        private readonly List<Text> _stockLevelTexts = new();

        private readonly List<RectTransform> _suppliesLevelFills = new();
        private readonly List<Text> _suppliesLevelTexts = new();

        // Shared Business Status labels across all pages
        private readonly List<Text> _statusLabels = new();

        // Sell button label refs so we can update the prices dynamically
        private Text _sellHylandLabel;
        private Text _sellSerenaLabel;

        private bool _equipmentOwned;
        private bool _staffOwned;
        private bool _securityOwned;

        private Text _equipmentPriceLabel;
        private Text _staffPriceLabel;
        private Text _securityPriceLabel;

        private Text _totalEarningsValueText;
        private Text _totalSalesValueText;

        private Text _resupplyStatText;
        private Text _sellHylandStatText;
        private Text _sellSerenaStatText;
        private Text _stockManufacturedStatText;

        protected override void OnCreated()
        {
            Instance = this;

            if (!_conversionRoutineStarted)
            {
                MelonCoroutines.Start(SuppliesToStockRoutine());
                _conversionRoutineStarted = true;
            }
            if (!_raidRoutineStarted)
            {
                MelonCoroutines.Start(RaidRoutine());
                _raidRoutineStarted = true;
            }
        }

        protected override void OnDestroyed()
        {
            if (Instance == this)
                Instance = null;
        }

        protected override void OnCreatedUI(GameObject container)
        {
            try
            {
                var iconSprite = LoadEmbeddedIcon();
                if (iconSprite != null)
                    SetIconSprite(iconSprite);

                // Login root
                _loginPanel = UIFactory.Panel(
                    "LoginPanel",
                    container.transform,
                    new Color(0.1f, 0.1f, 0.1f),
                    fullAnchor: true
                );
                BuildLoginUI(_loginPanel.transform);

                // Main/home root
                _homePanel = UIFactory.Panel(
                    "HomePanel",
                    container.transform,
                    new Color(0.08f, 0.08f, 0.08f),
                    fullAnchor: true
                );
                _homePanel.SetActive(false);
                BuildMainUI(_homePanel.transform);
            }
            catch (Exception ex)
            {
                Error($"[WeaponShipmentApp] UI build failed: {ex}");
            }
        }

        protected override void OnPhoneClosed()
        {
            if (_loginPanel != null) _loginPanel.SetActive(true);
            if (_homePanel != null) _homePanel.SetActive(false);

            Controls.IsTyping = false;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void BuildLoginUI(Transform parent)
        {
            // SAME AS BEFORE: overall login background
            var bg = UIFactory.Panel(
                "LoginBG",
                parent,
                new Color(0.1f, 0.1f, 0.1f),
                fullAnchor: true
            );

            // SAME SIZE AS BEFORE: center cube
            var cube = UIFactory.Panel(
                "LoginCube",
                bg.transform,
                new Color(0.08f, 0.08f, 0.08f),
                new Vector2(0.3f, 0.3f),
                new Vector2(0.7f, 0.7f)
            );

            // --- TOP BAR INSIDE CUBE (like alert) ---
            var topBar = UIFactory.Panel(
                "LoginTopBar",
                cube.transform,
                new Color(0.05f, 0.05f, 0.05f, 1f) // dark grey, opaque
            );
            var topRT = topBar.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0f, 0.65f);
            topRT.anchorMax = new Vector2(1f, 1f);
            topRT.offsetMin = Vector2.zero;
            topRT.offsetMax = Vector2.zero;

            // Same logo as before, just lives in the top bar now
            CreateBrandingLogo(topBar.transform, 40, 24);

            // Red line under the bar
            var divider = UIFactory.Panel(
                "LoginDivider",
                cube.transform,
                new Color(0.7f, 0.05f, 0.05f, 1f)
            );
            var divRT = divider.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0f, 0.63f);
            divRT.anchorMax = new Vector2(1f, 0.65f);
            divRT.offsetMin = Vector2.zero;
            divRT.offsetMax = Vector2.zero;

            // --- CONTENT AREA (just the button, no extra text) ---
            var buttonRow = UIFactory.ButtonRow(
                "LoginButtonRow",
                cube.transform,
                spacing: 0,
                alignment: TextAnchor.MiddleCenter
            );
            var brt = buttonRow.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0.1f);
            brt.anchorMax = new Vector2(1f, 0.3f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            var (_, enterButton, enterLabel) = UIFactory.RoundedButtonWithLabel(
                "EnterButton",
                "Click To Enter",                     // same text as your original
                buttonRow.transform,
                new Color(0.8f, 0.1f, 0.1f),         // original bright red
                240,
                60,
                24,
                Color.white
            );

            if (enterLabel != null)
                enterLabel.alignment = TextAnchor.MiddleCenter;

            ButtonUtils.AddListener(enterButton, OnLoginClicked);
        }

        private void OnLoginClicked()
        {
            if (_loginPanel != null) _loginPanel.SetActive(false);
            if (_homePanel != null) _homePanel.SetActive(true);
            SetActivePage(_homePage);
            UpdateBars();
        }

        // ---------------- ALERT BAR ----------------

        private void BuildAlertUI(Transform parent)
        {
            // Fullscreen root (completely invisible but NOT transparent UI-wise)
            _alertPanel = UIFactory.Panel(
                "AlertPanel",
                parent,
                new Color(0, 0, 0, 0),
                fullAnchor: true
            );
            _alertPanel.SetActive(false);

            // Center modal, fully OPAQUE black
            var modal = UIFactory.Panel(
                "AlertModal",
                _alertPanel.transform,
                new Color(0.05f, 0.05f, 0.05f, 1f) // full opaque
            );
            var modalRT = modal.GetComponent<RectTransform>();
            modalRT.anchorMin = new Vector2(0.2f, 0.25f);
            modalRT.anchorMax = new Vector2(0.8f, 0.65f);
            modalRT.offsetMin = Vector2.zero;
            modalRT.offsetMax = Vector2.zero;

            // === TOP BAR (dark grey, opaque) ===
            var topBar = UIFactory.Panel(
                "AlertTopBar",
                modal.transform,
                new Color(0.07f, 0.07f, 0.07f, 1f) // dark grey, NOT transparent
            );
            var tbRT = topBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0f, 0.75f);
            tbRT.anchorMax = new Vector2(1f, 1f);
            tbRT.offsetMin = Vector2.zero;
            tbRT.offsetMax = Vector2.zero;

            CreateBrandingLogo(topBar.transform, 22, 16);

            // === RED DIVIDER LINE ===
            var divider = UIFactory.Panel(
                "AlertDivider",
                modal.transform,
                new Color(0.7f, 0.05f, 0.05f, 1f)
            );
            var divRT = divider.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0f, 0.72f);
            divRT.anchorMax = new Vector2(1f, 0.74f);
            divRT.offsetMin = Vector2.zero;
            divRT.offsetMax = Vector2.zero;

            // === Big main text ===
            _alertMainText = UIFactory.Text(
                "AlertMainText",
                string.Empty,
                modal.transform,
                30,
                TextAnchor.UpperCenter,
                FontStyle.Bold
            );
            _alertMainText.color = Color.white;
            var mainRT = _alertMainText.GetComponent<RectTransform>();
            mainRT.anchorMin = new Vector2(0.1f, 0.40f);
            mainRT.anchorMax = new Vector2(0.9f, 0.68f);
            mainRT.offsetMin = Vector2.zero;
            mainRT.offsetMax = Vector2.zero;

            // === Detail text ===
            _alertDetailText = UIFactory.Text(
                "AlertDetailText",
                string.Empty,
                modal.transform,
                22,
                TextAnchor.UpperCenter,
                FontStyle.Normal
            );
            _alertDetailText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            var detailRT = _alertDetailText.GetComponent<RectTransform>();
            detailRT.anchorMin = new Vector2(0.1f, 0.12f);
            detailRT.anchorMax = new Vector2(0.9f, 0.38f);
            detailRT.offsetMin = Vector2.zero;
            detailRT.offsetMax = Vector2.zero;
        }

        public static void ShowAlertStatic(string message, bool isError = false)
        {
            // Backwards-compatible: only main text
            var app = Instance;
            if (app != null)
                app.ShowAlert(message, string.Empty, isError);
        }

        public static void ShowAlertStatic(string main, string detail, bool isError)
        {
            var app = Instance;
            if (app != null)
                app.ShowAlert(main, detail, isError);
        }

        private void ShowAlert(string main, string detail, bool isError)
        {
            if (_alertPanel == null)
                return;

            if (_alertMainText != null)
                _alertMainText.text = main ?? string.Empty;

            if (_alertDetailText != null)
                _alertDetailText.text = detail ?? string.Empty;

            // Optional: tint main text red-ish on errors
            if (_alertMainText != null)
                _alertMainText.color = isError
                    ? new Color(1f, 0.4f, 0.4f)
                    : Color.white;

            _alertPanel.SetActive(true);

            MelonCoroutines.Start(AlertAutoHideRoutine(3.5f));
        }

        private static IEnumerator AlertAutoHideRoutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            var app = Instance;
            if (app == null || app._alertPanel == null)
                yield break;

            app._alertPanel.SetActive(false);
        }

        // ---------------- MAIN / NAV + CONTENT ----------------
        private void BuildMainUI(Transform parent)
        {
            var bg = UIFactory.Panel(
                "MainBG",
                parent,
                new Color(0.06f, 0.06f, 0.06f),
                fullAnchor: true
            );

            // Top bar
            var topBar = UIFactory.Panel(
                "TopBar",
                bg.transform,
                new Color(0.07f, 0.07f, 0.07f),
                new Vector2(0f, 0.9f),
                new Vector2(1f, 1f)
            );
            var topRT = topBar.GetComponent<RectTransform>();
            topRT.offsetMin = Vector2.zero;
            topRT.offsetMax = Vector2.zero;

            // Branding left
            var brandingArea = UIFactory.Panel(
                "BrandingArea",
                topBar.transform,
                new Color(0, 0, 0, 0)
            );
            var brandingRT = brandingArea.GetComponent<RectTransform>();
            brandingRT.anchorMin = new Vector2(0f, 0f);
            brandingRT.anchorMax = new Vector2(0.55f, 1f);
            brandingRT.offsetMin = new Vector2(10f, 0f);
            brandingRT.offsetMax = new Vector2(0f, 0f);

            CreateBrandingLogo(brandingArea.transform, 26, 16);

            // Logged in text right
            var userText = UIFactory.Text(
                "UserLabel",
                "LOGGED IN AS: user",
                topBar.transform,
                14,
                TextAnchor.MiddleRight,
                FontStyle.Normal
            );
            var userRT = userText.GetComponent<RectTransform>();
            userRT.anchorMin = new Vector2(0.55f, 0f);
            userRT.anchorMax = new Vector2(0.98f, 1f);
            userRT.offsetMin = new Vector2(0f, 0f);
            userRT.offsetMax = new Vector2(-10f, 0f);
            userText.color = new Color(0.9f, 0.9f, 0.9f);

            // Left nav
            var navPanel = UIFactory.Panel(
                "NavPanel",
                bg.transform,
                new Color(0.09f, 0.09f, 0.09f),
                new Vector2(0f, 0f),
                new Vector2(0.24f, 0.9f)
            );
            var navRT = navPanel.GetComponent<RectTransform>();
            navRT.offsetMin = Vector2.zero;
            navRT.offsetMax = Vector2.zero;

            UIFactory.VerticalLayoutOnGO(
                navPanel,
                spacing: 8,
                padding: new RectOffset(10, 10, 10, 10)
            );

            var navLayout = navPanel.GetComponent<VerticalLayoutGroup>();
            if (navLayout != null)
            {
                navLayout.childControlHeight = true;
                navLayout.childForceExpandHeight = true;
            }

            CreateLocationCard(navPanel.transform);

            CreateNavButton(navPanel.transform, "Home", ShowHomePage);
            CreateNavButton(navPanel.transform, "Resupply", ShowResupplyPage);
            CreateNavButton(navPanel.transform, "Research", ShowResearchPage);
            CreateNavButton(navPanel.transform, "Sell Stock", ShowSellStockPage);
            CreateNavButton(navPanel.transform, "Buy Upgrades", ShowUpgradesPage);

            // Content area
            var contentArea = UIFactory.Panel(
                "ContentArea",
                bg.transform,
                new Color(0.05f, 0.05f, 0.05f),
                new Vector2(0.24f, 0f),
                new Vector2(1f, 0.9f)
            );
            var contentRT = contentArea.GetComponent<RectTransform>();
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            _homePage = UIFactory.Panel(
                "HomePage",
                contentArea.transform,
                new Color(0, 0, 0, 0),
                fullAnchor: true
            );
            _resupplyPage = UIFactory.Panel(
                "ResupplyPage",
                contentArea.transform,
                new Color(0, 0, 0, 0),
                fullAnchor: true
            );
            _researchPage = UIFactory.Panel(
                "ResearchPage",
                contentArea.transform,
                new Color(0, 0, 0, 0),
                fullAnchor: true
            );
            _sellStockPage = UIFactory.Panel(
                "SellStockPage",
                contentArea.transform,
                new Color(0, 0, 0, 0),
                fullAnchor: true
            );
            _upgradesPage = UIFactory.Panel(
                "UpgradesPage",
                contentArea.transform,
                new Color(0, 0, 0, 0),
                fullAnchor: true
            );

            _resupplyPage.SetActive(false);
            _researchPage.SetActive(false);
            _sellStockPage.SetActive(false);
            _upgradesPage.SetActive(false);

            // Build each page UI
            BuildHomePageUI(_homePage.transform);
            BuildResupplyPageUI(_resupplyPage.transform);
            BuildPlaceholderPage(_researchPage.transform, "Research");

            // Make Sell + Upgrades resilient so one failing doesn't kill the other
            try
            {
                BuildSellStockPageUI(_sellStockPage.transform);
            }
            catch (Exception ex)
            {
                Error($"[WeaponShipmentApp] BuildSellStockPageUI failed: {ex}");
            }

            try
            {
                BuildUpgradesPageUI(_upgradesPage.transform);
            }
            catch (Exception ex)
            {
                Error($"[WeaponShipmentApp] BuildUpgradesPageUI failed: {ex}");
            }

            // Global alert bar (sits above the content pages)
            BuildAlertUI(bg.transform);
            UpdateBars();
        }

        public void UpdateBars()
        {
            // STOCK
            float stockPct = 0f;
            if (BusinessConfig.MaxStock > 0)
                stockPct = Mathf.Clamp01(
                    (float)BusinessState.Stock / BusinessConfig.MaxStock
                );

            foreach (var rt in _stockLevelFills)
            {
                if (rt == null) continue;
                rt.anchorMax = new Vector2(stockPct, 1f);
            }

            string stockText = $"{BusinessState.Stock}/{BusinessConfig.MaxStock}";
            foreach (var txt in _stockLevelTexts)
            {
                if (txt == null) continue;
                txt.text = stockText;
            }

            // SUPPLIES
            float suppliesPct = 0f;
            if (BusinessConfig.MaxSupplies > 0)
                suppliesPct = Mathf.Clamp01(
                    (float)BusinessState.Supplies / BusinessConfig.MaxSupplies
                );

            foreach (var rt in _suppliesLevelFills)
            {
                if (rt == null) continue;
                rt.anchorMax = new Vector2(suppliesPct, 1f);
            }

            string suppliesText = $"{BusinessState.Supplies}/{BusinessConfig.MaxSupplies}";
            foreach (var txt in _suppliesLevelTexts)
            {
                if (txt == null) continue;
                txt.text = suppliesText;
            }

            // Update shared Business Status line on all pages
            UpdateBusinessStatus();
            UpdateSellButtons();
            UpdateHomeInfo();
            RefreshUpgradePriceLabels();
        }

        private void RefreshUpgradePriceLabels()
        {
            if (_equipmentPriceLabel != null)
                _equipmentPriceLabel.text = BusinessState.EquipmentUpgradeOwned
                    ? "OWNED"
                    : $"${BusinessConfig.EquipmentUpgradePrice:N0}";

            if (_staffPriceLabel != null)
                _staffPriceLabel.text = BusinessState.StaffUpgradeOwned
                    ? "OWNED"
                    : $"${BusinessConfig.StaffUpgradePrice:N0}";

            if (_securityPriceLabel != null)
                _securityPriceLabel.text = BusinessState.SecurityUpgradeOwned
                    ? "OWNED"
                    : $"${BusinessConfig.SecurityUpgradePrice:N0}";
        }

        private void UpdateSellButtons()
        {
            // Use the same logic as the actual sell code so UI matches payout
            float hylandTotal = SellCalculator.HylandPayout;
            float serenaTotal = SellCalculator.SerenaPayout;

            if (_sellHylandLabel != null)
            {
                _sellHylandLabel.text =
                    $"Sell To Hyland Point: ${hylandTotal:N0}";
            }

            if (_sellSerenaLabel != null)
            {
                _sellSerenaLabel.text =
                    $"Sell To Serena Flats: ${serenaTotal:N0} (Locked)";
            }
        }

        private void UpdateBusinessStatus()
        {
            // "Active" if we can convert supplies into stock
            bool active =
                BusinessState.Supplies > 0 &&
                BusinessState.Stock < BusinessConfig.MaxStock;

            string line = active
                ? "BUSINESS STATUS:  <color=#00AA00>ACTIVE</color>"
                : "BUSINESS STATUS:  <color=#AA0000>SUSPENDED</color>";

            foreach (var label in _statusLabels)
            {
                if (label != null)
                    label.text = line;
            }
        }

        private void UpdateHomeInfo()
        {
            // Totals
            if (_totalEarningsValueText != null)
                _totalEarningsValueText.text = $"${BusinessState.TotalEarnings:N0}";

            if (_totalSalesValueText != null)
                _totalSalesValueText.text = BusinessState.TotalSalesCount.ToString("N0");

            // Resupply stats
            if (_resupplyStatText != null)
            {
                float rate = BusinessState.GetResupplySuccessRate() * 100f;
                _resupplyStatText.text =
                    $"Resupply Success Rate: {rate:0}% ({BusinessState.ResupplyJobsCompleted}/{BusinessState.ResupplyJobsStarted})";
            }

            // Sell stats (Hyland only for now)
            if (_sellHylandStatText != null)
            {
                float rate = BusinessState.GetHylandSellSuccessRate() * 100f;
                _sellHylandStatText.text =
                    $"Sell Success (Hyland Point): {rate:0}% ({BusinessState.HylandSellSuccesses}/{BusinessState.HylandSellAttempts})";
            }

            // Serena still locked / not implemented
            if (_sellSerenaStatText != null)
            {
                _sellSerenaStatText.text =
                    "Sell Success (Serena Flats): 0% (0/0)";
            }

            // Stock manufactured
            if (_stockManufacturedStatText != null)
            {
                _stockManufacturedStatText.text =
                    $"Stock Manufactured: {BusinessState.TotalStockProduced:0}";
            }
        }


        private static IEnumerator SuppliesToStockRoutine()
        {
            while (true)
            {
                // BEFORE:
                // yield return new WaitForSeconds(BusinessConfig.ConversionInterval);

                // AFTER:
                yield return new WaitForSeconds(BusinessState.GetEffectiveConversionInterval());
                TryConvertOneSupplyToStock();
            }
        }

        private static void TryConvertOneSupplyToStock()
        {
            if (BusinessState.Supplies <= 0)
                return;

            if (BusinessState.Stock >= BusinessConfig.MaxStock)
                return;

            // consume supplies (reduced by Staff upgrade, if owned)
            float suppliesCost = 1f;
            if (!BusinessState.TryConsumeSupplies(suppliesCost))
                return;

            float perSupply = BusinessState.GetStockPerSupply();
            float totalStock = perSupply;

            if (!BusinessState.TryAddStock(totalStock))
                return;

            BusinessState.RegisterStockProduced(totalStock);

            Msg("[WeaponShipmentApp] Auto conversion: 1 supply -> {0} stock.", totalStock);

            var app = Instance;
            if (app != null)
                app.UpdateBars();
        }

        private static IEnumerator RaidRoutine()
        {
            while (true)
            {
                // Wait between checks
                yield return new WaitForSeconds(BusinessConfig.RaidCheckInterval);
                TryRollRaid();
            }
        }

        private static void TryRollRaid()
        {
            float currentStock = BusinessState.Stock;

            // Not enough stock? No raids.
            if (currentStock < BusinessConfig.RaidMinStockToTrigger)
                return;

            // Stock fullness 0–1
            float fullness = currentStock / BusinessConfig.MaxStock;

            // Base chance scales with how full the warehouse is
            float chance = BusinessConfig.RaidBaseChance * fullness;

            // Apply Security upgrade multiplier (e.g. 0.5f to halve chance)
            chance *= BusinessState.GetRaidChanceMultiplier();

            chance = Mathf.Clamp01(chance);

            float roll = UnityEngine.Random.value;

            if (roll <= chance)
            {
                TriggerRaid(currentStock, fullness);
            }
        }

        private static void TriggerRaid(float currentStock, float fullness)
        {
            // Random fraction between min/max
            float fraction =
                UnityEngine.Random.Range(
                    BusinessConfig.RaidLossMinFraction,
                    BusinessConfig.RaidLossMaxFraction
                );

            float amountToLose = currentStock * fraction;

            // Round nicely and clamp to available stock
            amountToLose = BusinessState.Round3(Mathf.Min(amountToLose, currentStock));

            if (amountToLose <= 0f)
                return;

            // Calculate value of the product we lost (approximate Hyland sell value)
            float lostValue = amountToLose * BusinessConfig.PriceHyland;

            // Actually remove stock
            bool consumed = BusinessState.TryConsumeStock(amountToLose);
            if (!consumed)
                return;

            Msg($"[Disruption Logistics] Police raid! Lost {amountToLose} stock (~${lostValue:N0}).");

            MelonLogger.Msg(
                "[WeaponShipmentApp] RAID triggered. Lost {0} stock (fullness={1:0.00}), value ≈ ${2:N0}. New stock: {3}",
                amountToLose,
                fullness,
                lostValue,
                BusinessState.Stock
            );

            // 👉 Tell Agent 28 (percent + value)
            WeaponShipments.NPCs.Agent28.NotifyRaid(fraction, lostValue);
            // or if you add `using WeaponShipments.NPCs;` at the top:
            // Agent28.NotifyRaid(fraction, lostValue);

            // Refresh UI
            var app = Instance;
            if (app != null)
            {
                app.UpdateBars();
            }
        }

        private Text CreateUpgradeCard(
            Transform parent,
            string key,
            string displayName,
            string priceText,
            Action onClick
        ) 
        {
            // 3:4-ish pillar card
            var card = UIFactory.Panel(
                key + "Card",
                parent,
                new Color(0.02f, 0.02f, 0.02f)
            );
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(0f, 0f);

            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.minWidth = 0f;
            cardLE.flexibleWidth = 1f;      // share row space evenly
            cardLE.preferredHeight = 360f;  // tall-ish pillar

            // Make sure there is an Image on the card
            var cardImg = card.GetComponent<Image>();
            if (cardImg == null)
                cardImg = card.AddComponent<Image>();

            cardImg.color = new Color(0.02f, 0.02f, 0.02f);

            // Whole card clickable
            var btn = card.AddComponent<Button>();
            btn.targetGraphic = cardImg;
            ButtonUtils.AddListener(btn, () => onClick?.Invoke());

            // ---------- IMAGE AREA (top ~70%) ----------
            var imgPanel = UIFactory.Panel(
                key + "Image",
                card.transform,
                new Color(0.05f, 0.05f, 0.05f)
            );
            var imgRT = imgPanel.GetComponent<RectTransform>();
            imgRT.anchorMin = new Vector2(0f, 0.30f);
            imgRT.anchorMax = new Vector2(1f, 1f);
            imgRT.offsetMin = new Vector2(4f, 4f);
            imgRT.offsetMax = new Vector2(-4f, -4f);

            Image img = imgPanel.GetComponent<Image>();
            if (img == null)
                img = imgPanel.AddComponent<Image>();

            img.color = Color.white;
            img.preserveAspect = true;

            var upgradeSprite = LoadUpgradeImage(key);
            if (upgradeSprite != null)
            {
                img.sprite = upgradeSprite;
                img.color = Color.white;
            }
            else
            {
                // Placeholder rectangle since image isn't available yet
                img.sprite = null;
                img.color = new Color(0.15f, 0.15f, 0.15f);
            }

            // ---------- NAME (middle band) ----------
            var nameText = UIFactory.Text(
                key + "Name",
                displayName,
                card.transform,
                14,
                TextAnchor.MiddleCenter,
                FontStyle.Bold
            );
            nameText.color = Color.white;
            var nameRT = nameText.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0.15f);
            nameRT.anchorMax = new Vector2(1f, 0.30f);
            nameRT.offsetMin = new Vector2(4f, 0f);
            nameRT.offsetMax = new Vector2(-4f, 0f);

            // ---------- PRICE (bottom band) ----------
            var price = UIFactory.Text(
                key + "Price",
                priceText,
                card.transform,
                30,
                TextAnchor.MiddleCenter,
                FontStyle.Bold
            );

            price.color = new Color(0.35f, 0f, 0f);

            var priceRT = price.GetComponent<RectTransform>();
            priceRT.anchorMin = new Vector2(0f, 0f);
            priceRT.anchorMax = new Vector2(1f, 0.15f);
            priceRT.offsetMin = new Vector2(4f, 0f);
            priceRT.offsetMax = new Vector2(-4f, 0f);

            return price;
        }

        private Sprite LoadUpgradeImage(string key)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                string target = key.ToLowerInvariant() switch
                {
                    "equipmentupgrade" => "equipment.png",
                    "staffupgrade" => "staff.png",
                    "securityupgrade" => "security.png",
                    _ => null
                };

                if (target == null)
                    return null;

                foreach (var name in asm.GetManifestResourceNames())
                {
                    var lower = name.ToLowerInvariant();
                    if (!lower.EndsWith(target))
                        continue;

                    using (var stream = asm.GetManifestResourceStream(name))
                    {
                        if (stream == null)
                            continue;

                        var data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        return ImageUtils.LoadImageRaw(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Warning($"[WeaponShipmentApp] Failed loading upgrade image for {key}: {ex}");
            }

            return null; // fallback -> gray box
        }

        // ---------------- NAVIGATION ----------------

        private void CreateNavButton(Transform parent, string label, Action onClick)
        {
            const float navButtonHeight = 12f;

            var (buttonGO, button, text) = UIFactory.RoundedButtonWithLabel(
                label.Replace(" ", "") + "NavBtn",
                label,
                parent,
                new Color(0.35f, 0.0f, 0.0f),
                0,
                navButtonHeight,
                16,
                Color.white
            );

            if (text != null)
                text.alignment = TextAnchor.MiddleCenter;

            var layout = buttonGO.GetComponent<LayoutElement>();
            if (layout == null)
                layout = buttonGO.AddComponent<LayoutElement>();

            layout.minHeight = navButtonHeight;
            layout.preferredHeight = navButtonHeight;
            layout.flexibleHeight = 0f;

            ButtonUtils.AddListener(button, () => onClick?.Invoke());
        }

        private void SetActivePage(GameObject page)
        {
            if (_homePage != null) _homePage.SetActive(page == _homePage);
            if (_resupplyPage != null) _resupplyPage.SetActive(page == _resupplyPage);
            if (_researchPage != null) _researchPage.SetActive(page == _researchPage);
            if (_sellStockPage != null) _sellStockPage.SetActive(page == _sellStockPage);
            if (_upgradesPage != null) _upgradesPage.SetActive(page == _upgradesPage);
        }

        private void ShowHomePage() => SetActivePage(_homePage);
        private void ShowResupplyPage() => SetActivePage(_resupplyPage);
        private void ShowResearchPage() => SetActivePage(_researchPage);
        private void ShowSellStockPage() => SetActivePage(_sellStockPage);
        private void ShowUpgradesPage() => SetActivePage(_upgradesPage);

        // ---------------- LOCATION CARD (SAFE IMAGE) ----------------
        private void CreateLocationCard(Transform parent)
        {
            var card = UIFactory.Panel(
                "LocationCard",
                parent,
                new Color(0.04f, 0.04f, 0.04f)
            );
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(0f, 0f);

            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.minHeight = 120f;
            cardLE.flexibleHeight = 1f;

            // IMAGE AREA (top ~78%)
            var imgPanel = UIFactory.Panel(
                "LocationImage",
                card.transform,
                new Color(0f, 0f, 0f)
            );
            var imgRT = imgPanel.GetComponent<RectTransform>();
            imgRT.anchorMin = new Vector2(0f, 0.18f);
            imgRT.anchorMax = new Vector2(1f, 1f);
            imgRT.offsetMin = Vector2.zero;
            imgRT.offsetMax = Vector2.zero;

            Image img = null;
            try { img = imgPanel.GetComponent<Image>(); } catch { }
            if (img == null)
            {
                try { img = imgPanel.AddComponent<Image>(); }
                catch { }
            }

            if (img != null)
            {
                try
                {
                    var sewerSprite = LoadLocationImage();
                    if (sewerSprite != null)
                    {
                        img.sprite = sewerSprite;
                        img.color = Color.white;
                        img.preserveAspect = true;
                    }
                    else
                    {
                        img.color = new Color(0.15f, 0.15f, 0.15f);
                    }
                }
                catch
                {
                    img.color = new Color(0.15f, 0.15f, 0.15f);
                }
            }

            // RED STRIP (~5% of card)
            var nameStrip = UIFactory.Panel(
                "LocationNameStrip",
                card.transform,
                new Color(0.5f, 0.0f, 0.0f)
            );
            var stripRT = nameStrip.GetComponent<RectTransform>();
            stripRT.anchorMin = new Vector2(0f, 0.12f);
            stripRT.anchorMax = new Vector2(1f, 0.15f);
            stripRT.offsetMin = Vector2.zero;
            stripRT.offsetMax = Vector2.zero;

            var nameText = UIFactory.Text(
                "LocationName",
                "Sewer Office",
                nameStrip.transform,
                13,
                TextAnchor.MiddleLeft,
                FontStyle.Bold
            );
            nameText.color = Color.white;
            var nameTextRT = nameText.GetComponent<RectTransform>();
            nameTextRT.offsetMin = new Vector2(6f, 0f);
            nameTextRT.offsetMax = new Vector2(-6f, 0f);

            // LOCATION INFO (~17% of card)
            var infoPanel = UIFactory.Panel(
                "LocationInfo",
                card.transform,
                new Color(0.02f, 0.02f, 0.02f)
            );
            var infoRT = infoPanel.GetComponent<RectTransform>();
            infoRT.anchorMin = new Vector2(0f, 0f);
            infoRT.anchorMax = new Vector2(1f, 0.15f);
            infoRT.offsetMin = Vector2.zero;
            infoRT.offsetMax = Vector2.zero;

            var locLabel = UIFactory.Text(
                "LocationLabel",
                "LOCATION:",
                infoPanel.transform,
                10,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            locLabel.color = Color.white;
            var locLabelRT = locLabel.GetComponent<RectTransform>();
            locLabelRT.anchorMin = new Vector2(0f, 0.65f);
            locLabelRT.anchorMax = new Vector2(1f, 1f);
            locLabelRT.offsetMin = new Vector2(6f, -2f);
            locLabelRT.offsetMax = new Vector2(-6f, 0f);

            var locValue = UIFactory.Text(
                "LocationValue",
                "Sewer Office",
                infoPanel.transform,
                10,
                TextAnchor.LowerLeft,
                FontStyle.Normal
            );
            locValue.color = Color.white;
            var locValueRT = locValue.GetComponent<RectTransform>();
            locValueRT.anchorMin = new Vector2(0f, 0f);
            locValueRT.anchorMax = new Vector2(1f, 0.55f);
            locValueRT.offsetMin = new Vector2(6f, 0f);
            locValueRT.offsetMax = new Vector2(-6f, 2f);
        }

        // ---------------- HOME PAGE CONTENT ----------------

        private void BuildHomePageUI(Transform parent)
        {
            // TOP: STATUS + BARS
            var topPanel = UIFactory.Panel(
                "HomeTopSection",
                parent,
                new Color(0, 0, 0, 0)
            );
            var topRT = topPanel.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0f, 0.65f);
            topRT.anchorMax = new Vector2(1f, 1f);
            topRT.offsetMin = new Vector2(20f, 20f);
            topRT.offsetMax = new Vector2(-20f, -20f);

            UIFactory.VerticalLayoutOnGO(
                topPanel,
                spacing: 10,
                padding: new RectOffset(0, 0, 0, 0)
            );

            var statusText = UIFactory.Text(
                "BusinessStatus",
                "BUSINESS STATUS:  <color=#AA0000>SUSPENDED</color>",
                topPanel.transform,
                20,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            statusText.supportRichText = true;
            statusText.color = Color.white;
            _statusLabels.Add(statusText);

            CreateStatusBarRow(topPanel.transform, "Stock Level");
            CreateStatusBarRow(topPanel.transform, "Research Progress");
            CreateStatusBarRow(topPanel.transform, "Supplies Level");

            // BOTTOM: HOME DESCRIPTION + TOTALS + PERFORMANCE STATS
            var bottomPanel = UIFactory.Panel(
                "HomeBottomSection",
                parent,
                new Color(0, 0, 0, 0)
            );
            var bottomRT = bottomPanel.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0f, 0f);
            bottomRT.anchorMax = new Vector2(1f, 0.55f);
            bottomRT.offsetMin = new Vector2(20f, 20f);
            bottomRT.offsetMax = new Vector2(-20f, 0f);

            UIFactory.VerticalLayoutOnGO(
                bottomPanel,
                spacing: 8,
                padding: new RectOffset(0, 0, 0, 0)
            );

            var overviewTitle = UIFactory.Text(
                "HomeOverviewTitle",
                "OVERVIEW",
                bottomPanel.transform,
                20,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            overviewTitle.color = Color.white;

            var overviewDesc = UIFactory.Text(
                "HomeOverviewDescription",
                "Monitor the state of your operation at a glance. " +
                "Stock Level shows how many weapons are ready to move, " +
                "Research Progress tracks unlocked modifications, and " +
                "Supplies Level represents the raw materials you have on hand.",
                bottomPanel.transform,
                16,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            overviewDesc.color = Color.white;

            // Row with totals (earnings, sales, etc.)
            var totalsRow = UIFactory.Panel(
                "TotalsRow",
                bottomPanel.transform,
                new Color(0, 0, 0, 0)
            );
            var hLayout = totalsRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = true;

            _totalEarningsValueText = CreateTotalsTab(totalsRow.transform, "Total Earnings", "$0");
            _totalSalesValueText = CreateTotalsTab(totalsRow.transform, "Total Sales", "0");

            // NEW: PERFORMANCE / STATS SECTION UNDER THE TOTALS
            var statsPanel = UIFactory.Panel(
                "HomeStatsSection",
                bottomPanel.transform,
                new Color(0, 0, 0, 0)
            );
            UIFactory.VerticalLayoutOnGO(
                statsPanel,
                spacing: 4,
                padding: new RectOffset(0, 0, 4, 0)
            );

            var statsTitle = UIFactory.Text(
                "HomeStatsTitle",
                "PERFORMANCE",
                statsPanel.transform,
                18,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            statsTitle.color = Color.white;

            // Simple text lines for each stat; you can wire these up to real values later.
            var resupplyStat = UIFactory.Text(
                "ResupplySuccessRate",
                "Resupply Success Rate: 0%",
                statsPanel.transform,
                14,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            resupplyStat.color = Color.white;
            _resupplyStatText = resupplyStat;

            var sellHylandStat = UIFactory.Text(
                "SellSuccessHyland",
                "Sell Success (Hyland Point): 0%",
                statsPanel.transform,
                14,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            sellHylandStat.color = Color.white;
            _sellHylandStatText = sellHylandStat;

            var sellSerenaStat = UIFactory.Text(
                "SellSuccessSerena",
                "Sell Success (Serena Flats): 0%",
                statsPanel.transform,
                14,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            sellSerenaStat.color = Color.white;
            _sellSerenaStatText = sellSerenaStat;

            var stockManufacturedStat = UIFactory.Text(
                "StockManufactured",
                "Stock Manufactured: 0",
                statsPanel.transform,
                14,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            stockManufacturedStat.color = Color.white;
            _stockManufacturedStatText = stockManufacturedStat;
        }

        // ---------------- RESUPPLY PAGE CONTENT ----------------

        private void BuildResupplyPageUI(Transform parent)
        {
            // TOP: STATUS + BARS
            var topPanel = UIFactory.Panel(
                "ResupplyTopSection",
                parent,
                new Color(0, 0, 0, 0)
            );
            var topRT = topPanel.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0f, 0.65f);
            topRT.anchorMax = new Vector2(1f, 1f);
            topRT.offsetMin = new Vector2(20f, 20f);
            topRT.offsetMax = new Vector2(-20f, -20f);

            UIFactory.VerticalLayoutOnGO(
                topPanel,
                spacing: 10,
                padding: new RectOffset(0, 0, 0, 0)
            );

            var statusText = UIFactory.Text(
                "ResupplyBusinessStatus",
                "BUSINESS STATUS:  <color=#AA0000>SUSPENDED</color>",
                topPanel.transform,
                20,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            statusText.supportRichText = true;
            statusText.color = Color.white;
            _statusLabels.Add(statusText);

            CreateStatusBarRow(topPanel.transform, "Stock Level");
            CreateStatusBarRow(topPanel.transform, "Research Progress");
            CreateStatusBarRow(topPanel.transform, "Supplies Level");

            // BOTTOM: RESUPPLY TITLE + DESC + BUTTONS + CRATE
            var bottomPanel = UIFactory.Panel(
                "ResupplyBottomSection",
                parent,
                new Color(0, 0, 0, 0)
            );
            var bottomRT = bottomPanel.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0f, 0f);
            bottomRT.anchorMax = new Vector2(1f, 0.55f);
            bottomRT.offsetMin = new Vector2(20f, 20f);
            bottomRT.offsetMax = new Vector2(-20f, 0f);

            UIFactory.VerticalLayoutOnGO(
                bottomPanel,
                spacing: 8,
                padding: new RectOffset(0, 0, 0, 0)
            );

            var titleText = UIFactory.Text(
                "ResupplyTitle",
                "RESUPPLY",
                bottomPanel.transform,
                20,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            titleText.color = Color.white;

            var descText = UIFactory.Text(
                "ResupplyDescription",
                "Supplies are required to manufacture weapons or research modifications. " +
                "They can be stolen from a range of sources or a fee can be paid to have " +
                "supplies delivered directly to the office.",
                bottomPanel.transform,
                16,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            descText.color = Color.white;

            var row = UIFactory.Panel(
                "ResupplyButtonsRow",
                bottomPanel.transform,
                new Color(0, 0, 0, 0)
            );
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 140f);

            var hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8f;
            hLayout.childAlignment = TextAnchor.UpperLeft;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandHeight = false;
            hLayout.padding = new RectOffset(0, 0, 0, 0);

            // LEFT COLUMN: BUTTONS
            var leftColumn = UIFactory.Panel(
                "ResupplyButtonsLeft",
                row.transform,
                new Color(0, 0, 0, 0)
            );
            var leftLE = leftColumn.AddComponent<LayoutElement>();
            leftLE.minWidth = 600f;
            leftLE.preferredWidth = 600f;
            leftLE.flexibleWidth = 0f;

            UIFactory.VerticalLayoutOnGO(
                leftColumn,
                spacing: 6,
                padding: new RectOffset(0, 0, 0, 0)
            );

            const int buttonHeight = 24;
            const int buttonFontSize = 14;

            var (stealGO, stealBtn, stealLabel) = UIFactory.RoundedButtonWithLabel(
                "StealSuppliesButton",
                "Steal Supplies",
                leftColumn.transform,
                new Color(0.35f, 0.0f, 0.0f),
                0,
                buttonHeight,
                buttonFontSize,
                Color.white
            );
            if (stealLabel != null)
                stealLabel.alignment = TextAnchor.MiddleCenter;
            var stealBtnLE = stealGO.AddComponent<LayoutElement>();
            stealBtnLE.minHeight = buttonHeight;
            stealBtnLE.preferredHeight = buttonHeight;
            stealBtnLE.flexibleWidth = 1f;
            ButtonUtils.AddListener(stealBtn, OnStealSuppliesClicked);

            var (buyGO, buyBtn, buyLabel) = UIFactory.RoundedButtonWithLabel(
                "BuySuppliesButton",
                $"Buy Supplies: ${BusinessConfig.BuySuppliesPrice:N0}",
                leftColumn.transform,
                new Color(0.35f, 0.0f, 0.0f),
                0,
                buttonHeight,
                buttonFontSize,
                Color.white
            );
            if (buyLabel != null)
                buyLabel.alignment = TextAnchor.MiddleCenter;
            var buyBtnLE = buyGO.AddComponent<LayoutElement>();
            buyBtnLE.minHeight = buttonHeight;
            buyBtnLE.preferredHeight = buttonHeight;
            buyBtnLE.flexibleWidth = 1f;
            ButtonUtils.AddListener(buyBtn, OnBuySuppliesClicked);

            // RIGHT: RESUPPLY CRATE IMAGE
            var cratePanel = UIFactory.Panel(
                "ResupplyCratePanel",
                row.transform,
                new Color(0.05f, 0.05f, 0.05f)
            );
            var crateLE = cratePanel.AddComponent<LayoutElement>();
            crateLE.minWidth = 400f;
            crateLE.preferredWidth = 400f;
            crateLE.flexibleWidth = 1f;
            crateLE.minHeight = 200f;
            crateLE.preferredHeight = 200f;

            var crateImg = cratePanel.GetComponent<Image>();
            if (crateImg == null)
                crateImg = cratePanel.AddComponent<Image>();

            var crateSprite = LoadResupplyActionImage();
            if (crateSprite != null)
            {
                crateImg.sprite = crateSprite;
                crateImg.color = Color.white;
                crateImg.preserveAspect = true;
            }
            else
            {
                crateImg.sprite = null;
                crateImg.color = new Color(0.2f, 0.2f, 0.2f);
            }
        }

        // ---------------- SELL STOCK PAGE CONTENT ----------------

        private void BuildSellStockPageUI(Transform parent)
        {
            // TOP: STATUS + BARS
            var topPanel = UIFactory.Panel(
                "SellTopSection",
                parent,
                new Color(0, 0, 0, 0)
            );
            var topRT = topPanel.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0f, 0.65f);
            topRT.anchorMax = new Vector2(1f, 1f);
            topRT.offsetMin = new Vector2(20f, 20f);
            topRT.offsetMax = new Vector2(-20f, -20f);

            UIFactory.VerticalLayoutOnGO(
                topPanel,
                10,
                new RectOffset(0, 0, 0, 0)
            );

            var statusText = UIFactory.Text(
                "SellBusinessStatus",
                "BUSINESS STATUS:  <color=#AA0000>SUSPENDED</color>",
                topPanel.transform,
                20,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            statusText.supportRichText = true;
            statusText.color = Color.white;
            _statusLabels.Add(statusText);

            CreateStatusBarRow(topPanel.transform, "Stock Level");
            CreateStatusBarRow(topPanel.transform, "Research Progress");
            CreateStatusBarRow(topPanel.transform, "Supplies Level");

            // BOTTOM: TITLE + DESC + BUTTONS
            var bottomPanel = UIFactory.Panel(
                "SellBottomSection",
                parent,
                new Color(0, 0, 0, 0)
            );
            var bottomRT = bottomPanel.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0f, 0f);
            bottomRT.anchorMax = new Vector2(1f, 0.55f);
            bottomRT.offsetMin = new Vector2(20f, 20f);
            bottomRT.offsetMax = new Vector2(-20f, 0f);

            UIFactory.VerticalLayoutOnGO(
                bottomPanel,
                8,
                new RectOffset(0, 0, 0, 0)
            );

            var titleText = UIFactory.Text(
                "SellStockTitle",
                "Sell Stock",
                bottomPanel.transform,
                20,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            titleText.color = Color.white;

            var descText = UIFactory.Text(
                "SellStockDescription",
                "Sell your current stock of weapons to buyers in Hyland Point.\n" +
                "Further destinations like Serena Flats offer higher payouts once unlocked.",
                bottomPanel.transform,
                16,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            descText.color = Color.white;

            var buttonsPanel = UIFactory.Panel(
                "SellButtons",
                bottomPanel.transform,
                new Color(0, 0, 0, 0)
            );
            var vLayout = buttonsPanel.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 8;
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;

            // HYLAND POINT
            var (hyGO, hyBtn, hyLabel) = UIFactory.RoundedButtonWithLabel(
                "SellHylandPoint",
                "Sell To Hyland Point: $7,000",
                buttonsPanel.transform,
                new Color(0.35f, 0.0f, 0.0f),
                0,
                32,
                16,
                Color.white
            );
            _sellHylandLabel = hyLabel;
            if (hyLabel != null)
                hyLabel.alignment = TextAnchor.MiddleCenter;
            hyGO.AddComponent<LayoutElement>().minHeight = 32f;
            ButtonUtils.AddListener(hyBtn, OnSellToHylandPointClicked);

            // SERENA FLATS (LOCKED)
            var (sfGO, sfBtn, sfLabel) = UIFactory.RoundedButtonWithLabel(
                "SellSerenaFlats",
                "Sell To Serena Flats: $10,500 (Locked)",
                buttonsPanel.transform,
                new Color(0.12f, 0.00f, 0.00f),
                0,
                32,
                16,
                new Color(0.4f, 0.4f, 0.4f)
            );
            _sellSerenaLabel = sfLabel;
            if (sfLabel != null)
                sfLabel.alignment = TextAnchor.MiddleCenter;

            var sfLE = sfGO.AddComponent<LayoutElement>();
            sfLE.minHeight = 32f;

            sfBtn.interactable = false;

            var overlay = sfGO.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.35f);
            overlay.raycastTarget = false;
        }

        // ---------------- UPGRADES PAGE CONTENT ----------------

        private void BuildUpgradesPageUI(Transform parent)
        {
            // TOP: STATUS + BARS
            var topPanel = UIFactory.Panel(
                "UpgradesTopSection",
                parent,
                new Color(0, 0, 0, 0)
            );
            var topRT = topPanel.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0f, 0.65f);
            topRT.anchorMax = new Vector2(1f, 1f);
            topRT.offsetMin = new Vector2(20f, 20f);
            topRT.offsetMax = new Vector2(-20f, -20f);

            UIFactory.VerticalLayoutOnGO(
                topPanel,
                spacing: 8,
                padding: new RectOffset(0, 0, 0, 0)
            );

            var statusText = UIFactory.Text(
                "UpgradesBusinessStatus",
                "BUSINESS STATUS:  <color=#AA0000>SUSPENDED</color>",
                topPanel.transform,
                20,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            statusText.supportRichText = true;
            statusText.color = Color.white;
            _statusLabels.Add(statusText);

            // BOTTOM: UPGRADES GRID
            var bottomPanel = UIFactory.Panel(
                "UpgradesBottomSection",
                parent,
                new Color(0, 0, 0, 0)
            );
            var bottomRT = bottomPanel.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0f, 0f);
            bottomRT.anchorMax = new Vector2(1f, 0.85f);
            bottomRT.offsetMin = new Vector2(20f, 20f);
            bottomRT.offsetMax = new Vector2(-20f, 0f);

            UIFactory.VerticalLayoutOnGO(
                bottomPanel,
                spacing: 8,
                padding: new RectOffset(0, 0, 0, 0)
            );

            var title = UIFactory.Text(
                "UpgradesTitle",
                "UPGRADES",
                bottomPanel.transform,
                20,
                TextAnchor.UpperLeft,
                FontStyle.Bold
            );
            title.color = Color.white;

            var desc = UIFactory.Text(
                "UpgradesDescription",
                "Invest in your infrastructure, staff, and security to improve your operation.",
                bottomPanel.transform,
                16,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            desc.color = Color.white;

            var upgradesRow = UIFactory.Panel(
                "UpgradesRow",
                bottomPanel.transform,
                new Color(0, 0, 0, 0)
            );
            var rowRT = upgradesRow.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 0f);

            var hLayout = upgradesRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8f;
            hLayout.childAlignment = TextAnchor.UpperCenter;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandHeight = true;

            _equipmentPriceLabel = CreateUpgradeCard(
                upgradesRow.transform,
                "EquipmentUpgrade",
                "Equipment Upgrade",
                $"${BusinessConfig.EquipmentUpgradePrice:N0}",
                OnEquipmentUpgradeClicked
            );

            _staffPriceLabel = CreateUpgradeCard(
                upgradesRow.transform,
                "StaffUpgrade",
                "Staff Upgrade",
                $"${BusinessConfig.StaffUpgradePrice:N0}",
                OnStaffUpgradeClicked
            );

            _securityPriceLabel = CreateUpgradeCard(
                upgradesRow.transform,
                "SecurityUpgrade",
                "Security Upgrade",
                $"${BusinessConfig.SecurityUpgradePrice:N0}",
                OnSecurityUpgradeClicked
            );

            // Make sure labels show OWNED if you already bought something
            RefreshUpgradePriceLabels();
        }

        // ---------------- STATUS BARS + TOTALS ----------------

        private void CreateStatusBarRow(Transform parent, string labelText)
        {
            var row = UIFactory.Panel(
                labelText.Replace(" ", "") + "Row",
                parent,
                new Color(0, 0, 0, 0)
            );
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 26f);

            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.minHeight = 26f;
            rowLE.preferredHeight = 26f;

            const float labelWidth = 160f;
            const float gap = 40f;
            const float barWidth = 260f;

            var label = UIFactory.Text(
                labelText.Replace(" ", "") + "Label",
                labelText,
                row.transform,
                16,
                TextAnchor.MiddleLeft,
                FontStyle.Normal
            );
            label.color = Color.white;

            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(0f, 1f);
            labelRT.pivot = new Vector2(0f, 0.5f);
            labelRT.sizeDelta = new Vector2(labelWidth, 0f);
            labelRT.anchoredPosition = new Vector2(0f, 0f);

            var barOuter = UIFactory.Panel(
                labelText.Replace(" ", "") + "BarOuter",
                row.transform,
                new Color(0.8f, 0.8f, 0.8f)
            );
            var outerRT = barOuter.GetComponent<RectTransform>();
            outerRT.anchorMin = new Vector2(0f, 0.5f);
            outerRT.anchorMax = new Vector2(0f, 0.5f);
            outerRT.pivot = new Vector2(0f, 0.5f);
            outerRT.sizeDelta = new Vector2(barWidth, 18f);
            outerRT.anchoredPosition = new Vector2(labelWidth + gap, 0f);

            var outerLE = barOuter.AddComponent<LayoutElement>();
            outerLE.minWidth = barWidth;
            outerLE.preferredWidth = barWidth;

            var barInner = UIFactory.Panel(
                labelText.Replace(" ", "") + "BarInner",
                barOuter.transform,
                new Color(0.02f, 0.02f, 0.02f)
            );
            var innerRT = barInner.GetComponent<RectTransform>();
            innerRT.anchorMin = new Vector2(0f, 0f);
            innerRT.anchorMax = new Vector2(1f, 1f);
            innerRT.offsetMin = new Vector2(1f, 1f);
            innerRT.offsetMax = new Vector2(-1f, -1f);

            var fillGO = UIFactory.Panel(
                labelText.Replace(" ", "") + "Fill",
                barInner.transform,
                new Color(0.7f, 0.1f, 0.1f)
            );
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.pivot = new Vector2(0f, 0.5f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            var valueText = UIFactory.Text(
                labelText.Replace(" ", "") + "Value",
                "?/?",
                row.transform,
                14,
                TextAnchor.MiddleLeft,
                FontStyle.Normal
            );
            valueText.color = Color.white;
            var valueRT = valueText.GetComponent<RectTransform>();
            valueRT.anchorMin = new Vector2(0f, 0.5f);
            valueRT.anchorMax = new Vector2(0f, 0.5f);
            valueRT.pivot = new Vector2(0f, 0.5f);
            valueRT.anchoredPosition = new Vector2(labelWidth + gap + barWidth + 10f, 0f);
            valueRT.sizeDelta = new Vector2(80f, 0f);

            if (labelText == "Stock Level")
            {
                _stockLevelFills.Add(fillRT);
                _stockLevelTexts.Add(valueText);
            }
            else if (labelText == "Supplies Level")
            {
                _suppliesLevelFills.Add(fillRT);
                _suppliesLevelTexts.Add(valueText);
            }
        }

        private Text CreateTotalsTab(Transform parent, string title, string value)
        {
            var tab = UIFactory.Panel(
                title.Replace(" ", "") + "Tab",
                parent,
                new Color(0.35f, 0.0f, 0.0f)
            );
            var tabRT = tab.GetComponent<RectTransform>();
            tabRT.sizeDelta = new Vector2(0f, 80f);

            var vLayout = tab.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 4f;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true;
            vLayout.childForceExpandWidth = true;

            var titleText = UIFactory.Text(
                title.Replace(" ", "") + "Title",
                title,
                tab.transform,
                20,
                TextAnchor.MiddleCenter,
                FontStyle.Bold
            );
            titleText.color = Color.white;

            var valueText = UIFactory.Text(
                title.Replace(" ", "") + "Value",
                value,
                tab.transform,
                32,
                TextAnchor.MiddleCenter,
                FontStyle.Normal
            );
            valueText.color = Color.white;

            return valueText;
        }

        private void BuildPlaceholderPage(Transform parent, string pageTitle)
        {
            UIFactory.VerticalLayoutOnGO(
                parent.gameObject,
                spacing: 10,
                padding: new RectOffset(20, 20, 20, 20)
            );

            var text = UIFactory.Text(
                pageTitle + "Placeholder",
                pageTitle + " Page\n(placeholder – add content later)",
                parent,
                18,
                TextAnchor.UpperLeft,
                FontStyle.Italic
            );
            text.color = new Color(0.85f, 0.85f, 0.85f);
        }

        // ---------------- BRANDING ----------------

        private void CreateBrandingLogo(Transform parent, int disruptionFontSize, int logisticsFontSize)
        {
            var container = UIFactory.Panel(
                "BrandingContainer",
                parent,
                new Color(0, 0, 0, 0)
            );
            var cRT = container.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0f, 0f);
            cRT.anchorMax = new Vector2(1f, 1f);
            cRT.offsetMin = Vector2.zero;
            cRT.offsetMax = Vector2.zero;

            var disruptionText = UIFactory.Text(
                "DisruptionTitle",
                "DISRUPTION",
                container.transform,
                disruptionFontSize,
                TextAnchor.LowerLeft,
                FontStyle.Bold
            );
            disruptionText.color = new Color(0.9f, 0.1f, 0.1f);
            var dRT = disruptionText.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0.05f, 0.45f);
            dRT.anchorMax = new Vector2(0.95f, 0.95f);
            dRT.offsetMin = Vector2.zero;
            dRT.offsetMax = Vector2.zero;

            var logisticsText = UIFactory.Text(
                "LogisticsSubtitle",
                "LOGISTICS",
                container.transform,
                logisticsFontSize,
                TextAnchor.UpperLeft,
                FontStyle.Normal
            );
            logisticsText.color = Color.white;
            var lRT = logisticsText.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0.05f, 0.0f);
            lRT.anchorMax = new Vector2(0.6f, 0.5f);
            lRT.offsetMin = Vector2.zero;
            lRT.offsetMax = Vector2.zero;
        }

        // ---------------- BUTTON HANDLERS ----------------

        private void OnStealSuppliesClicked()
        {
            var mgr = ShipmentManager.Instance;

            if (mgr.HasActiveInProgressShipment())
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Already stealing",
                    "You must finish the current shipment before starting another.",
                    true
                );
                return;
            }

            ShipmentManager.ShipmentEntry chosen = null;

            foreach (var s in mgr.GetAllShipments())
            {
                if (mgr.AcceptShipment(s.Id))
                {
                    chosen = s;
                    break;
                }
            }

            if (chosen == null)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "No shipment available",
                    "All routes are currently on cooldown.",
                    true
                );
                return;
            }

            BusinessState.RegisterResupplyJobStarted();
            ShipmentSpawner.SpawnShipmentCrate(chosen);
            ShipmentSpawner.SpawnDeliveryArea(chosen.Id);

            Msg(
                "[WeaponShipmentApp] Started steal job for {0} (qty {1}) – origin: {2}, dest: {3}",
                chosen.GunType,
                chosen.Quantity,
                chosen.Origin,
                chosen.Destination
            );
        }

        private void OnBuySuppliesClicked()
        {
            float balance = Money.GetCashBalance();
            int price = BusinessConfig.BuySuppliesPrice;

            if (BusinessState.Supplies >= BusinessConfig.MaxSupplies)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Supplies storage already full",
                    "You can't hold any more supplies.",
                    true
                );
                return;
            }

            if (balance < price)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Not enough money",
                    "You need more cash to buy supplies.",
                    true
                );
                return;
            }

            Money.ChangeCashBalance(-price);

            int amount = BusinessConfig.MaxSupplies;
            if (BusinessState.TryAddSupplies(amount))
            {
                Msg($"[WeaponShipmentApp] Bought {amount} supplies.");

                // Tell Agent 28 a purchase arrived
                Agent28.NotifySuppliesArrived(
                    amount,
                    BusinessState.Supplies,
                    false   // fromShipment = false (this was a purchase)
                );

                UpdateBars();
            }
        }

        private void OnEquipmentUpgradeClicked()
        {
            if (BusinessState.EquipmentUpgradeOwned)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Already owned",
                    "You have already purchased this upgrade.",
                    true
                );
                return;
            }

            float balance = Money.GetCashBalance();
            float price = BusinessConfig.EquipmentUpgradePrice;

            if (balance < price)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Not enough money",
                    "You need more cash to do this.",
                    true
                );
                return;
            }

            Money.ChangeCashBalance(-price);

            if (BusinessState.TryBuyEquipmentUpgrade())
            {
                _equipmentOwned = true;
                Msg("[WeaponShipmentApp] Equipment upgrade purchased.");

                UpdateBars();
            }
        }

        private void OnStaffUpgradeClicked()
        {
            if (BusinessState.StaffUpgradeOwned)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Already owned",
                    "You have already purchased this upgrade.",
                    true
                );
                return;
            }

            float balance = Money.GetCashBalance();
            float price = BusinessConfig.StaffUpgradePrice;

            if (balance < price)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Not enough money",
                    "You need more cash to do this.",
                    true
                );
                return;
            }

            Money.ChangeCashBalance(-price);

            if (BusinessState.TryBuyStaffUpgrade())
            {
                _staffOwned = true;
                Msg("[WeaponShipmentApp] Staff upgrade purchased.");

                UpdateBars();
            }
        }

        private void OnSecurityUpgradeClicked()
        {
            if (BusinessState.SecurityUpgradeOwned)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Already owned",
                    "You have already purchased this upgrade.",
                    true
                );
                return;
            }

            float balance = Money.GetCashBalance();
            float price = BusinessConfig.SecurityUpgradePrice;

            if (balance < price)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Not enough money",
                    "You need more cash to do this.",
                    true
                );
                return;
            }

            Money.ChangeCashBalance(-price);

            if (BusinessState.TryBuySecurityUpgrade())
            {
                _securityOwned = true;
                Msg("[WeaponShipmentApp] Security upgrade purchased.");

                UpdateBars();
            }
        }

        private void OnSellToHylandPointClicked()
        {
            // Prevent stacking multiple sell jobs
            if (BusinessState.SellJobInProgress)
            {
                WeaponShipmentApp.ShowAlertStatic(
                    "Delivery already in progress",
                    "Finish your current delivery before starting another.",
                    true
                );
                return;
            }

            float currentStock = BusinessState.Stock;

            // Nothing to sell
            if (currentStock <= 0f)
            {
                Msg("[WeaponShipmentApp] No stock to sell.");
                UpdateBars();
                return;
            }

            float payout = SellCalculator.HylandPayout;

            // Track that the player attempted a Hyland sale (for stats),
            // regardless of whether they succeed at the delivery.
            BusinessState.RegisterSellAttempt(true);

            if (payout <= 0f)
            {
                Msg("[WeaponShipmentApp] Calculated payout was zero; aborting sell job.");
                UpdateBars();
                return;
            }

            if (!BusinessState.TryBeginSellJob())
            {
                // Race-safety: if something else set the flag just now.
                WeaponShipmentApp.ShowAlertStatic(
                    "Delivery already in progress",
                    "Finish your current delivery before starting another.",
                    true
                );
                return;
            }

            // Lock the product into the crate: remove from warehouse stock.
            // If the crate is lost, the product is gone.
            if (!BusinessState.TryConsumeStock(currentStock))
            {
                BusinessState.ClearSellJobFlag();
                Msg("[WeaponShipmentApp] Failed to consume stock for sell job.");
                UpdateBars();
                return;
            }

            // Start the world minigame: one crate representing the whole shipment.
            ShipmentSpawner.SpawnSellJob(currentStock, payout);

            Msg(
                "[WeaponShipmentApp] Started sell job for Hyland Point: {0} stock -> payout ${1:N0}.",
                currentStock,
                payout
            );

            UpdateBars();
        }

        public void OnExternalShipmentChanged(string shipmentId)
        {
            UpdateBars();
        }

        // ---------------- ICON & LOCATION IMAGE LOADING ----------------

        private Sprite LoadEmbeddedIcon()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (!name.Contains("shipments.png"))
                        continue;

                    using (var stream = asm.GetManifestResourceStream(name))
                    {
                        if (stream == null)
                            continue;

                        var data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);

                        return ImageUtils.LoadImageRaw(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Warning($"[WeaponShipmentApp] Failed to load icon: {ex}");
            }

            return null;
        }

        private Sprite LoadLocationImage()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (!name.Contains("seweroffice.png"))
                        continue;

                    using (var stream = asm.GetManifestResourceStream(name))
                    {
                        if (stream == null)
                            continue;

                        var data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);

                        return ImageUtils.LoadImageRaw(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Warning($"[WeaponShipmentApp] Failed to load sewer office image: {ex}");
            }

            return null;
        }

        private Sprite LoadResupplyActionImage()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    var lower = name.ToLowerInvariant();

                    if (!lower.EndsWith("resupply.png"))
                        continue;

                    using (var stream = asm.GetManifestResourceStream(name))
                    {
                        if (stream == null)
                            continue;

                        var data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        return ImageUtils.LoadImageRaw(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Warning($"[WeaponShipmentApp] Failed to load resupply.png: {ex}");
            }

            return null;
        }
    }
}
