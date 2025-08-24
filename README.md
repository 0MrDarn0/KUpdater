📦 KUpdater – KalOnline Client Updater
KUpdater ist ein moderner, vollständig in C# (.NET 8) entwickelter Client‑Updater für das MMORPG KalOnline. Er kombiniert eine flexible Lua‑Skripting‑Engine mit einem vollständig selbst gezeichneten WinForms‑UI auf Basis von Layered Windows – für maximale Anpassbarkeit und ein individuelles Look‑and‑Feel.

✨ Features
🎨 Theme‑System

Hintergrundgrafiken, Layout‑Offsets und Farben frei konfigurierbar

Themes werden in Lua definiert und können live gewechselt werden

Fallback‑Mechanismus, falls Dateien fehlen oder fehlerhaft sind

🖌 Custom Rendering

Komplett selbst gezeichnete Oberfläche (kein Standard‑WinForms‑Look)

Unterstützung für transparente Fenster und Anti‑Aliasing

⚙ Lua‑Integration

UI‑Elemente wie Labels und Buttons können direkt aus Lua erstellt werden

Lua‑API für Fenstergröße, Theme‑Laden und UI‑Interaktion

🖱 UI‑System

IUIElement‑Interface für eigene Steuerelemente

UIButton und UILabel als Beispiele

Hover‑ und Klick‑Effekte mit themenabhängigen Grafiken

🚀 Launcher‑Funktionen

Startet das Spiel mit Parametern

Öffnet direkt die Spieleinstellungen

Schließt sich automatisch nach dem Start

📂 Projektstruktur
Code
KUpdater/
 ├── UI/                # Renderer, UIManager, UI-Elemente
 ├── Scripting/         # LuaManager, Theme-Handling
 ├── Resources/         # Theme-Grafiken, Standard-Assets
 ├── Lua/               # Lua-Skripte & Themes
 └── MainForm.cs        # Hauptfenster & Event-Handling
🚀 Installation & Nutzung
Repository klonen:

bash
git clone https://github.com/<dein-user>/KUpdater.git
In Visual Studio oder Rider öffnen.

Abhängigkeiten wiederherstellen und Projekt kompilieren.

Lua‑Skripte und Ressourcen in den entsprechenden Ordner legen.

KUpdater.exe starten.

🛠 Anforderungen
.NET 8 SDK

Windows (Layered Window API)

Lua‑Dateien & Theme‑Ressourcen

📜 Lizenz
Dieses Projekt steht unter der GPL‑3.0 Lizenz. Details siehe LICENSE.txt.