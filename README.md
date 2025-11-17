Perfect — **clean, efficient, and polished** with a professional tone.
I’ll now generate the **full README** using your approved structure and tone.

---

# 📦 Weapon Shipments

*A modular weapon–delivery contract system with dynamic routes, cooldowns, and payouts.*

## **Overview**

Weapon Shipments adds a fully-featured delivery contract loop to the game.
Each weapon type has its own shipment slot, randomly generated routes, quantities, and payouts.
Using the in-game phone, players can accept, deliver, and complete shipments for cash rewards.

The system is designed to feel integrated, progression-friendly, and expandable for future updates.

---

## **Features**

* **7 permanent shipment slots** (one per weapon type).
* **Dynamic routes** with randomized origins, destinations, and quantities.
* **Interactive phone app** for managing shipments and tracking cooldowns.
* **In-world crates** that scale based on quantity (1–3).
* **Delivery zones** with visual markers, unique per destination.
* **Per-weapon cooldown system** preventing spam and adding pacing.
* **Payouts based on weapon tier**, with physical delivery required.
* **Minimal UI** that updates automatically inside the phone app.

---

## **How It Works**

1. **Accept a shipment** in the phone app (only one active at a time).
2. **Pick up the spawned crate** at its origin location.
3. **Deliver it to the highlighted zone** at the destination.
4. **Complete the contract** in the phone app to receive your payout.
5. The slot enters **cooldown**, then regenerates a new route.

Simple, quick, and loop-friendly.

---

## **Future Features (Planned)**

### **Weapon Smuggler Unlock System**

A new progression framework tied directly to the criminal hierarchy.

* The Weapon Shipments app will start **locked**.
* Access is unlocked by speaking with the **Weapon Smuggler NPC**.
* Higher ranks unlock higher-tier weapons for delivery.

**Planned unlock progression:**

* **Peddler 3** → Baseball Bat
* **Hustler 3** → Machete
* **Bagman 3** → Revolver
* **Shot Caller 1** → M1911
* **Underlord 1** → Pump Shotgun
* **Baron 3** → AK-47
* **Kingpin** → Minigun *(final tier)*

### **Vehicle Transport System**

Improve delivery speed and reduce crate carrying time.

* Ability to **purchase vehicles** for smuggling operations.
* Crates can be **loaded into vehicles** instead of carried manually.
* Larger vehicles may allow carrying **multiple crates** at once.
* Vehicle tiers may match the criminal rank unlock progression.

---

## **Installation**

1. Install **MelonLoader** (version 0.7.0 recommended).
2. Place the mod `.dll` into

   ```
   /Mods
   ```

   inside your game directory.
3. Launch the game

---

## **Credits**

**Mod Developer:** *FannsoNetti*
**Dependencies:** MelonLoader + S1API