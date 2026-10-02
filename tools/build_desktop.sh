#!/bin/zsh
# Builds the desktop store packages into release/:
#   release/macos/The Ultra.app   (signed for the Mac App Store, for checking before upload)
#   release/macos/TheUltra.pkg    (upload to App Store Connect with Transporter)
#   release/windows/TheUltra-<version>-<x64|arm64>.msix  (upload both to Partner Center; the Store signs them)
#   release/windows/TheUltra-<version>-<x64|arm64>.zip   (the same game unpackaged, for testing on a PC)
#
# Mac signing uses "Apple Distribution: Paul Johnson (3UH7BE38T3)", the rel-ultra-mac profile
# (~/Downloads/relultramac.provisionprofile, or set ULTRA_MAC_PROFILE) and
# "3rd Party Mac Developer Installer: Paul Johnson (3UH7BE38T3)" for the .pkg.
#
#   tools/build_desktop.sh          both platforms
#   tools/build_desktop.sh mac      macOS only
#   tools/build_desktop.sh windows  Windows only
set -euo pipefail
cd "$(dirname "$0")/.."

PROJECT=Ultra.Desktop/Ultra.Desktop.csproj
VERSION=$(dotnet msbuild $PROJECT -getProperty:Version)
BUILD=$(dotnet msbuild $PROJECT -getProperty:BuildNumber)
WHAT=${1:-all}

APP_SIGN="Apple Distribution: Paul Johnson (3UH7BE38T3)"
PKG_SIGN="3rd Party Mac Developer Installer: Paul Johnson (3UH7BE38T3)"
PROFILE=${ULTRA_MAC_PROFILE:-$HOME/Downloads/relultramac.provisionprofile}

build_mac() {
  local out=release/macos
  local app="$out/The Ultra.app"
  local tmp=$(mktemp -d)
  rm -rf $out && mkdir -p $out

  # Apple silicon only (the Mac App Store accepts that for macOS 12 and later).
  dotnet publish $PROJECT -c Release -r osx-arm64 --self-contained -o $tmp/publish \
    -p:DebugType=none -p:GenerateDocumentationFile=false

  mkdir -p "$app/Contents/MacOS" "$app/Contents/MonoBundle" "$app/Contents/Resources"
  cp -R $tmp/publish/. "$app/Contents/MonoBundle/"
  # The runtime is started by the launcher below; these aren't needed in the app.
  rm -f "$app/Contents/MonoBundle/TheUltra" "$app/Contents/MonoBundle/createdump"

  local sdk=$(xcrun --sdk macosx --show-sdk-version)
  clang -O2 -arch arm64 -mmacosx-version-min=12.0 -isysroot "$(xcrun --sdk macosx --show-sdk-path)" \
    -framework AppKit -o "$app/Contents/MacOS/TheUltra" Ultra.Desktop/macOS/launcher.c
  cp Ultra.Desktop/macOS/AppIcon.icns "$app/Contents/Resources/"
  sed -e "s/\$(VERSION)/$VERSION/" -e "s/\$(BUILD)/$BUILD/" Ultra.Desktop/macOS/Info.plist > "$app/Contents/Info.plist"
  # Build environment keys that Xcode would add.
  local plist="$app/Contents/Info.plist"
  plutil -insert DTPlatformName -string macosx "$plist"
  plutil -insert DTPlatformVersion -string "$sdk" "$plist"
  plutil -insert DTSDKName -string "macosx$sdk" "$plist"
  plutil -insert DTSDKBuild -string "$(xcrun --sdk macosx --show-sdk-build-version)" "$plist"
  plutil -insert DTXcode -string "$(xcodebuild -version | awk '/Xcode/ {split($2, v, "."); printf "%02d%d%d", v[1], v[2], v[3]}')" "$plist"
  plutil -insert DTXcodeBuild -string "$(xcodebuild -version | awk '/Build version/ {print $3}')" "$plist"
  plutil -insert DTCompiler -string com.apple.compilers.llvm.clang.1_0 "$plist"
  plutil -insert BuildMachineOSBuild -string "$(sw_vers -buildVersion)" "$plist"
  printf 'APPL????' > "$app/Contents/PkgInfo"

  # Entitlements take the app and team identifiers from the provisioning profile.
  security cms -D -i "$PROFILE" > $tmp/profile.plist
  local app_id=$(plutil -extract Entitlements.application-identifier raw $tmp/profile.plist)
  local team_id=$(plutil -extract Entitlements.com\\.apple\\.developer\\.team-identifier raw $tmp/profile.plist)
  sed -e "s/\$(APP_ID)/$app_id/" -e "s/\$(TEAM_ID)/$team_id/" Ultra.Desktop/macOS/Entitlements.plist > $tmp/entitlements.plist
  cp "$PROFILE" "$app/Contents/embedded.provisionprofile"

  xattr -cr "$app"
  find "$app/Contents/MonoBundle" -name '*.dylib' -print0 | xargs -0 codesign --force --sign "$APP_SIGN"
  codesign --force --sign "$APP_SIGN" --entitlements $tmp/entitlements.plist "$app"
  codesign --verify --strict --verbose=2 "$app"

  productbuild --component "$app" /Applications --sign "$PKG_SIGN" "$out/TheUltra.pkg"
  rm -rf $tmp
  echo "macOS: $out/TheUltra.pkg (version $VERSION, build $BUILD, $app_id)"
}

build_windows() {
  local out=release/windows
  rm -rf $out && mkdir -p $out
  # Intel/AMD and Arm PCs. Upload both .msix files to the same Partner Center submission.
  for arch in x64 arm64; do
    local tmp=$(mktemp -d)
    local pkg=$tmp/package
    dotnet publish $PROJECT -c Release -r win-$arch --self-contained -o $pkg \
      -p:DebugType=none -p:GenerateDocumentationFile=false
    rm -f $pkg/createdump.exe
    (cd $pkg && zip -qr "$OLDPWD/$out/TheUltra-$VERSION-$arch.zip" .)

    mkdir -p $pkg/Assets
    cp Ultra.Desktop/Windows/Assets/*.png $pkg/Assets/
    sed -e "s/\$(VERSION)/$VERSION.0/" -e "s/\$(ARCH)/$arch/" Ultra.Desktop/Windows/AppxManifest.xml > $pkg/AppxManifest.xml
    python3 tools/make_msix.py $pkg "$out/TheUltra-$VERSION-$arch.msix"
    rm -rf $tmp
  done
  echo "Windows: $out/TheUltra-$VERSION-{x64,arm64}.msix (version $VERSION.0)"
}

[[ $WHAT == all || $WHAT == mac ]] && build_mac
[[ $WHAT == all || $WHAT == windows ]] && build_windows
ls -la release/macos release/windows 2>/dev/null || true
