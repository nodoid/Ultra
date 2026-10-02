# The Ultra

A MonoGame remake for **Android**, **iOS**, **macOS** and **Windows** of *The Ultra*, the 1983 arcade shooter published by PSS for the Oric-1 and Oric Atmos.

Written by **Paul F. Johnson**.

![Splash](art/splash-1920x1080.png)

## The game

- **The original 16 sheets.** The alien graphics, animation frames, movement paths and formations come from the original tape (see [Original data](#original-data)). Aliens step one Oric text cell at a time along their sheet's path and wrap around the screen edges, just as on the Oric. After sheet 16 you get the original "WELL DONE!" message and a 5,000 point bonus, then the sheets repeat, faster.
- **Gun overheating.** The machine gun heats up with every shot. If it reaches the limit it locks until it has cooled. The temperature carries over from one sheet to the next, so you start each sheet with the gun as hot as you left it.
- **Original rules.** You have 5 lives, with bonus lives at 2,000 and 10,000 points. Aliens score 10 points on sheet 1, 20 on sheet 2 and so on; the rate stops rising after sheet 8.
- **Oric-style presentation.** A 240x224 screen laid out like the Oric's 40x28 text screen, with a red status bar and "PREPARE FOR HYPERSPACE JUMP" between sheets. In-game sprites are drawn at 1.5x (the screen is rendered at 2x internally so every pixel stays even). It uses the eight Oric colours, a 6x8 character-cell font, and AY-style square-wave and noise sound effects synthesised at runtime.

### High scores

The Hall of Fame keeps the top 10 scores. If you make the table, you enter your name (up to 10 characters) on an on-screen keyboard or a hardware keyboard. The table is saved to the app's private storage (`LocalApplicationData/ultra_hiscores.txt`) as soon as you enter your name and is loaded again when the game starts, so scores carry over from one launch to the next.

### Controls

The game is locked to landscape.

- **Move:** tilt the device like a steering wheel. Lower the right edge to go right and the left edge to go left. The further you tilt, the faster the ship moves. A tilt meter sits in the left margin.
- **Fire:** tap anywhere on the screen. Hold your finger down for rapid fire, but watch the heat gauge.
- **Pause:** the pause icon in the top-left corner, or the Back button on Android. The pause menu offers Resume, Options and Quit.

### Options

Tap the slider icon in the top-left corner of the title screen, or choose **Options** from the pause menu.

- **Tilt sensitivity** runs from 1 (low) to 10 (high). Each step needs about 20% less tilt to reach full speed. At the default of 5, full speed takes about 17°.
- **Invert tilt** reverses the steering direction.
- **Tilt test** shows a live preview of the ship, so you can tune the setting before playing.

Options are saved (`ultra_settings.txt`) and restored when the game starts.

### Title screen

The title screen cycles through three pages: **How to Play**, **Points per Alien** (every alien with its name and score) and the **Hall of Fame**. To move between pages yourself, swipe left or right, tap the ‹ › buttons in the bottom corners, or use the arrow keys. A page you choose stays on screen for longer before the cycle continues. Tap anywhere else to start a game.

On **macOS and Windows** the game runs in a resizable window and is played with the mouse and keyboard:

- **Move:** move the mouse and the ship follows the pointer (at the same top speed as the keys), or press **Z** (left) and **X** (right). The arrow keys work too. Pressing a key hands steering to the keyboard until the mouse moves again.
- **Fire:** left click or **Space** (or Ctrl). Hold for rapid fire.
- **Pause:** **P**, **Esc** or a click on the pause icon in the top-left corner. On the title screen, click or press Space to start; on Windows, Esc there quits (on a Mac use Command-Q).
- **Full screen:** F11 or Alt+Enter, or Command-Control-F on a Mac.
- **Options** (O on the title screen, or the slider icon) has Mouse steering on/off and Full screen on/off, and lists the controls. The How to Play page shows the desktop controls.

Tilt uses the gyroscope-fused gravity vector: CoreMotion device motion on iOS and the `TYPE_GRAVITY` sensor on Android. On Android devices without a gyroscope it falls back to a filtered accelerometer. If the device has no motion sensor at all (for example the iOS Simulator), on-screen left and right buttons appear in the left margin instead. Hardware keyboards and game pads also work: arrow keys, Space or Ctrl to fire, P to pause, Esc to go back.

## Project layout

| Project | Purpose |
| --- | --- |
| `Ultra.Core` | All game code (net10.0, MonoGame 3.8.5). It needs no content pipeline because graphics, font and sound are generated in code. |
| `Ultra.Android` | Android host activity: fixed `SensorLandscape`, immersive full screen, adaptive icon, splash theme, tilt sensor |
| `Ultra.iOS` | iOS host: landscape only, app icon set, `LaunchScreen.storyboard` splash, CoreMotion tilt sensor |
| `Ultra.Desktop` | macOS and Windows host (MonoGame DesktopGL): window icon, `macOS/` (Info.plist, sandbox entitlements, native launcher, `.icns`) and `Windows/` (MSIX `AppxManifest.xml` and tile assets) |
| `tools/build_desktop.sh` | Builds the signed Mac App Store `.pkg` and the Microsoft Store `.msix` packages (x64 and arm64) |
| `tools/make_msix.py` | Writes an unsigned `.msix` without Windows tools (makeappx only runs on Windows) |
| `tools/capture_screenshots.sh` | Captures the Mac App Store and Microsoft Store screenshots from the running game |
| `tools/generate_art.py` | Regenerates every icon, splash image and store graphic from the game's own sprite and font data (requires Pillow) |
| `tools/extract_original.py` | Regenerates `Ultra.Core/OriginalData.cs` from a tape image of the original game |
| `tools/check_listing.py` | Checks the store listing text against each store's character limits |
| `store/` | Store screenshots, icons, feature graphic, listing text (`LISTING.md`) and privacy policy (`PRIVACY.md`) |

## Original data

`Ultra.Core/OriginalData.cs` holds the aliens, paths and formations of the 16 sheets. `tools/extract_original.py` generates it from a tape image of the original (the Oric-1/Atmos `.tap`), and the script's header describes the data format found by disassembling the game. The tape image itself isn't included in this repository.

Because the remake draws aliens at 1.5x, formations are re-spaced so aliens don't overlap. Aliens that share a track are spread further apart along it, and fixed formations are scaled up about their centre. Some sheets have fewer aliens than the original as a result. `OriginalData.OriginalCounts` records the original numbers.

## Store submission

`store/LISTING.md` has the text for every Google Play and App Store field, with answers for the content-rating, data-safety and privacy questionnaires. It also covers how to build the upload files. Read its note about rights to the original game before publishing.

## Building

```sh
dotnet build Ultra.Android/Ultra.Android.csproj
dotnet build Ultra.iOS/Ultra.iOS.csproj
dotnet run --project Ultra.Desktop        # macOS or Windows
```

You need the .NET 10 SDK with the `android` and `ios` workloads installed. The desktop build needs no workloads.

To build the Mac App Store `.pkg` and the Microsoft Store `.msix` files into `release/`, run `tools/build_desktop.sh` (on a Mac). See `store/LISTING.md`.

To build the signed store packages (Google Play .aab/.apk and App Store .ipa) into `release/`, run `tools/build_release.sh`. See `store/LISTING.md` for details of the signing setup.

## Credits

- Original game: *The Ultra* by J.B. Marshall, published by PSS in 1983 for the Oric-1 and Atmos.
- Remake: Paul F. Johnson.
