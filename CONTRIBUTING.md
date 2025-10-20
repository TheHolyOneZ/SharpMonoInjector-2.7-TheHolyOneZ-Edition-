---

# CONTRIBUTING.md

# Contributing to SharpMonoInjector – TheHolyOneZ Edition v2.7

Thank you for your interest in improving **SharpMonoInjector** 🎯
This guide explains how to set up your environment, report bugs, and submit pull requests safely.

---

## ⚙️ Development Setup

### Prerequisites

* Visual Studio 2022 or VS Code
* .NET Framework 4.8 SDK or newer
* Administrator privileges for testing injection features
* Optional: BepInEx 5.4+ (for Receiver Plugin testing)

### Building

1. Fork and clone the repository:

   ```bash
   git clone https://github.com/TheHolyOneZ/SharpMonoInjector-2.7-TheHolyOneZ-Edition-.git
   cd SharpMonoInjector-2.7-TheHolyOneZ-Edition-
   ```
2. Open the solution file (`SharpMonoInjector.sln`) in Visual Studio.
3. Restore NuGet packages automatically on load.
4. Build in **Release** or **Debug** mode.
5. Test the executable under a sample Unity/Mono process.

---

## 🧩 Coding Guidelines

* Use **C# 10+ conventions** and **4-space indentation**.
* Name methods and variables clearly (`PascalCase` for methods, `camelCase` for locals).
* Avoid large methods; break logic into smaller reusable parts.
* Document public methods using XML comments.
* Keep the **UI theme consistent** (dark theme with green accents `#00E676`).
* Test both **x86** and **x64** builds before committing.

---

## 🧠 Feature Additions

* Ensure backward compatibility with existing injection workflows.
* Add detailed log messages to the integrated log viewer (`DebugLog.txt`).
* Follow the existing **logging color scheme**:

  * `Info`: Blue
  * `Success`: Green
  * `Warning`: Orange
  * `Error`: Red
* If adding UI elements, maintain consistent spacing, corner rounding, and hover effects.

---

## 🧪 Testing Guidelines

Before submitting a PR:

1. Test normal and stealth injection modes.
2. Verify detection of BepInEx, Thunderstore, and r2modman profiles.
3. Confirm logs are properly written to `DebugLog.txt`.
4. Validate that the **Receiver Plugin** communicates via pipe correctly.
5. Ensure the app runs safely without administrator privileges (limited features).

---

## 🐛 Reporting Bugs

Include in your bug report:

* Steps to reproduce the issue
* Expected vs. actual behavior
* Game/environment (Mono/Unity version)
* Log output (`DebugLog.txt`)
* Screenshots if relevant

Use the **GitHub Issues** tab and apply labels like `bug`, `enhancement`, or `question`.

---

## 🧾 Submitting Pull Requests

1. Fork the repository.
2. Create a new branch:

   ```bash
   git checkout -b feature/my-feature
   ```
3. Commit with a clear message:

   ```bash
   git commit -m "Add Smart Router fallback for r2modman profiles"
   ```
4. Push your branch:

   ```bash
   git push origin feature/my-feature
   ```
5. Open a Pull Request targeting the `main` branch.

PRs should:

* Describe what the change does and why it’s needed.
* Include screenshots for UI modifications.
* Reference related issues (if any).

---

## 💬 Discussions & Questions

For non-bug questions, use the **Discussions** tab or start a new thread labeled `idea` or `support`.

---

## ⚠️ Ethical & Legal Notice

SharpMonoInjector is intended **for educational, debugging, and legitimate modding use only**.
Do **not** use it for:

* Cheating or bypassing anti-cheat systems
* Unauthorized game modification
* Any malicious purpose

Violating these terms may result in your PR being rejected or blocked.

---

## 🙏 Credits

* **TheHolyOneZ** – Modernization, UI, stealth system, Smart Router, Thunderstore integration and Profile System, Pipe Injection
* **wh0am1** – Technical fixes and Mono patching
* **Warbler** – Original SharpMonoInjector base

Happy coding! 💉

