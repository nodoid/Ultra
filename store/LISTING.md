# Store listing: The Ultra

Everything you need to paste into Google Play Console, App Store Connect (iOS and Mac) and Microsoft Partner Center. The character counts are within each store's limits (checked by `tools/check_listing.py`).

> **Before you submit:** The Ultra reproduces the aliens, movement patterns and rules of *The Ultra* (PSS, 1983, by J.B. Marshall). The alien graphics and formation data were extracted from the original tape. Both stores reject apps that use other people's intellectual property without permission, and each store may ask you to prove you have the rights. Make sure you have permission from whoever now owns the rights to *The Ultra*, or replace the original graphics and patterns, before you publish.

---

## Shared details

| Field | Value |
|---|---|
| App name | The Ultra |
| Developer / seller | Paul F. Johnson |
| Category | Games, then Arcade |
| Price | Free (or set your own) |
| Contains ads | No |
| In-app purchases | No |
| Support email | paul@all-the-johnsons.co.uk |
| Privacy policy URL | *(host `store/PRIVACY.md` publicly and paste the URL here)* |
| Website (optional) | *(your site)* |
| Supported devices | Android phones and tablets, iPhone, iPad, Macs with Apple silicon or Intel (macOS 12 or later), Windows 10/11 PCs (x64 and Arm) |
| Orientation | Landscape only |

---

## Google Play

**App name** (30 max)
```
The Ultra
```

**Short description** (80 max)
```
Classic Oric arcade shooter: 16 sheets of aliens, tilt to steer, tap to fire.
```

**Full description** (4000 max)
```
Blast your way through the sixteen alien sheets of The Ultra, a faithful remake of the 1983 Oric arcade shooter.

Every sheet has its own aliens and its own attack. Rows of marchers scroll across the sky, a diamond formation sways towards you, conga lines of saucers snake across the screen, and arrowheads, mushrooms, birds and frogs sweep down in loops and V shapes. Each alien animates and moves just like the Oric original, one chunky character cell at a time.

TILT TO STEER, TAP TO FIRE
Tilt your phone or tablet to move the ship and tap anywhere to fire. Hold your finger down for rapid fire, and adjust the tilt sensitivity in Options to suit you.

WATCH THE HEAT
Your machine gun overheats. Fire too long and it locks up until it cools, and the heat carries on into the next sheet, so keep an eye on the gauge.

CLASSIC SCORING
Aliens are worth 10 points on the first sheet, 20 on the second and so on, up to 80. You get bonus lives at 2,000 and 10,000 points, and a 5,000 point bonus for clearing all sixteen sheets.

HALL OF FAME
Get into the top ten and enter your name. Your scores stay on your device between games.

FEATURES
- 16 sheets of aliens with the original movement patterns
- Authentic Oric look: eight colours, chunky pixels and square-wave sound effects
- Tilt steering with adjustable sensitivity and an invert option
- Top-ten Hall of Fame with name entry
- Landscape play on phones and tablets
- No ads, no tracking, no internet connection needed

Written by PFJ. Based on the PSS Oric game.
```

**Graphics** (in `store/google-play/`)
| Asset | File | Required size |
|---|---|---|
| App icon | `icon-512.png` | 512 x 512 PNG |
| Feature graphic | `feature-graphic-1024x500.png` | 1024 x 500 PNG/JPEG |
| Phone screenshots (8) | `phone-screenshots/*.png` | 1920 x 1080 (16:9, under the 2:1 limit) |

Tablet screenshots are optional. If you want to add them, capture them on a 7-inch and a 10-inch tablet emulator.

**Release notes** (500 max)
```
First release: all 16 sheets of The Ultra, with tilt controls, the overheating gun and a Hall of Fame.
```

### Content rating (IARC questionnaire)
- Category: Game.
- Violence: yes, fantasy violence against non-human characters (shooting aliens). No blood, no gore, no realistic violence.
- Everything else (sexuality, language, controlled substances, gambling, user interaction, sharing location, digital purchases): no.
- Expected result: PEGI 7 / ESRB Everyone. The questionnaire makes the final decision.

### Data safety
- Does the app collect or share any of the required user data types? **No.**
- Is all user data encrypted in transit? Not applicable (no data leaves the device).
- Can users request that data be deleted? Not applicable. High scores and settings are stored only on the device and are removed when the app is uninstalled.
- The app declares no network permissions.

