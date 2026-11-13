#!/usr/bin/env bash
# Helper build CLI untuk Warteg Simulator.
# Sesuaikan UNITY_BIN dengan lokasi Unity Editor di mesin kamu.
#
# Contoh:
#   ./Tools/build.sh windows
#   ./Tools/build.sh android
#   ./Tools/build.sh all

set -euo pipefail

UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity}"
# Linux contoh: /home/<user>/Unity/Hub/Editor/2022.3.62f1/Editor/Unity
# Windows contoh: "C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe"

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
TARGET="${1:-all}"

if [[ ! -x "$UNITY_BIN" ]]; then
  echo "Unity tidak ditemukan di: $UNITY_BIN" >&2
  echo "Atur variabel UNITY_BIN ke path Unity Editor yang benar, contoh:" >&2
  echo "  UNITY_BIN=/path/ke/Unity ./Tools/build.sh windows" >&2
  exit 1
fi

build_windows() {
  echo "==> Build Windows 64-bit..."
  "$UNITY_BIN" -batchmode -quit -projectPath "$PROJECT_DIR" \
    -executeMethod Warteg.EditorTools.BuildScript.BuildWindowsCLI
  echo "Selesai: $PROJECT_DIR/Builds/Windows/WartegSimulator.exe"
}

build_android() {
  echo "==> Build Android APK..."
  "$UNITY_BIN" -batchmode -quit -projectPath "$PROJECT_DIR" \
    -executeMethod Warteg.EditorTools.BuildScript.BuildAndroidCLI
  echo "Selesai: $PROJECT_DIR/Builds/Android/WartegSimulator.apk"
}

case "$TARGET" in
  windows) build_windows ;;
  android) build_android ;;
  all)     build_windows; build_android ;;
  *) echo "Target tidak dikenal: $TARGET (pakai: windows | android | all)" >&2; exit 1 ;;
esac
