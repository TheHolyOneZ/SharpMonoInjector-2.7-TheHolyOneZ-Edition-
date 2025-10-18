# SharpMonoInjector TheHolyOneZ Edition v2.7

A **modern, fully-featured Mono assembly injector** with **advanced stealth injection**, **real-time logging**, **profile management**, **Thunderstore and r2modman auto-detection**, and **smart injection routing** for both **standard BepInEx** and **mod manager environments**.

This release builds on **v2.6**, introducing the new **Smart Injection Router**, **Receiver Auto-Detection**, and **Thunderstore integration**, making SharpMonoInjector easier to use than ever before.

![SharpMonoInjector GUI](Images/AllWindows.png)

---

## 🧭 Overview

**SharpMonoInjector** allows safe injection of managed assemblies into Mono-embedded applications (commonly Unity Engine games) without restarting the target process. This is ideal for runtime modding, plugin debugging, or live patching.

Now fully compatible with **BepInEx**, **Thunderstore Mod Manager**, and **r2modman** setups.

---

## 🆕 What's New in v2.7 (Latest Release)

### ⚙️ Smart Injection Router (NEW)

Automatically detects where BepInEx or Thunderstore profiles are located and injects accordingly — no manual path setup needed.

**Detection Priority:**

1. Standard BepInEx in game directory
2. Thunderstore profile under `%AppData%\\Thunderstore Mod Manager`
3. r2modman profile under `%AppData%\\r2modman`

**Status Messages:**

* `Injecting via BepInEx Receiver...` → Standard setup detected ✅
* `Injecting via Thunderstore BepInEx...` → Thunderstore/r2modman detected ✅
* `BepInEx detected! Receiver plugin required.` → Receiver missing ⚠️

### 🧩 Receiver Auto-Detection

The injector automatically looks for `SharpMonoInjectorTheHolyOneZEdition.dll` under any of these valid plugin locations:

```
GameFolder\\BepInEx\\plugins\\
AppData\\Roaming\\Thunderstore Mod Manager\\DataFolder\\<GameName>\\profiles\\Default\\BepInEx\\plugins\\
AppData\\Roaming\\r2modman\\profiles\\<GameName>\\BepInEx\\plugins\\
```

No need for manual file placement — the router finds it automatically.

### 💉 Safe Double Injection (Improved)

The Receiver now performs an internal two-phase injection to prevent Unity/Photon crashes and ensure stable DLL loading. Logs clearly indicate both passes.

### 🪄 Quality of Life

* Smart Router remembers last successful injection path per game.
* Added **Receiver Ping Test** button to verify communication before injection.
* Enhanced error recovery when receiver pipe isn’t found.
* Rewritten detection logic for Thunderstore profiles (now scans correctly under AppData).

---

## 🔌 SharpMonoInjector Receiver Plugin v2.7

A companion **BepInEx plugin** that safely receives injections from SharpMonoInjector and handles them internally via named pipes.

### 📦 Installation

#### **If Using Thunderstore or r2modman (Recommended)**

Simply drag the receiver DLL here:

```
AppData\\Roaming\\Thunderstore Mod Manager\\DataFolder\\<GameName>\\profiles\\Default\\BepInEx\\plugins\\SharpMonoInjectorTheHolyOneZEdition.dll
```

> Works even if not in a subfolder – BepInEx still loads it automatically.

#### **If Using Standard BepInEx Installation**

```
YourGameFolder\\BepInEx\\plugins\\SharpMonoInjectorTheHolyOneZEdition.dll
```

---

### 🔧 How It Works

**Communication:** Named Pipe → `SharpMonoInjectorPipe_THOZE`

**What It Does:**

1. Waits for injection requests from SharpMonoInjector.
2. Blocks problematic components (like PhotonView) that crash on injection.
3. Safely loads DLLs in a BepInEx-compatible way.
4. Performs automatic double-injection to ensure success.

**Logs:**

* `GameFolder/SmiReceiverLog.txt` – Injection receiver logs
* `BepInEx/LogOutput.log` – BepInEx system logs

---

## 🖥️ Usage

1. Open **SharpMonoInjector v2.7 TheHolyOneZ Edition**.
2. Select your target game process.
3. Choose your DLL, namespace, class, and method.
4. Click **Inject** once.
5. Watch the log — router selects the right injection path automatically.

**Successful Status Example:**

```
[SmartRouter] Thunderstore BepInEx detected
[Pipe] Connected successfully
[Double Injection Complete]
```

---

## 🧠 Troubleshooting

### "Connection timed out"

* Ensure the game is running.
* Check the Receiver plugin is installed in one of the listed plugin paths.
* Restart the game.

### "Receiver not found"

* Plugin is missing or placed in the wrong BepInEx profile.
* Verify both BepInEx and the Receiver loaded successfully in `LogOutput.log`.

### "BepInEx detected! Receiver plugin required."

* BepInEx found, but receiver not loaded.
* Install the DLL to the `BepInEx/plugins/` directory for your environment.

### Still Crashes?

* Check `SmiReceiverLog.txt` and `LogOutput.log`.
* Look for these success indicators:

  * `[BLOCKED] PhotonView` → protection active ✅
  * `Double injection complete!` → success ✅

---

## 📊 Technical Summary

| Component             | Description                                   |
| --------------------- | --------------------------------------------- |
| **Receiver**          | BepInEx plugin that handles injections safely |
| **Injector**          | SharpMonoInjector v2.7 GUI/CLI tool           |
| **Pipe Name**         | `SharpMonoInjectorPipe_THOZE`                 |
| **Supported Loaders** | BepInEx, Thunderstore, r2modman               |
| **Log Files**         | `SmiReceiverLog.txt`, `DebugLog.txt`          |

---

## 🧩 Compatibility

✅ **BepInEx:** 5.4.x and higher
✅ **Thunderstore:** Full support (AppData-based profiles)
✅ **r2modman:** Fully supported (auto-detected paths)
✅ **Mono/Unity Games:** Compatible with all standard Unity/Mono titles

---

## 🧾 Version History

### v2.7 (Smart Router Edition)

* ✨ Added Smart Injection Router (auto-detects BepInEx/Thunderstore/r2modman)
* ✨ Added Receiver auto-location and ping verification
* 🧠 Improved double-injection stability
* 🪄 Enhanced Thunderstore path scanning
* 🧰 Added receiver test utilities and fallback recovery
* 🐛 Fixed rare pipe timeout issues

### v2.6

* Added real-time log viewer and process monitor
* Introduced profile management
* Improved stealth injection system and UI persistence

### v2.5

* Introduced Stealth Injection with thread hiding and randomization
* Dark theme visual overhaul

---

## 👥 Credits

**Developed by:** TheHolyOneZ
**Enhanced and documented with:** GPT-5

**Special Thanks:**

* BepInEx Team – For the base framework
* Thunderstore & r2modman developers – For community mod tools
* Warbler & wh0am1 – Original SharpMonoInjector creators

---

## ⚠️ Disclaimer

This software is intended for **educational and legitimate modding or research purposes only.**
Unauthorized use (e.g., cheating, exploiting multiplayer titles) is **strictly prohibited.**

---

**Design, modernization, and enhancements by [TheHolyOneZ](https://github.com/TheHolyOneZ)**
*Documentation enhanced with GPT-5 for clarity and technical precision.*