### Target audience and content
- Target age groups: 13 and over is the simplest choice. If you include children under 13, Google's Families policy applies.
- Ads: none.

---

## App Store (App Store Connect)

**Name** (30 max)
```
The Ultra
```

**Subtitle** (30 max)
```
Classic Oric arcade shooter
```

**Promotional text** (170 max, can be changed without a new build)
```
Sixteen sheets of aliens from the 1983 Oric classic. Tilt to steer, tap to fire, and don't let your gun overheat!
```

**Description** (4000 max)
```
Blast your way through the sixteen alien sheets of The Ultra, a faithful remake of the 1983 Oric arcade shooter.

Every sheet has its own aliens and its own attack. Rows of marchers scroll across the sky, a diamond formation sways towards you, conga lines of saucers snake across the screen, and arrowheads, mushrooms, birds and frogs sweep down in loops and V shapes. Each alien animates and moves just like the Oric original, one chunky character cell at a time.

TILT TO STEER, TAP TO FIRE
Tilt your iPhone or iPad to move the ship and tap anywhere to fire. Hold your finger down for rapid fire, and adjust the tilt sensitivity in Options to suit you.

WATCH THE HEAT
Your machine gun overheats. Fire too long and it locks up until it cools, and the heat carries on into the next sheet, so keep an eye on the gauge.

CLASSIC SCORING
Aliens are worth 10 points on the first sheet, 20 on the second and so on, up to 80. You get bonus lives at 2,000 and 10,000 points, and a 5,000 point bonus for clearing all sixteen sheets.

HALL OF FAME
Get into the top ten and enter your name. Your scores stay on your device between games.

FEATURES
• 16 sheets of aliens with the original movement patterns
• Authentic Oric look: eight colours, chunky pixels and square-wave sound effects
• Tilt steering with adjustable sensitivity and an invert option
• Top-ten Hall of Fame with name entry
• Landscape play on iPhone and iPad
• No ads, no tracking, no internet connection needed

Written by PFJ. Based on the PSS Oric game.
```

**Keywords** (100 max, comma separated, no spaces needed)
```
retro,arcade,shooter,oric,8-bit,aliens,space,classic,invaders,shoot em up,tilt,pixel,1983
```

**What's New** (for later updates)
```
First release.
```

**Screenshots** (in `store/app-store/`)
| Display | Folder | Size |
|---|---|---|
| iPhone 6.9" (required) | `iphone-6.9in/` | 2868 x 1320 landscape |
| iPad 13" (required because the app supports iPad) | `ipad-13in/` | 2752 x 2064 landscape |

App Store Connect scales these down for the smaller displays automatically.

**App icon:** `store/app-store/icon-1024.png` (1024 x 1024, no transparency). The build's asset catalogue already includes it.

### Age rating questionnaire
- Cartoon or fantasy violence: **Infrequent/Mild**. (Choose Frequent/Intense instead if you judge the constant shooting that way.)
- Every other category: None.
- Unrestricted web access, gambling, contests, user-generated content: No.
- Expected rating: 4+ or 9+, depending on the violence answer.

### App Privacy
- Data collection: **Data Not Collected**. Nothing is collected, tracked or sent off the device.

### Other answers
- Export compliance: the app uses no encryption. `ITSAppUsesNonExemptEncryption` is set to `false` in Info.plist, so App Store Connect won't ask.
- Sign-in required: No (no demo account needed for review).
- Review notes: "Tilt the device to steer and tap anywhere to fire. Pause is in the top-left corner. Options (tilt sensitivity) are on the title screen and the pause menu."

---

## Mac App Store (App Store Connect)

The Mac version is a separate build of the same app (bundle ID `uk.co.allthejohnsons.theultra`). In App Store Connect, add the **macOS** platform to the existing app, then fill in the macOS version page. Name, subtitle, keywords, privacy answers and age rating are as for iOS above.

**Promotional text** (170 max)
```
Sixteen sheets of aliens from the 1983 Oric classic. Steer with the mouse or Z and X, click to fire, and don't let your gun overheat!
```

