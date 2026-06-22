<p align="center">
<img alt=".NET 10" src="https://img.shields.io/badge/.NET_10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
<img alt="Windows" src="https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white" />
<img alt="WPF" src="https://img.shields.io/badge/WPF-5C2D91?style=for-the-badge&logo=windows&logoColor=white" />
<img alt="OpenCV" src="https://img.shields.io/badge/OpenCV-5C3EE8?style=for-the-badge&logo=opencv&logoColor=white" />
<img alt="NAudio" src="https://img.shields.io/badge/NAudio-1DB954?style=for-the-badge&logo=soundcharts&logoColor=white" />
<img alt="License" src="https://img.shields.io/badge/GPL--3.0-red?style=for-the-badge" />
</p>

# 🎣 ToxicFishing

**ToxicFishing is a simple, lightweight automation tool that streamlines fishing in World of Warcraft by detecting your bobber on-screen with computer vision and reacting to bites automatically.**

> ⚠️ Automation is prohibited on many World of Warcraft servers and may violate the game's Terms of Service or rules. ToxicFishing is provided as sample tooling — use it responsibly, only where permitted, and entirely at your own risk.

## 📋 What ToxicFishing Does For You

ToxicFishing gives you hands-free convenience with a deliberately low profile:

- **🎣 Fish Hands-Free** — Automates casting, waiting, and hooking so you can step away from the keyboard
- **⏳ Reclaim Your Time** — Offload the repetitive grind while you do something else
- **🎯 Catch Every Bite** — Reacts the instant a bite is detected, cast after cast
- **🕵️ Stay Low-Profile** — Reads only the pixels and audio you can already see and hear, and sends standard OS input
- **✅ Peace of Mind** — Fully open-source and auditable, so you always know exactly what it does

## ⭐ Features

- **🧠 Full-Screen Vision** — Scans the entire primary monitor with OpenCV (OpenCvSharp), so the bobber is found wherever it lands
- **🎨 HSV Colour + Template Matching** — Combines HSV colour masking with `bobber.png` template matching to lock onto the float and ignore the game's red UI
- **🔊 Sound-Based Bite Detection** — Listens for the splash on the game's own audio session via WASAPI, a more reliable trigger than on-screen motion
- **⚡ Fast Reaction Time** — Hooks as soon as a bite is detected
- **🔁 Continuous Loop** — Re-casts after each catch for uninterrupted sessions, with periodic lure re-application
- **⏱️ Scheduled Run Time** — Optionally auto-stops after a chosen number of minutes
- **🪟 Pick Your Window** — Select the World of Warcraft window the bot should drive from the UI
- **🕵️ Least-Invasive by Design** — Sends standard Windows input messages and reads only on-screen pixels; **no DLL injection, no memory reading/writing, no kernel hooks**
- **🎲 Human-Like Behaviour** — A built-in *Humanizer* randomises every timing and adds a per-session fatigue model, avoiding rigid, robotic patterns
- **🧩 Minimal Overhead** — Built to be lightweight so it won't noticeably impact gameplay
- **🔍 Transparent** — Open-source code you can audit and adapt to your needs
- **💯 Completely Free** — Open-source and free to use, forever

## 🚀 Usage

Before your first run, prepare World of Warcraft. The bot drives fishing through **fixed keyboard keys**, so the right abilities must sit on your action bar:

