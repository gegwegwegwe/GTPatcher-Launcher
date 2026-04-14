# We now live on Codeberg. https://codeberg.org/GT-Archive-Team/GTPatcher-Launcher


<div align="center">
    <a href="https://github.com/gtarchiveteam/GTPatcher-Launcher/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/gtarchiveteam/GTPatcher-Launcher?style=flat"></a>
    <a href="https://github.com/gtarchiveteam/GTPatcher-Launcher/releases/latest">
    <img src="https://img.shields.io/github/downloads/gtarchiveteam/GTPatcher-Launcher/total?style=flat"></a>
    <a href="https://discord.gg/X2KX2Yc2eR">
    <img src="https://img.shields.io/discord/1193649345434751077?label=Discord&style=flat"></a>
</div>
<div align="center">
  <img src="https://raw.githubusercontent.com/gtarchiveteam/Assets/main/Banners/gtpl-banner.png">
</div>

# GT Patcher Launcher

A modern launcher to download and patch historical versions of Gorilla Tag via Steam Depot, with custom patches applied.

Made using [XdeltaSharp](https://github.com/pleonex/xdelta-sharp), [DepotDownloader](https://github.com/SteamRE/DepotDownloader) and [Avalonia UI](https://avaloniaui.net/).

## ✨ Features

- **Multiple Historical Versions** - Download and play various Gorilla Tag builds from different time periods
  <details>
  <summary>Currently available patches</summary>
      <ul>
          <li>🎄 Holiday Overstock (Manifest: 5272615492296865291)</li>
          <li>🌈 Neon Colors (Manifest: 1831142320395008381)</li>
          <li>🏔️ Mountains Beta (Manifest: 9066807640258182301)</li>
          <li>🎃 Halloween 2022 (Manifest: 4819580441710160978)</li>
          <li>🚀 Steam Release (Manifest: 2218992975128065135)</li>
          <li>🔧 Stick Turn Fix (Non-Steam build)</li>
      </ul>
  </details>
- **Modern UI** - Clean, responsive interface with real-time download progress tracking
- **Smart Build Management** - Each version installed in separate folders to avoid conflicts
- **Automatic Patching** - Seamlessly applies xdelta patches to game assemblies
- **Cross-Platform** - Works on Windows and Linux (with Proton)
- **Proper Cloudscripts** - Cosmetic verification, whitelist, etc.
- **Smooth Turn & Private Lobbies** - Yes, they actually work!

## 📸 Screenshots

![Installations Tab](https://via.placeholder.com/800x600.png?text=Installations+Tab)
*Select from available builds with detailed descriptions and manifest information*

![Settings Tab](https://via.placeholder.com/800x600.png?text=Settings+Tab)
*Configure your Steam account and installation path*

## 🚀 Quick Start

1. Enter your Steam username in the Settings tab
2. Choose an installation directory
3. Select a build version from the Installations tab
4. Click "Play!" to download and launch

### First-Time Setup

- You'll be prompted for your Steam password when downloading
- Steam Guard verification may be required (email/code)
- Each build is downloaded separately (~2-3 GB per version)

## 📖 How It Works

1. **Build Information** - Fetches available versions from [patchIndex.json](https://raw.githubusercontent.com/gtarchiveteam/patches/refs/heads/main/patchIndex.json)
2. **Steam Depot Download** - Uses DepotDownloader to download specific manifest versions via Steam
3. **Patch Application** - Applies xdelta3 patches to `Assembly-CSharp.dll` for custom modifications
4. **Launch** - Starts the patched game executable

## ❓ FAQ

#### Why do you need my Steam password?
We use your Steam account to download the game files through Steam's depot system, as we only distribute our patches (not full game files).

We use [DepotDownloader](https://github.com/SteamRE/DepotDownloader) for this. If you're uncomfortable providing your Steam credentials, you can:
- Audit DepotDownloader's source code
- Review GTPatcher's source code
- Manually download and patch versions yourself

#### Will I get VAC banned?
No. Gorilla Tag does not use VAC (Valve Anti-Cheat), and using DepotDownloader to download game files you own is legitimate.

#### I don't want to open the Launcher every time. How do I add builds to my library?
You can add the `.exe` files as non-Steam games:
1. Open Steam → Games → Add a Non-Steam Game
2. Browse to the build folder (e.g., `YourPath/MtnsBeta/GorillaTag.exe`)
3. Add it to your library
4. Right-click → Properties to configure controller/Proton settings

#### Where are the game files stored?
By default, builds are stored in your chosen installation path, with each version in a separate subfolder named after its shorthand (e.g., `MtnsBeta`, `Hallo22`).

#### Can I play online with these versions?
Yes! Most versions can connect to community servers. However:
- Official Another Axiom servers may restrict older versions
- Community servers like ours support multiple versions
- Don't ask about GTPatcher in official Another Axiom channels

#### I don't see my question here
Check the organization's [profile](https://github.com/gtarchiveteam) or join our [Discord server](https://discord.gg/X2KX2Yc2eR).

## 🐧 Linux Notes

#### Password prompt doesn't appear!
Due to how process spawning works on Linux, you must start the Launcher from a terminal to see the Steam password prompt:

```bash
cd /path/to/GTPatcher
./GTPatcher
```

#### Running on Steam Deck / Proton
1. Add the build as a non-Steam game
2. Set compatibility to Proton Experimental or latest version
3. Launch from Steam library

## 🛠️ Building from Source

### Requirements
- .NET 8 SDK or later
- Avalonia UI dependencies

### Build Steps

```bash
# Clone the repository
git clone https://github.com/gtarchiveteam/GTPatcher-Launcher.git
cd GTPatcher-Launcher

# Build
dotnet build

# Publish (optional)
dotnet publish -c Release -r win-x64 --self-contained
```

## 📦 Dependencies

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [DepotDownloader](https://github.com/SteamRE/DepotDownloader) - Steam content download utility
- [XdeltaSharp](https://github.com/pleonex/xdelta-sharp) - Binary patching library
- [MessageBox.Avalonia](https://github.com/AvaloniaCommunity/MessageBox.Avalonia) - Modern message dialogs
- [Newtonsoft.Json](https://www.newtonsoft.com/json) - JSON parsing

## ⚠️ Disclaimer

**GTPatcher is not affiliated with or endorsed by Another Axiom.**

GTPatcher is not intended to aid or facilitate piracy. All game builds require legitimate ownership through Steam (except for pre-release builds never released on Steam).

**Please do not inquire for help about GTPatcher in any official Another Axiom community.** Doing so is both annoying to Another Axiom employees and to us. Don't do it!

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Credits

- **GT Archive Team** - Development and maintenance
- **Another Axiom** - Creating Gorilla Tag
- **SteamRE** - DepotDownloader
- **PleIades** - XdeltaSharp
- **AvaloniaUI Team** - UI framework

---

<div align="center">
  <sub>Built with 🦍 by the GT Archive Team</sub>
</div>