**Description** (4000 max)
```
Blast your way through the sixteen alien sheets of The Ultra, a faithful remake of the 1983 Oric arcade shooter.

Every sheet has its own aliens and its own attack. Rows of marchers scroll across the sky, a diamond formation sways towards you, conga lines of saucers snake across the screen, and arrowheads, mushrooms, birds and frogs sweep down in loops and V shapes. Each alien animates and moves just like the Oric original, one chunky character cell at a time.

MOUSE OR KEYBOARD
Move the mouse and your ship follows it, or steer with Z (left) and X (right). Click or press Space to fire, and hold it down for rapid fire. Play in a window or full screen.

WATCH THE HEAT
Your machine gun overheats. Fire too long and it locks up until it cools, and the heat carries on into the next sheet, so keep an eye on the gauge.

CLASSIC SCORING
Aliens are worth 10 points on the first sheet, 20 on the second and so on, up to 80. You get bonus lives at 2,000 and 10,000 points, and a 5,000 point bonus for clearing all sixteen sheets.

HALL OF FAME
Get into the top ten and enter your name. Your scores are kept between games.

FEATURES
• 16 sheets of aliens with the original movement patterns
• Authentic Oric look: eight colours, chunky pixels and square-wave sound effects
• Mouse or keyboard controls (Z, X and Space), plus game pad support
• Resizable window or full screen
• Top-ten Hall of Fame with name entry
• No ads, no tracking, no internet connection needed

Written by PFJ. Based on the PSS Oric game.
```

**Screenshots:** `store/mac-app-store/screenshots-2880x1800/` (2880 x 1800, 16:10). Upload the first eight; `09-how-to-play.png` shows the controls.

**App icon:** in the build (`Ultra.Desktop/macOS/AppIcon.icns`); `store/mac-app-store/icon-1024.png` is the same artwork.

**Review notes**
```
Move the mouse to steer (or Z and X), click or press Space to fire. P or Esc pauses. Options (O on the title screen) has mouse steering and full screen.
```

**Requirements:** macOS 12 or later, on Apple silicon or Intel Macs (universal app). The app is sandboxed and asks for no other permissions.

---

## Microsoft Store (Partner Center)

Package name `49556nodoid.TheUltra`, publisher `CN=A6EAEB04-6634-41C1-BDFE-695819ECE444` (as reserved in Partner Center, in `Ultra.Desktop/Windows/AppxManifest.xml`).

| Field | Value |
|---|---|
| Product name | The Ultra |
| Category | Games, then Action & adventure (secondary: Classics) |
| Packages | Upload both `TheUltra-<version>-x64.msix` and `TheUltra-<version>-arm64.msix` to the same submission. They're unsigned; the Store signs them. |
| Device family | Windows 10/11 Desktop |
| Age ratings | Same answers as the IARC questionnaire above |
| Privacy policy | Same URL as the other stores |
| System requirements | Keyboard and mouse (game pad optional). Any graphics card with OpenGL 3.0. |

**Short description** (1000 max)
```
Sixteen sheets of aliens from the 1983 Oric classic. Steer with the mouse or Z and X, click to fire, and don't let your gun overheat!
```

**Description** (10000 max)
```
Blast your way through the sixteen alien sheets of The Ultra, a faithful remake of the 1983 Oric arcade shooter.

Every sheet has its own aliens and its own attack. Rows of marchers scroll across the sky, a diamond formation sways towards you, conga lines of saucers snake across the screen, and arrowheads, mushrooms, birds and frogs sweep down in loops and V shapes. Each alien animates and moves just like the Oric original, one chunky character cell at a time.

MOUSE OR KEYBOARD
Move the mouse and your ship follows it, or steer with Z (left) and X (right). Click or press Space to fire, and hold it down for rapid fire. Play in a window or full screen.

WATCH THE HEAT
Your machine gun overheats. Fire too long and it locks up until it cools, and the heat carries on into the next sheet, so keep an eye on the gauge.

CLASSIC SCORING
Aliens are worth 10 points on the first sheet, 20 on the second and so on, up to 80. You get bonus lives at 2,000 and 10,000 points, and a 5,000 point bonus for clearing all sixteen sheets.

HALL OF FAME
Get into the top ten and enter your name. Your scores are kept between games.

FEATURES
- 16 sheets of aliens with the original movement patterns
- Authentic Oric look: eight colours, chunky pixels and square-wave sound effects
- Mouse or keyboard controls (Z, X and Space), plus game pad support
- Resizable window or full screen
- Top-ten Hall of Fame with name entry
- No ads, no tracking, no internet connection needed

Written by PFJ. Based on the PSS Oric game.
```

