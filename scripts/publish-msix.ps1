<#
.SYNOPSIS
  Builds the Microsoft Store MSIX package (win-x64) on a Windows host with the Windows SDK.
.DESCRIPTION
  With packaging/store-identity.local.json (or -IdentityFile) the package carries the Partner Center
  identity and is left UNSIGNED: the Store signs it on submission. Without it, a test package is built
  and signed with a local self-signed certificate; it can be sideloaded but NOT submitted to the Store.
.EXAMPLE
  powershell -File scripts\publish-msix.ps1
#>
param(
    [string]$IdentityFile = (Join-Path $PSScriptRoot '..\packaging\store-identity.local.json')
)
$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$out = Join-Path $root 'artifacts'

function Find-SdkTool([string]$name) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $kits = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $found = Get-ChildItem $kits -Recurse -Filter $name -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\' } | Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $found) { throw "$name not found. Install the Windows SDK (https://developer.microsoft.com/windows/downloads/windows-sdk/)." }
    return $found.FullName
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet SDK not found.' }
$makeappx = Find-SdkTool 'makeappx.exe'

# Version: x.y.z from Directory.Build.props -> x.y.z.0 (the Store reserves the fourth part).
$propsXml = [xml](Get-Content (Join-Path $root 'Directory.Build.props'))
$version = ($propsXml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Unexpected version '$version' in Directory.Build.props." }
$msixVersion = "$version.0"

# Identity: Partner Center values, or a test identity.
$isTest = -not (Test-Path $IdentityFile)
if ($isTest) {
    $identity = [pscustomobject]@{ identityName = 'MothEidol.Test'; publisher = 'CN=MothEidolTest'; publisherDisplayName = 'Moth-Eidol (test)' }
    Write-Warning 'No store identity file found: building a TEST package. It cannot be submitted to the Store.'
} else {
    $identity = Get-Content $IdentityFile -Raw | ConvertFrom-Json
}

$pub = Join-Path $out 'publish\win-x64-msix'
$stage = Join-Path $out 'stage\msix'
Remove-Item $pub, $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory $stage | Out-Null

dotnet publish (Join-Path $root 'src\MothEidol.App') -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false -p:DebugType=none -p:DebugSymbols=false -o $pub --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

Move-Item (Join-Path $pub 'MothEidol.App.exe') (Join-Path $stage 'MothEidol.exe')
Copy-Item (Join-Path $root 'packaging\msix\Assets') (Join-Path $stage 'Assets') -Recurse

$manifest = Get-Content (Join-Path $root 'packaging\msix\AppxManifest.template.xml') -Raw
$manifest = $manifest.Replace('@IDENTITY_NAME@', $identity.identityName).Replace('@PUBLISHER@', $identity.publisher) `
    .Replace('@PUBLISHER_DISPLAY_NAME@', $identity.publisherDisplayName).Replace('@VERSION@', $msixVersion)
Set-Content (Join-Path $stage 'AppxManifest.xml') $manifest -Encoding UTF8

$package = Join-Path $out ("MothEidol-$version-win-x64" + $(if ($isTest) { '-test' } else { '' }) + '.msix')
Remove-Item $package -ErrorAction SilentlyContinue
& $makeappx pack /d $stage /p $package /o | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'makeappx failed.' }

if ($isTest) {
    $signtool = Find-SdkTool 'signtool.exe'
    $cert = New-SelfSignedCertificate -Type Custom -Subject $identity.publisher -KeyUsage DigitalSignature `
        -FriendlyName 'Moth-Eidol test' -CertStoreLocation 'Cert:\CurrentUser\My' `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
    $cer = Join-Path $out 'MothEidol-test.cer'
    Export-Certificate -Cert $cert -FilePath $cer | Out-Null
    & $signtool sign /fd SHA256 /sha1 $cert.Thumbprint $package | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'signtool failed.' }
    Write-Host "TEST package (not submittable): $package"
    Write-Host "To sideload: import $cer into 'Trusted People' (LocalMachine) as admin, then open the .msix."
} else {
    Write-Host "Store package (unsigned; the Store signs it): $package"
}
Remove-Item (Join-Path $out 'stage') -Recurse -Force
