Remove-Item -Recurse -Force .\x64\Release\** -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\x86\Release\** -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\TqkLibrary.AudioCapture\bin\Release\** -ErrorAction SilentlyContinue

$env:PATH="$($env:PATH);C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE;C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE"
devenv .\TqkLibrary.AudioCapture.sln /Rebuild 'Release|x64' /Project TqkLibrary.AudioCapture.Native
devenv .\TqkLibrary.AudioCapture.sln /Rebuild 'Release|x86' /Project TqkLibrary.AudioCapture.Native

dotnet build --no-incremental .\TqkLibrary.AudioCapture\TqkLibrary.AudioCapture.csproj -c Release
nuget pack .\TqkLibrary.AudioCapture\TqkLibrary.AudioCapture.nuspec -Symbols -OutputDirectory .\TqkLibrary.AudioCapture\bin\Release

$localNuget = $env:localNuget
if(![string]::IsNullOrWhiteSpace($localNuget))
{
    Copy-Item .\TqkLibrary.AudioCapture\bin\Release\*.nupkg -Destination $localNuget -Force
}

$nugetKey = $env:nugetKey
if(![string]::IsNullOrWhiteSpace($nugetKey))
{
    Write-Host "Enter to push nuget"
    pause
    Write-Host "enter to confirm"
    pause
    $files = [System.IO.Directory]::GetFiles(".\TqkLibrary.AudioCapture\bin\Release\")
    iex "nuget push $($files[0]) -ApiKey $($nugetKey) -Source https://api.nuget.org/v3/index.json"
}