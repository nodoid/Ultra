#!/bin/zsh
# Captures the Mac App Store (2880x1800) and Microsoft Store (3840x2160) screenshots by running
# the desktop build in its screenshot mode, which plays each sheet by itself. Takes about a minute;
# a window opens while it runs.
set -euo pipefail
cd "$(dirname "$0")/.."

tmp=$(mktemp -d)
dotnet build Ultra.Desktop/Ultra.Desktop.csproj -c Release -v quiet
dotnet Ultra.Desktop/bin/Release/net10.0/TheUltra.dll --screenshots $tmp 2880x1800 3840x2160

# The stores want opaque images.
python3 - $tmp <<'PY'
import glob, os, shutil, sys
from PIL import Image
src = sys.argv[1]
for size, dest in (("2880x1800", "store/mac-app-store/screenshots-2880x1800"),
                   ("3840x2160", "store/microsoft-store/screenshots-3840x2160")):
    shutil.rmtree(dest, ignore_errors=True)
    os.makedirs(dest)
    for f in sorted(glob.glob(os.path.join(src, size, "*.png"))):
        Image.open(f).convert("RGB").save(os.path.join(dest, os.path.basename(f)))
        print("wrote", os.path.join(dest, os.path.basename(f)))
PY
rm -rf $tmp
