#!/bin/zsh
# Builds the store packages into release/:
#   release/android/*.aab  (upload to Google Play)
#   release/android/*.apk  (for side-loading / testing)
#   release/ios/*.ipa      (upload to App Store Connect)
# Android signing uses ~/keys/ultra-upload.jks; its password is read from the macOS Keychain.
# iOS signing uses "Apple Distribution: Paul Johnson (3UH7BE38T3)" and the rel-ultra profile.
set -euo pipefail
cd "$(dirname "$0")/.."

export ULTRA_KEYSTORE_PASS="$(security find-generic-password -a ultra-upload -s 'The Ultra Android upload keystore' -w)"
SDK="${ANDROID_HOME:-$HOME/Library/Android/sdk}"

rm -rf release/android release/ios
for format in aab apk; do
  dotnet publish Ultra.Android/Ultra.Android.csproj -c Release -f net10.0-android \
    -p:AndroidPackageFormat=$format -p:AndroidSdkDirectory="$SDK" -o release/android/$format
done
mkdir -p release/android/out
mv release/android/aab/*-Signed.aab release/android/apk/*-Signed.apk release/android/out/
rm -rf release/android/aab release/android/apk
mv release/android/out/* release/android/ && rmdir release/android/out
dotnet publish Ultra.iOS/Ultra.iOS.csproj -c Release -f net10.0-ios -o release/ios

ls -la release/android release/ios/*.ipa
