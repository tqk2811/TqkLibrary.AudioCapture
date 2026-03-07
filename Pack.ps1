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

Write-Host "Building Win32..."
& msbuild $vcxproj /p:Configuration=$Configuration /p:Platform=Win32 /t:Build /v:minimal /p:OutDir="Release\"
if ($LASTEXITCODE -ne 0) { throw "C++ Win32 build failed" }

Write-Host "Building x64..."
& msbuild $vcxproj /p:Configuration=$Configuration /p:Platform=x64 /t:Build /v:minimal /p:OutDir="x64\Release\"
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
