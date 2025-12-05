[![.NET Desktop CI](https://github.com/0MrDarn0/KUpdater/actions/workflows/dotnet.yml/badge.svg)](https://github.com/0MrDarn0/KUpdater/actions/workflows/dotnet.yml)
<img width="1105" height="641" alt="kUpdater_screenshot" src="https://github.com/user-attachments/assets/7cc468dc-0173-4b6d-b9fe-c75a45f047f8" />

# 📦 KUpdater – Universal Game Launcher & Updater
KUpdater is a modern, fully customizable launcher and updater originally built for KalOnline, but now adaptable to any game.

## ✨ Features

### 🖌 Custom Rendering
Entirely custom‑drawn interface (no default WinForms look)
Fully configurable background images, layout offsets
Support for transparent windows and anti‑aliasing for smooth visuals

### 🖱 UI System
IControl interface for building custom controls
Includes Button, Label, ProgressBar and TextBox as reference implementations
Hover and click effects with theme‑dependent graphics

### ⚙ Lua Integration
Create UI elements such as labels, buttons and textboxs directly from Lua
Enables dynamic, script‑driven customization


### 🔧 Tech Stack
C# / .NET for core logic and UI rendering
Lua for scripting and theme control
SkiaSharp for advanced graphics



## 🛠️ UpdateCreator Tool

The **UpdateCreator Tool** (`UpdateBuilder.exe`) prepares update packages for distribution.

1. Place all new or modified files into the `Update/` folder.  
2. Run `UpdateBuilder.exe`.  
3. Enter the **PackageUrl** (must be a valid `http` or `https` URL).  
4. The tool generates the following files inside `Upload/`:  
   - `update.zip` → compressed archive of all update files  
   - `update.json` → metadata file containing version, URL, and SHA256 hashes  
   - `version.txt` → current version string (default: `1.0.0`)  
5. Upload these files to your server.  


### ⚙️ Updater Configuration
The Updater reads its configuration from a Lua file located in the same folder as KUpdater.exe:

Path: kUpdater/Lua/base.lua
```
Base = {
    Url = "http://darn.bplaced.net/KUpdater/", -- must match PackageUrl
    Language = "en", -- supported: "en", "de", ...
}
```

### Important
**The Url in base.lua must match the PackageUrl you entered in the UpdateCreator Tool.**
This ensures the Updater knows where to check for update.json and download update.zip.


#### 🚀 Typical Workflow:
1. Build update package with UpdateCreator Tool.
2. Upload update.zip, update.json, and version.txt to your server.
3. Configure the Updater (base.lua) with the same Url.
4. Run KUpdater → it checks update.json, verifies hashes, and applies updates.



## 📜 License
**This project is licensed under the [GNU General Public License v3.0](./LICENSE.txt).
Copyright (c) 2025 Christian Schnuck**

### That means:
**✅ You are free to use this software for any purpose.
✅ You are free to study, modify, and improve the code.
✅ You are free to share the software and your modifications with others.
🔄 If you distribute modified versions, you must also share the source code under the same license.
🙌 Attribution is required — please keep the original copyright notices and clearly mark your changes.**
