# Store listing: The Ultra

Everything you need to paste into Google Play Console and App Store Connect. The character counts are within each store's limits (checked by `tools/check_listing.py`).

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
| Support email | *(your support email)* |
| Privacy policy URL | *(host `store/PRIVACY.md` publicly and paste the URL here)* |
| Website (optional) | *(your site)* |
| Supported devices | Android phones and tablets, iPhone, iPad |
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

## Building the uploads

**Google Play (.aab):** you need an upload keystore. Create one once and keep it safe:
```sh
keytool -genkeypair -v -keystore ultra-upload.keystore -alias ultra -keyalg RSA -keysize 2048 -validity 10000
dotnet publish Ultra.Android/Ultra.Android.csproj -c Release -f net10.0-android \
  -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=ultra-upload.keystore -p:AndroidSigningKeyAlias=ultra \
  -p:AndroidSigningKeyPass=env:ULTRA_KEY_PASS -p:AndroidSigningStorePass=env:ULTRA_STORE_PASS
```
Raise `ApplicationVersion` in `Ultra.Android/Ultra.Android.csproj` for every upload.

**App Store (.ipa):** set your team's distribution signing identity and provisioning profile, then:
```sh
dotnet publish Ultra.iOS/Ultra.iOS.csproj -c Release -f net10.0-ios -p:ArchiveOnBuild=true \
  -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Distribution: …" -p:CodesignProvision="…"
```
Upload the .ipa with Transporter or `xcrun altool`. Raise `ApplicationVersion` in `Ultra.iOS/Ultra.iOS.csproj` for every upload.