- **1️⃣ Action bar slot `1` → Fishing** — Drag your **Fishing** ability onto the action button bound to key **`1`**. The bot presses `1` to cast, so without this it will never start fishing.
- **2️⃣ Action bar slot `2` → Lure** — Place the **fishing lure** you use (the lure item itself, or a macro that applies it) on the action button bound to key **`2`**. The bot presses `2` at startup and re-applies it about every 10 minutes. Leave the slot empty if you don't use a lure — the extra keypress is then harmless.
- **⌨️ Keep the default `1`/`2` keybinds** — Those action buttons must still be bound to the keyboard keys `1` and `2` (WoW's default), because those are the exact keys the bot sends. The bot also taps **Spacebar** once on start (a harmless jump) to shake the character out of any idle/AFK state.
- **✅ Auto Loot** — Enable it so caught fish are picked up automatically
- **🎣 Fishing Rod** — Equip a fishing pole and stand in a valid fishing spot
- **🖥️ Display** — Put the game on your **primary monitor** in **borderless fullscreen**
- **🔊 Sound** — Keep **Sound Effects** on with the volume up; bites are detected from the in-game splash

Then drive everything from the app:

- **🪟 Select Game Window** — Pick the World of Warcraft window the bot should drive (use **Refresh** to re-scan)
- **▶️ Start / Stop Bot** — Begin or end an automated fishing session
- **⏱️ Scheduled Run Time** — Optionally auto-stop after a chosen number of minutes
- **🖼️ Change Bobber** — Swap in a crop of your own bobber for the best detection at your resolution and UI scale
- **📊 Activity Panel** — Watch live casts, bites, and warnings as they happen

## 🛠️ Building from Source

### Prerequisites

- **Windows 10 / 11** — ToxicFishing is Windows-only (WPF, Windows Forms, Win32 input, and the native OpenCV runtime)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Build and Run

```bash
git clone https://github.com/your-username/ToxicFishing.git
cd ToxicFishing
dotnet build ToxicFishing.sln -c Release
dotnet run --project ToxicFishing.App
```

### Project Structure

| Project | Purpose |
|---|---|
| `ToxicFishing.Shared` | Shared kernel — platform-neutral primitives: `PixelPoint`, `HsvRange`, and the `AppOptions` tuning constants |
| `ToxicFishing.Activity` | Status/activity reporting (`IActivityReporter`) surfaced to the UI |
| `ToxicFishing.Humanization` | Humanized, non-deterministic timing (`IHumanizer`) |
| `ToxicFishing.Vision` | OpenCV bobber detection + screen capture (`IVision`) |
| `ToxicFishing.Input` | Win32 game input + process location (`IInput`, `IProcessLocator`) |
| `ToxicFishing.Audio` | Sound-based bite detection from the game's splash (`IAudioBiteDetector`) |
| `ToxicFishing.Fishing` | Fishing-loop orchestration: bot controller and fishing session |
| `ToxicFishing.App` | WPF MVVM desktop UI and the composition root that wires the modules |
| `ToxicFishing.Test` | NUnit + NSubstitute test suite, organized per module |

## 🚀 Roadmap

ToxicFishing is constantly evolving. Here's what we're planning for future releases:

- **🧠 Smarter Visual Tracking** — Improved bobber classification and fewer false positives
- **👥 Community-Driven Development** — We highly value community suggestions and contributions to guide the project's future

## 📸 Screenshots

<p align="center">
  <img width="60%" alt="ToxicFishing" src="https://placehold.co/1200x675?text=ToxicFishing+Screenshot" />
</p>

## 🔍 Security Notes

- 🖥️ This project automates user input and reads screen pixels; treat it as sensitive tooling
- 🛡️ It is least-invasive **by design** — it does not inject code or read game memory — but it still drives input into another application; review the code before running it
- ⚠️ Use it only on accounts you can afford to risk, and only where automation is permitted

## 💡 How to Contribute

- We welcome contributions from everyone, regardless of your technical background!
- Every contribution matters and helps make this project better for everyone!

#### For Non-Developers

You can make valuable contributions too:
- **Report Bugs**: Found something that doesn't work? Let us know by opening an issue.
- **Suggest Features**: Have ideas for new features or improvements? We'd love to hear them.
- **Documentation**: Improve or clarify our setup notes and troubleshooting.
- **User Testing**: Share results across different resolutions, UI scales, and game settings.
- **Spread the Word**: Share the project on social media, blog about it, or tell your friends.

#### For Developers

1. Fork the repository
2. Create a feature branch from `develop`
3. Implement your changes with documentation and tests
4. Submit a pull request

We especially welcome contributions around detection reliability, calibration, and configuration UX.
