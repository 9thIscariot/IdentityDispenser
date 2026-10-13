# Run with Windows PowerShell 5.1. Fixtures stay in the ignored Temp directory.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$project = Split-Path $PSScriptRoot -Parent
$testRoot = Join-Path $project ('Temp/AutoUpdateChecks/' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $testRoot
$dummy = Join-Path $testRoot 'TestDispenser.exe'
Add-Type -OutputAssembly $dummy -OutputType WindowsApplication -TypeDefinition @'
using System;
using System.IO;
using System.Threading;
public static class DummyApp {
    public static void Main(string[] args) {
        if (args.Length == 1) {
            for (int i = 0; i < 1200 && !File.Exists(args[0]); i++) Thread.Sleep(50);
        } else File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launched.txt"), "started");
    }
}
'@
$unityVersion = ((Get-Content (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -First 1) -split ': ')[1]
$compiler = "C:/Program Files/Unity/Hub/Editor/$unityVersion/Editor/Data/DotNetSdkRoslyn/csc.dll"
$versionAssembly = Join-Path $testRoot 'ReleaseVersion.dll'
$framework = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
& dotnet exec --roll-forward Major $compiler -nologo -target:library ('-r:' + $framework + '/mscorlib.dll') ('-r:' + $framework + '/System.dll') ('-out:' + $versionAssembly) (Join-Path $project 'Assets/Scripts/ReleaseVersion.cs')
if ($LASTEXITCODE -ne 0) { throw 'Version compilation failed' }
Add-Type -Path $versionAssembly
function Assert([bool] $Condition, [string] $Message) { if (!$Condition) { throw $Message } }
[long]$v = 0
Assert ([ReleaseVersion]::TryParse('1015ver', 2026, [ref]$v) -and $v -eq 202610150000) 'Short date tag failed'
Assert ([ReleaseVersion]::TryParse('v20261015.2', 0, [ref]$v) -and $v -eq 202610150002) 'Revision tag failed'
Assert (![ReleaseVersion]::TryParse('20260230', 0, [ref]$v)) 'Invalid calendar date accepted'
Assert (![ReleaseVersion]::TryParse('LimbusCompany', 2026, [ref]$v)) 'Legacy tag accepted'
Assert (![ReleaseVersion]::TryParse('1015ver', 0, [ref]$v)) 'Build version missing year accepted'
Write-Output 'PASS: date version parsing'

foreach ($scenario in @('success', 'wrapped', 'bad-hash', 'bad-version', 'traversal', 'locked-file')) {
    $root = Join-Path $testRoot $scenario
    $install = Join-Path $root 'installation space 한글'
    $payload = Join-Path $root 'payload'
    $work = Join-Path $root 'work'
    $parentDirectory = Join-Path $root 'parent'
    $null = New-Item -ItemType Directory -Path $install, $payload, $work, $parentDirectory
    Copy-Item -LiteralPath $dummy -Destination (Join-Path $parentDirectory 'TestDispenser.exe')
    foreach ($directory in @($install, $payload)) {
        Copy-Item -LiteralPath $dummy -Destination (Join-Path $directory 'TestDispenser.exe')
        Copy-Item -LiteralPath (Join-Path $project 'Assets/Editor/IdentityUpdater.ps1') -Destination (Join-Path $directory 'IdentityUpdater.ps1')
        $null = New-Item -ItemType Directory -Path (Join-Path $directory 'TestDispenser_Data')
        [IO.File]::WriteAllText((Join-Path $directory 'UnityPlayer.dll'), 'fixture')
        [IO.File]::WriteAllText((Join-Path $directory 'TestDispenser_Data/data'), 'data')
    }
    [IO.File]::WriteAllText((Join-Path $install 'a.txt'), 'old')
    [IO.File]::WriteAllText((Join-Path $install 'z.txt'), 'old-locked')
    [IO.File]::WriteAllText((Join-Path $install 'obsolete.txt'), 'obsolete')
    [IO.File]::WriteAllText((Join-Path $payload 'a.txt'), 'new')
    [IO.File]::WriteAllText((Join-Path $payload 'z.txt'), 'new-locked')
    [IO.File]::WriteAllText((Join-Path $payload 'b-new.txt'), 'added')
    foreach ($directory in @($install, $payload)) {
        $files = @(Get-ChildItem -LiteralPath $directory -Recurse -File | ForEach-Object { $_.FullName.Substring($directory.Length + 1).Replace('\', '/') }) + 'identity-update.json'
        $version = if ($directory -eq $install) { '202610010000' } else { '202610150000' }
        @{ schema = 1; version = $version; executable = 'TestDispenser.exe'; files = @($files | Sort-Object) } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $directory 'identity-update.json') -Encoding UTF8
    }
    [IO.File]::WriteAllText((Join-Path $install 'personal.txt'), 'keep')
    $archive = Join-Path $work 'update.zip'
    [IO.Compression.ZipFile]::CreateFromDirectory($payload, $archive, [IO.Compression.CompressionLevel]::Optimal, ($scenario -eq 'wrapped'))
    if ($scenario -eq 'traversal') {
        $zip = [IO.Compression.ZipFile]::Open($archive, 'Update')
        $null = $zip.CreateEntry('../escape.txt')
        $zip.Dispose()
    }
    $gate = Join-Path $root 'exit-parent'
    $parent = Start-Process -FilePath (Join-Path $parentDirectory 'TestDispenser.exe') -ArgumentList ('"' + $gate + '"') -WindowStyle Hidden -PassThru
    $hash = (Get-FileHash -LiteralPath $archive).Hash
    if ($scenario -eq 'bad-hash') { $hash = '0' * 64 }
    $expected = if ($scenario -eq 'bad-version') { '202610160000' } else { '202610150000' }
    @{ installDirectory = $install; executable = 'TestDispenser.exe'; archive = $archive; digest = $hash;
        expectedVersion = $expected; parentId = $parent.Id } | ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $work 'job.json') -Encoding UTF8
    Copy-Item -LiteralPath (Join-Path $project 'Assets/Editor/IdentityUpdater.ps1') -Destination (Join-Path $work 'IdentityUpdater.ps1')
    $lock = $null
    $helper = $null
    try {
        if ($scenario -eq 'locked-file') { $lock = [IO.File]::Open((Join-Path $install 'z.txt'), 'Open', 'Read', 'Read') }
        $helper = Start-Process -FilePath "$PSHOME/powershell.exe" -WindowStyle Hidden -PassThru -ArgumentList (
            '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + (Join-Path $work 'IdentityUpdater.ps1') + '" -NoDialog')
        $deadline = [DateTime]::UtcNow.AddSeconds(25)
        while (!$helper.HasExited -and !(Test-Path -LiteralPath (Join-Path $work 'ready'))) {
            if ([DateTime]::UtcNow -gt $deadline) { throw 'Preparation timeout' }
            Start-Sleep -Milliseconds 100
        }
        [IO.File]::WriteAllText($gate, 'exit')
        Assert ($helper.WaitForExit(15000)) 'Updater exit timeout'
        $errorPath = Join-Path $work 'error.txt'
        if ($scenario -in @('success', 'wrapped')) {
            if (Test-Path -LiteralPath $errorPath) { throw (Get-Content -LiteralPath $errorPath -Raw) }
            Assert ((Get-Content -LiteralPath (Join-Path $install 'a.txt') -Raw) -eq 'new') 'New files not installed'
            Assert (!(Test-Path -LiteralPath (Join-Path $install 'obsolete.txt'))) 'Obsolete file retained'
            $deadline = [DateTime]::UtcNow.AddSeconds(5)
            while (!(Test-Path -LiteralPath (Join-Path $install 'launched.txt')) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
            Assert (Test-Path -LiteralPath (Join-Path $install 'launched.txt')) 'Updated application not restarted'
        } else {
            Assert (Test-Path -LiteralPath $errorPath) 'Invalid update was accepted'
            Assert ((Get-Content -LiteralPath (Join-Path $install 'a.txt') -Raw) -eq 'old') 'Original file not preserved/restored'
            Assert (Test-Path -LiteralPath (Join-Path $install 'obsolete.txt')) 'Original obsolete file not preserved/restored'
            Assert (!(Test-Path -LiteralPath (Join-Path $install 'b-new.txt'))) 'New file left after rollback'
            Assert (!(Test-Path -LiteralPath (Join-Path $work 'escape.txt'))) 'ZIP escaped staging directory'
            if ($scenario -eq 'locked-file') {
                Assert ((Get-Content -LiteralPath $errorPath -Raw) -match 'Previous files restored') 'Rollback failed'
            }
        }
        Assert ((Get-Content -LiteralPath (Join-Path $install 'personal.txt') -Raw) -eq 'keep') 'Personal file changed'
        Write-Output "PASS: $scenario"
    } finally {
        if ($lock) { $lock.Dispose() }
        if ($helper -and !$helper.HasExited) { $helper.Kill() }
        if (!$parent.HasExited) { $parent.Kill() }
    }
}
