#!/usr/bin/env bash
# Produce self-contained, single-file packages that run after unzipping (no .NET, no installer, no admin).
# Usage: scripts/publish.sh [platform ...]     platforms: win-x64 osx-arm64 osx-x64 linux-x64 (default: all)
# Output: artifacts/MothEidol-<version>-<platform>.zip
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SUPPORTED=(win-x64 osx-arm64 osx-x64 linux-x64)
DIST="$ROOT/scripts/dist"
OUT="$ROOT/artifacts"

command -v dotnet >/dev/null 2>&1 || { echo "dotnet SDK not found. Install it from https://dotnet.microsoft.com/download" >&2; exit 1; }

PLATFORMS=("$@")
[ ${#PLATFORMS[@]} -eq 0 ] && PLATFORMS=("${SUPPORTED[@]}")
for p in "${PLATFORMS[@]}"; do
  case " ${SUPPORTED[*]} " in *" $p "*) ;; *) echo "Unsupported platform '$p'. Supported: ${SUPPORTED[*]}" >&2; exit 1 ;; esac
done

VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props" | head -1)"
[ -n "$VERSION" ] || { echo "Could not read the version from Directory.Build.props" >&2; exit 1; }

fill() { # fill <template> <arch> > output
  sed -e "s/@VERSION@/$VERSION/g" -e "s/@ARCH@/$2/g" "$1"
}

make_icns() { # make_icns <png> <icns>
  command -v iconutil >/dev/null 2>&1 && command -v sips >/dev/null 2>&1 || return 1
  local set; set="$(mktemp -d)/MothEidol.iconset"
  mkdir -p "$set"
  for s in 16 32 128 256 512; do
    sips -z $s $s "$1" --out "$set/icon_${s}x${s}.png" >/dev/null
    sips -z $((s * 2)) $((s * 2)) "$1" --out "$set/icon_${s}x${s}@2x.png" >/dev/null
  done
  iconutil -c icns "$set" -o "$2"
}

mkdir -p "$OUT"
for rid in "${PLATFORMS[@]}"; do
  echo "==> $rid"
  PUB="$OUT/publish/$rid"
  rm -rf "$PUB"
  dotnet publish "$ROOT/src/MothEidol.App" -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -p:DebugType=none -p:DebugSymbols=false \
    -o "$PUB" --nologo -v quiet

  NAME="MothEidol-$VERSION-$rid"
  STAGE="$OUT/stage/$NAME"
  rm -rf "$STAGE"; mkdir -p "$STAGE"

  case "$rid" in
    win-x64)
      mv "$PUB/MothEidol.App.exe" "$STAGE/MothEidol.exe"
      fill "$DIST/README.win.txt" x64 > "$STAGE/README.txt" ;;
    linux-x64)
      mv "$PUB/MothEidol.App" "$STAGE/MothEidol"; chmod +x "$STAGE/MothEidol"
      fill "$DIST/README.linux.txt" x64 > "$STAGE/README.txt" ;;
    osx-*)
      APP="$STAGE/MothEidol.app"
      mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
      mv "$PUB/MothEidol.App" "$APP/Contents/MacOS/MothEidol"; chmod +x "$APP/Contents/MacOS/MothEidol"
      fill "$DIST/Info.plist.template" "" > "$APP/Contents/Info.plist"
      make_icns "$ROOT/src/MothEidol.App/Assets/app-icon.png" "$APP/Contents/Resources/MothEidol.icns" \
        || echo "   (no iconutil/sips here: the bundle has no icon)"
      if command -v codesign >/dev/null 2>&1; then
        codesign --force --deep -s - "$APP" >/dev/null 2>&1 || echo "   (ad-hoc signing failed)"
      else
        echo "   (no codesign here: the bundle is unsigned and may not start on Apple Silicon)"
      fi
      fill "$DIST/README.mac.txt" "${rid#osx-}" > "$STAGE/README.txt" ;;
  esac

  rm -f "$OUT/$NAME.zip"
  (cd "$OUT/stage" && zip -qry "$OUT/$NAME.zip" "$NAME")
  echo "   $OUT/$NAME.zip ($(du -h "$OUT/$NAME.zip" | cut -f1))"
done
rm -rf "$OUT/stage"
