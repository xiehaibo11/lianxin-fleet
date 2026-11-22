# 在 Windows 上交叉编译 macOS 版中控，打包成 LianxinFleet-Mac.app 并压缩为 zip（保留可执行权限）。
# 产物未经 Apple 公证；在 Mac 上首次打开前需移除隔离属性，见 docs/MAC_GUIDE.md。在 Mac 上构建请用 publish-mac.sh。
param(
    [ValidateSet('osx-arm64', 'osx-x64')][string[]]$Runtime = @('osx-arm64', 'osx-x64'),
    [string]$Python = $env:PYTHON_PATH
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK first.' }
if (-not $Python) {
    $Python = @('data/fleet-python/Scripts/python.exe', 'python') | Where-Object { Get-Command $_ -ErrorAction SilentlyContinue } |
        Select-Object -First 1
}
if (-not $Python) { throw 'Python is required to write zip files with Unix permissions.' }
& dotnet msbuild src/Lianxin.Fleet.Core/Lianxin.Fleet.Core.csproj '-t:ValidateGovernanceRules,ValidateRepositoryGovernance' -nologo
if ($LASTEXITCODE -ne 0) { throw 'Code governance checks failed.' }

[xml]$project = Get-Content src/Lianxin.Fleet.Mac/Lianxin.Fleet.Mac.csproj
$version = @($project.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ })[0]
if (-not $version) { throw 'Version is missing in Lianxin.Fleet.Mac.csproj.' }
$plist = @"
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
"@
# Mach-O 文件（主程序、createdump、动态库）设为 755，其余 644；zip 标记为 Unix 创建，Mac 解压后保留权限。
$zipper = @'
import os, sys, zipfile
source, target = sys.argv[1], sys.argv[2]
magics = {b"\xcf\xfa\xed\xfe", b"\xce\xfa\xed\xfe", b"\xca\xfe\xba\xbe"}
base = os.path.dirname(source)
with zipfile.ZipFile(target, "w", zipfile.ZIP_DEFLATED) as archive:
    for folder, dirs, files in os.walk(source):
        dirs.sort()
        relative = os.path.relpath(folder, base).replace(os.sep, "/") + "/"
        entry = zipfile.ZipInfo.from_file(folder, relative)
        entry.create_system = 3
        entry.external_attr = (0o40755 << 16) | 0x10
        archive.writestr(entry, b"")
        for name in sorted(files):
            path = os.path.join(folder, name)
            with open(path, "rb") as stream:
                data = stream.read()
            entry = zipfile.ZipInfo.from_file(path, relative + name)
            entry.create_system = 3
            entry.compress_type = zipfile.ZIP_DEFLATED
            entry.external_attr = (0o100755 if data[:4] in magics else 0o100644) << 16
            archive.writestr(entry, data)
'@
$zipScript = Join-Path ([IO.Path]::GetTempPath()) "lianxin-mac-zip-$PID.py"
[IO.File]::WriteAllText($zipScript, $zipper)
try {
    foreach ($rid in $Runtime) {
        $arch = $rid.Substring(4)
        $root = Join-Path (Get-Location) "dist/mac-$arch"
        if (Test-Path $root) { Remove-Item -Recurse -Force $root }
        $app = Join-Path $root 'LianxinFleet-Mac.app'
        $macos = Join-Path $app 'Contents/MacOS'
        & dotnet publish src/Lianxin.Fleet.Mac/Lianxin.Fleet.Mac.csproj -c Release -r $rid --self-contained true `
            -p:RuntimeFrameworkVersion=10.0.15 -p:DebugType=None -p:DebugSymbols=false -o $macos
        if ($LASTEXITCODE -ne 0) { throw "Mac publish failed for $rid." }
        New-Item -ItemType Directory -Force -Path (Join-Path $app 'Contents/Resources') | Out-Null
        [IO.File]::WriteAllText((Join-Path $app 'Contents/Info.plist'), $plist, (New-Object Text.UTF8Encoding $false))
        $zip = Join-Path (Get-Location) "dist/LianxinFleet-Mac-$version-$arch.zip"
        if (Test-Path $zip) { Remove-Item -Force $zip }
        & $Python -I $zipScript $app $zip
        if ($LASTEXITCODE -ne 0) { throw "Zip failed for $rid." }
        Write-Host "Packaged $zip"
    }
}
finally { Remove-Item -Force $zipScript -ErrorAction SilentlyContinue }
