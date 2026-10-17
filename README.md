# DecimalSelector

A small Windows system tray app that lets you choose what the NumPad decimal key types: a **dot ( . )** or a **comma ( , )**, whatever your keyboard layout is.

## Install

Run `dist\DecimalSelector-Setup.exe`. It installs for the current user (no admin rights needed), adds a Start Menu entry, and can optionally start the app with Windows. Uninstall from **Settings > Apps > Installed apps**.

The installer is not code-signed, so on other PCs Windows SmartScreen may warn about it. Click **More info > Run anyway**.

## Use

- **Left-click** the tray icon to switch between dot and comma. Blue icon with `.` = dot, green icon with `,` = comma.
- **Right-click** for the menu: Comma, Dot, Start with Windows, Exit.
- Your choice is remembered.

Windows 11 may hide new tray icons behind the **^** arrow. To keep it visible, drag it onto the taskbar, or turn it on under **Settings > Personalization > Taskbar > Other system tray icons**.

## Build

Run `build.cmd`. It compiles `DecimalSelector.cs` with the C# compiler that comes with Windows (.NET Framework 4.8, nothing to install) into `build\DecimalSelector.exe`, then builds the installer from `installer.iss` if [Inno Setup 6](https://jrsoftware.org/isdl.php) is installed.