**Search terms** (7 terms, 30 characters each): retro, arcade, shooter, oric, aliens, space invaders, 8-bit

**Store images** (in `store/microsoft-store/`)
| Asset | File | Size |
|---|---|---|
| Screenshots (up to 10) | `screenshots-3840x2160/*.png` | 3840 x 2160 |
| 2:3 Poster art | `poster-art-1440x2160.png` | 1440 x 2160 |
| 1:1 Box art | `box-art-2160x2160.png` | 2160 x 2160 |
| 1:1 App tile icon | `app-tile-icon-300x300.png` | 300 x 300 |
| 16:9 Super hero art (no title, the Store overlays it) | `super-hero-art-3840x2160.png` | 3840 x 2160 |
| 16:9 Hero art with title (for other promotional slots) | `hero-art-with-title-1920x1080.png` | 1920 x 1080 |

The tiles inside the package (Start menu, taskbar, splash screen) are in `Ultra.Desktop/Windows/Assets/`.

**Notes for certification**
```
Move the mouse to steer (or Z and X), click or press Space to fire. P or Esc pauses. Options (O on the title screen) has mouse steering and full screen.
```

---

## Building the uploads

Run:
```sh
tools/build_release.sh
```
It creates:

| File | Use |
|---|---|
| `release/android/uk.co.allthejohnsons.theultra-Signed.aab` | Upload to Google Play |
| `release/android/uk.co.allthejohnsons.theultra-Signed.apk` | Install directly for testing |
| `release/ios/Ultra.iOS.ipa` | Upload to App Store Connect (Transporter app, or `xcrun altool --upload-app`) |

For the Mac and Windows packages, run `tools/build_desktop.sh` (or `tools/build_desktop.sh mac` / `windows`):

| File | Use |
|---|---|
| `release/macos/TheUltra.pkg` | Upload to App Store Connect with Transporter |
| `release/macos/The Ultra.app` | The signed app inside the .pkg, for checking (it won't run outside the Store: it's signed for distribution) |
| `release/windows/TheUltra-<version>-x64.msix`, `-arm64.msix` | Upload both to Partner Center |
| `release/windows/TheUltra-<version>-x64.zip`, `-arm64.zip` | The game unpackaged: unzip on a PC and run `TheUltra.exe` to test |

**Mac signing:** the app is signed with "Apple Distribution: Paul Johnson (3UH7BE38T3)" and a Mac App Store profile of type **macOS** (not Mac Catalyst) for `uk.co.allthejohnsons.theultra` (read from `~/Downloads/relultramac-2.provisionprofile`, or set `ULTRA_MAC_PROFILE`; the script stops if given a Catalyst profile), and the .pkg with "3rd Party Mac Developer Installer: Paul Johnson (3UH7BE38T3)". The app is universal: `Contents/MonoBundle` holds a .NET runtime for each architecture (`arm64`, `x64`) and the universal launcher in `Contents/MacOS` starts the one that matches the Mac.

**Windows packaging:** `tools/make_msix.py` builds the .msix on the Mac. The packages are unsigned, which is what the Store expects.

**Screenshots:** `tools/capture_screenshots.sh` regenerates the Mac and Microsoft Store screenshots from the game itself.

**Android signing:** the upload key is `~/keys/ultra-upload.jks` (alias `ultra`), and its password is in your login Keychain under "The Ultra Android upload keystore". Back up both. You can't publish updates without the key unless you ask Google for an upload-key reset. Use Play App Signing when you create the app in Play Console.

**iOS signing:** "Apple Distribution: Paul Johnson (3UH7BE38T3)" with the App Store profile `rel-ultra` (bundle ID `uk.co.allthejohnsons.theultra`).

Raise `ApplicationVersion` (the build number) in both `.csproj` files for every upload, and `ApplicationDisplayVersion` for each new release. For the desktop builds, raise `BuildNumber` in `Ultra.Desktop/Ultra.Desktop.csproj` for every Mac upload, and `Version` for each new release (the Microsoft Store needs a higher version for every submission).
