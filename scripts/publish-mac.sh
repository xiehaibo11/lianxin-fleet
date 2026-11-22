#!/bin/sh
# 在 Mac 上构建 macOS 版中控：打包 LianxinFleet-Mac.app，做本机临时签名（ad-hoc），再用 ditto 压缩。
# 用法：scripts/publish-mac.sh [osx-arm64|osx-x64 ...]，默认两种都打包。产物在 dist/ 下。
set -eu
cd "$(dirname "$0")/.."
command -v dotnet >/dev/null || { echo "Install the .NET 10 SDK first." >&2; exit 1; }
dotnet msbuild src/Lianxin.Fleet.Core/Lianxin.Fleet.Core.csproj -t:ValidateGovernanceRules,ValidateRepositoryGovernance -nologo
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/Lianxin.Fleet.Mac/Lianxin.Fleet.Mac.csproj | head -n 1)
[ -n "$version" ] || { echo "Version is missing in Lianxin.Fleet.Mac.csproj." >&2; exit 1; }
[ $# -gt 0 ] || set -- osx-arm64 osx-x64
for rid in "$@"; do
    case "$rid" in osx-arm64|osx-x64) ;; *) echo "Unsupported runtime: $rid" >&2; exit 1 ;; esac
    arch=${rid#osx-}
    root="dist/mac-$arch"
    app="$root/LianxinFleet-Mac.app"
    rm -rf "$root"
    dotnet publish src/Lianxin.Fleet.Mac/Lianxin.Fleet.Mac.csproj -c Release -r "$rid" --self-contained true \
        -p:RuntimeFrameworkVersion=10.0.15 -p:DebugType=None -p:DebugSymbols=false -o "$app/Contents/MacOS"
    mkdir -p "$app/Contents/Resources"
    cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>联信 Fleet</string>
    <key>CFBundleDisplayName</key><string>联信 Fleet</string>
    <key>CFBundleIdentifier</key><string>com.lianxin.fleet.mac</string>
    <key>CFBundleExecutable</key><string>Lianxin.Fleet.Mac</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>$version</string>
    <key>CFBundleVersion</key><string>$version</string>
    <key>LSMinimumSystemVersion</key><string>14.0</string>
    <key>LSApplicationCategoryType</key><string>public.app-category.developer-tools</string>
    <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST
    # 没有 Developer ID 证书时做临时签名，本机可直接运行；分发给别人仍需对方移除隔离属性。
    codesign --force --deep --sign - "$app"
    zip="dist/LianxinFleet-Mac-$version-$arch.zip"
    rm -f "$zip"
    ditto -c -k --sequesterRsrc --keepParent "$app" "$zip"
    echo "Packaged $zip"
done
