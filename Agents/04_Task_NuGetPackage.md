# Task 4: NuGet Package & Build Script

## Mô tả
Tạo `.nuspec` và script PowerShell để đóng gói thư viện thành NuGet package, bao gồm cả managed (.NET) DLL và native C++ DLL.

## Subtasks

### 4.1 Tạo file `.nuspec`
- [ ] Tạo `TqkLibrary.AudioCapture.nuspec` tại root
- [ ] Khai báo metadata: id, version, authors, description, license
- [ ] Cấu trúc files trong package:
  ```xml
  <files>
    <!-- .NET Framework 4.6.2 -->
    <file src="TqkLibrary.AudioCapture\bin\Release\net462\TqkLibrary.AudioCapture.dll"
          target="lib\net462\" />

    <!-- .NET 8.0 Windows -->
    <file src="TqkLibrary.AudioCapture\bin\Release\net8.0-windows7.0\TqkLibrary.AudioCapture.dll"
          target="lib\net8.0-windows7.0\" />

    <!-- Native DLLs - x86 -->
    <file src="TqkLibrary.AudioCapture.Native\Release\TqkLibrary.AudioCapture.Native.dll"
          target="runtimes\win-x86\native\" />

    <!-- Native DLLs - x64 -->
    <file src="TqkLibrary.AudioCapture.Native\x64\Release\TqkLibrary.AudioCapture.Native.dll"
          target="runtimes\win-x64\native\" />
  </files>
  ```

### 4.2 Tạo MSBuild targets file
- [ ] Tạo `TqkLibrary.AudioCapture.targets` để tự động copy native DLL khi consume package
- [ ] Phân biệt x86/x64 runtime
- [ ] Đặt trong `build\` folder của NuGet package

### 4.3 Tạo script đóng gói `Pack.ps1`
- [ ] Tạo `Pack.ps1` tại root
- [ ] Các bước:
  1. Clean build directories
  2. Build C++ Native (Release|Win32 + Release|x64) bằng MSBuild
  3. Build C# Library (Release) bằng `dotnet build`
  4. Gọi `nuget.exe pack` với `.nuspec`
- [ ] Tham số:
  - `-Version`: version number (default từ `.nuspec`)
  - `-OutputDir`: thư mục output (default `./nupkg`)

### 4.4 Cấu trúc NuGet Package

```
TqkLibrary.AudioCapture.{version}.nupkg
├── lib/
│   ├── net462/
│   │   └── TqkLibrary.AudioCapture.dll
│   └── net8.0-windows7.0/
│       └── TqkLibrary.AudioCapture.dll
├── runtimes/
│   ├── win-x86/
│   │   └── native/
│   │       └── TqkLibrary.AudioCapture.Native.dll
│   └── win-x64/
│       └── native/
│           └── TqkLibrary.AudioCapture.Native.dll
├── build/
│   └── TqkLibrary.AudioCapture.targets
└── TqkLibrary.AudioCapture.nuspec
```

## Script Pack.ps1 (Draft)

```powershell
param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = "./nupkg",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$SolutionRoot = $PSScriptRoot

Write-Host "=== TqkLibrary.AudioCapture NuGet Pack ===" -ForegroundColor Cyan

# 1. Clean
Write-Host "`n[1/4] Cleaning..." -ForegroundColor Yellow
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# 2. Build C++ Native (Win32 + x64)
Write-Host "`n[2/4] Building C++ Native..." -ForegroundColor Yellow
$vcxproj = "$SolutionRoot\TqkLibrary.AudioCapture.Native\TqkLibrary.AudioCapture.Native.vcxproj"

& msbuild $vcxproj /p:Configuration=$Configuration /p:Platform=Win32 /t:Build /v:minimal
if ($LASTEXITCODE -ne 0) { throw "C++ Win32 build failed" }

& msbuild $vcxproj /p:Configuration=$Configuration /p:Platform=x64 /t:Build /v:minimal
if ($LASTEXITCODE -ne 0) { throw "C++ x64 build failed" }

# 3. Build C# Library
Write-Host "`n[3/4] Building C# Library..." -ForegroundColor Yellow
& dotnet build "$SolutionRoot\TqkLibrary.AudioCapture\TqkLibrary.AudioCapture.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "C# build failed" }

# 4. Pack NuGet
Write-Host "`n[4/4] Packing NuGet..." -ForegroundColor Yellow
& nuget pack "$SolutionRoot\TqkLibrary.AudioCapture.nuspec" -Version $Version -OutputDirectory $OutputDir -NoPackageAnalysis
if ($LASTEXITCODE -ne 0) { throw "NuGet pack failed" }

Write-Host "`n=== Done! Package created in $OutputDir ===" -ForegroundColor Green
```
