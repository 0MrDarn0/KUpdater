# Kx – UI/Runtime Framework

![License: GPL-3.0](https://img.shields.io/badge/License-GPL--3.0-blue.svg)
![.NET 10](https://img.shields.io/badge/.NET-10-purple)
![Platform: Windows](https://img.shields.io/badge/Platform-Windows-blue)
![Status: Active](https://img.shields.io/badge/Status-Active-success)

`Kx` is a .NET 10 desktop UI/runtime framework featuring a plugin-driven window system, YAML-based markup, custom rendering, and a sample updater application built on top of it.

---

## 📑 Table of Contents
1. [Repository Overview](#repository-overview)  
2. [Current Architecture Direction](#current-architecture-direction)  
3. [Documentation](#documentation)  
4. [Quick Start](#quick-start)  
5. [File-Based Update Flow](#file-based-update-flow)  
6. [Asset and Configuration Boundaries](#asset-and-configuration-boundaries)  
7. [Plugin and Test Structure](#plugin-and-test-structure)  
8. [Goals of the Framework](#goals-of-the-framework)  
9. [License](#license)

---

## Repository Overview

### Framework & Applications
- **src/Kx** – runtime, window system, rendering, configuration loading, plugin infrastructure  
- **src/Kx.Sdk** – contracts for plugins, UI, logging, DI, window hosting, markup  
- **apps/KxUpdater** – updater application with its own `Assets` and file-based update client  
- **apps/KxUpdateBuilder** – desktop manifest builder for publishing file-based updates  
- **apps/KxUpdater/Plugins/KalTheme** – updater-specific visual plugin for KalOnline-style UI  
- **tests/Kx.Tests** – framework/runtime tests  
- **tests/KxUpdater.Tests** – updater application tests  

### Reusable Plugins
- **plugins** – reusable, cross-application plugins  
- **plugins/KalCipher** – reusable cipher plugin  
- **plugins/KalCipher.Tests** – tests for the cipher plugin  

### Examples
- **examples/Kx.Plugin.Example** – reference plugin demonstrating controls, actions, commands, themes, window definitions  
- **examples/Kx.Example.App** – minimal sample host application using the framework and example plugin  

---

## Current Architecture Direction

The codebase is being separated into:
- framework-generic infrastructure (`Kx`)  
- app-specific behavior (`KxUpdater`)  
- reusable plugins (`plugins`)  
- app-local plugins beside their owning app  
- plugin extension points via `Kx.Sdk`  
- learning/reference material under `examples`

Runtime bootstrap is composed through:
- `PluginRuntimeComposition`  
- `RuntimeUiComposition`  
- `RuntimeLoggingComposition`  
- `RuntimeShellComposition`

This keeps `RuntimeServiceConfiguration` focused on registering composed services rather than constructing them inline.

---

## Documentation

- **architecture.md** – project boundaries, runtime startup flow, composition model  
- **grid-splitter.md** – resizable grid dividers, YAML usage, layout recommendations  
- **plugins.md** – plugin model, registries, markup assets, example plugin walkthrough  
- **update-builder.md** – file-based update publishing flow, manifest structure, IIS notes  
- **windows-and-markup.md** – window lifecycle, YAML lookup, control layers, icon precedence, fallback UI behavior  

---

## Quick Start

### Run the updater application
Open the solution in Visual Studio 2026 or build from the repository root and run `apps/KxUpdater`.

### Run the update builder
Build and run `apps/KxUpdateBuilder`.  
It mirrors files from `Update` to `Upload` and writes a file-based `update.json` manifest.

### Run the sample application
Build and run `examples/Kx.Example.App`.

### Run tests
- Framework/runtime: `dotnet test tests/Kx.Tests/Kx.Tests.csproj`  
- Updater app: `dotnet test tests/KxUpdater.Tests/KxUpdater.Tests.csproj`  
- Reusable plugins: `dotnet test plugins/KalCipher.Tests/KalCipher.Tests.csproj`  

### Main entry points
- Updater startup: `apps/KxUpdater/Program.cs`  
- Update builder startup: `apps/KxUpdateBuilder/Program.cs`  
- Sample app startup: `examples/Kx.Example.App/Program.cs`  
- Runtime bootstrap: `src/Kx/App/Runtime.cs`  
- Base window behavior: `src/Kx/App/Window.cs`  
- Example plugin: `examples/Kx.Plugin.Example/Example.cs`  

---

## File-Based Update Flow

`KxUpdater` now uses a file-based manifest instead of a monolithic `update.zip`.

### Publication model
- `update.json` – describes all files plus `deletedFiles`  
- `news.yaml` – updater news entries  
- each file is downloaded directly from its relative path under the update base URL  

### Client behavior
- load `update.json`  
- compare local file hashes  
- download changed/missing files  
- remove files listed in `deletedFiles`  
- stage a replacement updater executable for self-update  

### Builder behavior
- read files from `Update`  
- mirror them into `Upload`  
- remove legacy `update.zip` and `version.txt`  
- write `update.json`  

See `docs/update-builder.md` for a walkthrough.

---

## Asset and Configuration Boundaries

Framework-generic path/loading infrastructure stays in `Kx`.

App-specific assets include:
- `Assets/Configs/*.yaml`  
- `Assets/Languages/*.yaml`  
- `Assets/Icons/*`  
- `Assets/Themes/<ThemeName>/*`

Resource ID mapping examples:
- `Icons:app.ico` → `Assets/Icons/app.ico`  
- `Themes:KalOnline:Frame:top_left.png` → `Assets/Themes/KalOnline/Frame/top_left.png`  

Examples:
- Updater assets: `apps/KxUpdater/Assets`  
- Sample app assets: `examples/Kx.Example.App/Assets`  

---

## Plugin and Test Structure

- App-local plugins: `apps/KxUpdater/Plugins/*`  
- Reusable plugins: `plugins/*`  
- Plugin inclusion via MSBuild `PluginProject` + `PluginCopy.targets`  
- Framework tests: `tests/Kx.Tests`  
- App tests: `tests/KxUpdater.Tests`  
- Plugin tests: `plugins/<PluginName>.Tests`  

---

## Goals of the Framework

- plugin-driven UI extension  
- YAML-defined windows and themes  
- custom-rendered desktop UI  
- explicit runtime composition  
- separation between reusable framework code and app-specific code  

---

## License

GPL-3.0 — see `LICENSE.txt`.
