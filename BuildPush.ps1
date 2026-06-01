$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
Set-Location $repo

# ---------------------------------------------------------------------------
# 1) Locate MSBuild via vswhere (works on any machine / VS edition, no devenv)
# ---------------------------------------------------------------------------
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) { throw "vswhere not found at $vswhere. Install Visual Studio / Build Tools." }
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($msbuild) -or -not (Test-Path $msbuild)) { throw "MSBuild not found via vswhere. Install the C++ workload." }
Write-Host "MSBuild: $msbuild"

# ---------------------------------------------------------------------------
# 2) Compute version from GitVersion (Major.Minor.<commits since tag>)
# ---------------------------------------------------------------------------
if (-not (Get-Command dotnet-gitversion -ErrorAction SilentlyContinue)) {
    Write-Host "Installing GitVersion.Tool ..."
    dotnet tool install -g GitVersion.Tool | Out-Host
    $env:PATH = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
}
$gv = dotnet-gitversion /output json | ConvertFrom-Json
$verMajor = [int]$gv.Major
$verMinor = [int]$gv.Minor
$verBuild = [int]$gv.CommitsSinceVersionSource
$packageVersion = "$verMajor.$verMinor.$verBuild"
Write-Host "Version: $packageVersion (Assembly $verMajor.$verMinor.0.0, File $packageVersion.0)"

# ---------------------------------------------------------------------------
# 3) Write the native version header (consumed by version.rc)
# ---------------------------------------------------------------------------
$header = @"
#pragma once
// This file is overwritten by BuildPush.ps1 from GitVersion before a release build.
// The values below are a fallback so the project still compiles in the IDE / dev builds.
#define VER_FILE_MAJOR    $verMajor
#define VER_FILE_MINOR    $verMinor
#define VER_FILE_BUILD    $verBuild
#define VER_FILE_REVISION 0
#define VER_FILEVERSION_STR    "$verMajor.$verMinor.$verBuild.0"
#define VER_PRODUCTVERSION_STR "$verMajor.$verMinor.$verBuild"
"@
$headerPath = Join-Path $repo 'TqkLibrary.AudioCapture.Native\version.generated.h'
# Trailing newline is required or rc.exe reports RC1004 (unexpected end of file).
[System.IO.File]::WriteAllText($headerPath, ($header + "`r`n"), (New-Object System.Text.UTF8Encoding($false)))

# ---------------------------------------------------------------------------
# 4) Clean previous outputs
# ---------------------------------------------------------------------------
Remove-Item -Recurse -Force .\x64\Release\** -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\x86\Release\** -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\TqkLibrary.AudioCapture\bin\Release\** -ErrorAction SilentlyContinue

# ---------------------------------------------------------------------------
# 5) Build the native C++ DLL (x64 + Win32) with MSBuild.
#    SolutionDir is passed so OutDir ($(SolutionDir)x64|x86\Release\) resolves.
# ---------------------------------------------------------------------------
$nativeProj = Join-Path $repo 'TqkLibrary.AudioCapture.Native\TqkLibrary.AudioCapture.Native.vcxproj'
foreach ($platform in @('x64','Win32')) {
    Write-Host "Building native $platform ..."
    & $msbuild $nativeProj /t:Rebuild /p:Configuration=Release /p:Platform=$platform /p:SolutionDir="$repo\" /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw "Native build failed ($platform)" }
}

# ---------------------------------------------------------------------------
# 6) Pack the managed package (version comes from GitVersion via the targets;
#    the nuspec pulls in the native DLLs from x64\Release and x86\Release).
# ---------------------------------------------------------------------------
dotnet pack .\TqkLibrary.AudioCapture\TqkLibrary.AudioCapture.csproj -c Release -o .\TqkLibrary.AudioCapture\bin\Release
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed" }

$nupkg = Get-ChildItem .\TqkLibrary.AudioCapture\bin\Release\*.nupkg | Select-Object -First 1
Write-Host "Packed: $($nupkg.Name)"

# ---------------------------------------------------------------------------
# 7) Optional: copy to local feed / push to nuget.org
# ---------------------------------------------------------------------------
$localNuget = $env:localNuget
if (![string]::IsNullOrWhiteSpace($localNuget)) {
    Copy-Item $nupkg.FullName -Destination $localNuget -Force
    Write-Host "Copied to local feed: $localNuget"
}

$nugetKey = $env:nugetKey
if (![string]::IsNullOrWhiteSpace($nugetKey)) {
    Write-Host "Enter to push nuget"
    pause
    Write-Host "enter to confirm"
    pause
    nuget push $nupkg.FullName -ApiKey $nugetKey -Source https://api.nuget.org/v3/index.json
}
