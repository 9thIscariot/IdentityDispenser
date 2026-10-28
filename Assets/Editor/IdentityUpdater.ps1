# Copied to an isolated temporary directory before launch. Compatible with Windows PowerShell 5.1.
param([switch] $NoDialog)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$job = $null
$applying = $false
$committed = $false
$changed = [System.Collections.Generic.List[string]]::new()
$backedUp = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$guard = $null
$progressClock = [Diagnostics.Stopwatch]::StartNew()
$lastProgress = -1000
function Write-ProgressFile([string] $Message, [switch] $Force) {
    if ($Force -or $progressClock.ElapsedMilliseconds - $script:lastProgress -ge 500) {
        # Both sides permit concurrent access; a status read must never abort installation.
        $stream = [IO.FileStream]::new((Join-Path $PSScriptRoot 'progress.txt'), [IO.FileMode]::Create,
            [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
        try {
            $bytes = [Text.Encoding]::UTF8.GetBytes($Message)
            $stream.Write($bytes, 0, $bytes.Length)
        } finally { $stream.Dispose() }
        $script:lastProgress = $progressClock.ElapsedMilliseconds
    }
}

function Safe-Path([string] $Root, [string] $Relative) {
    if ([string]::IsNullOrWhiteSpace($Relative) -or [IO.Path]::IsPathRooted($Relative) -or
        $Relative.Contains(':') -or $Relative -match '[\x00-\x1f]' -or
        $Relative -match '(^|[/\\])(\.|\.\.)([/\\]|$)') { throw "Unsafe package path: $Relative" }
    foreach ($part in ($Relative -split '[/\\]')) {
        if (!$part -or $part.EndsWith('.') -or $part.EndsWith(' ') -or
            $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)') { throw "Invalid package path: $Relative" }
    }
    $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\') + '\'
    $result = [IO.Path]::GetFullPath([IO.Path]::Combine($prefix, $Relative))
    if (!$result.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Path escapes root.' }
    # Never follow junctions or symlinks in an existing installation.
    $cursor = $result
    while ($cursor -and $cursor.Length -ge $prefix.TrimEnd('\').Length) {
        if ([IO.File]::Exists($cursor) -or [IO.Directory]::Exists($cursor)) {
            if ([IO.File]::GetAttributes($cursor) -band [IO.FileAttributes]::ReparsePoint) {
                throw "Reparse point is not supported: $cursor"
            }
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
    return $result
}

function Read-Manifest([string] $Root) {
    $manifest = Get-Content -LiteralPath (Join-Path $Root 'identity-update.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.schema -ne 1 -or !$manifest.files -or !$manifest.executable -or !$manifest.version) {
        throw 'Invalid package manifest.'
    }
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($relative in $manifest.files) {
        $null = Safe-Path $Root $relative
        if ($relative -eq '.identity-update.lock' -or !$seen.Add($relative.Replace('/', '\'))) {
            throw 'Duplicate or reserved package path.'
        }
    }
    return $manifest
}

try {
    $job = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'job.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $install = [IO.Path]::GetFullPath($job.installDirectory)
    $exe = Safe-Path $install $job.executable
    if ([IO.Path]::GetFileName($exe) -ne $job.executable) { throw 'Invalid executable name.' }
    Write-ProgressFile '다운로드 파일 검증 중…' -Force
    if ((Get-FileHash -LiteralPath $job.archive -Algorithm SHA256).Hash -ne $job.digest) { throw 'ZIP checksum mismatch.' }
    $stage = Join-Path $PSScriptRoot 'stage'
    $backup = Join-Path $PSScriptRoot 'backup'
    $null = New-Item -ItemType Directory -Path $stage, $backup
    $zip = [IO.Compression.ZipFile]::OpenRead($job.archive)
    try {
        $entries = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $entryIndex = 0
        foreach ($entry in $zip.Entries) {
            $entryIndex++
            Write-ProgressFile ("압축 해제 중… {0}/{1}" -f $entryIndex, $zip.Entries.Count)
            $relative = $entry.FullName.TrimEnd('/', '\')
            if (!$relative) { continue }
            $target = Safe-Path $stage $relative
            if (!$entries.Add($target)) { throw 'Duplicate ZIP entry.' }
            if ((($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000) { throw 'Symlink ZIP entry.' }
            if (!$entry.Name) { $null = [IO.Directory]::CreateDirectory($target); continue }
            $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $false)
        }
    } finally { $zip.Dispose() }
    Write-ProgressFile '설치 파일 목록 확인 중…' -Force
    # Accept either build contents at ZIP root or exactly one wrapping folder.
    $manifests = @(Get-ChildItem -LiteralPath $stage -Filter 'identity-update.json' -Recurse -File)
    if ($manifests.Count -ne 1) { throw 'Expected one build manifest in ZIP.' }
    $source = $manifests[0].Directory.FullName
    $next = Read-Manifest $source
    if ($next.executable -ne $job.executable -or [string]$next.version -ne [string]$job.expectedVersion) {
        throw 'Package version or executable does not match release.'
    }
    foreach ($required in @($job.executable, 'UnityPlayer.dll', 'IdentityUpdater.ps1', 'identity-update.json')) {
        if ($next.files -notcontains $required) { throw "Required package file missing: $required" }
    }
    $dataDirectory = [IO.Path]::GetFileNameWithoutExtension($job.executable) + '_Data'
    if (!(Test-Path -LiteralPath (Safe-Path $source $dataDirectory) -PathType Container)) { throw 'Unity data directory missing.' }
    if (@(Get-ChildItem -LiteralPath $source -File -Recurse).Count -ne @($next.files).Count) {
        throw 'Package manifest does not match ZIP contents.'
    }
    $affected = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $fileIndex = 0
    foreach ($relative in $next.files) {
        $fileIndex++
        Write-ProgressFile ("설치 파일 확인 중… {0}/{1}" -f $fileIndex, $next.files.Count)
        $from = Safe-Path $source $relative
        if (!(Test-Path -LiteralPath $from -PathType Leaf)) { throw "Package file missing: $relative" }
        $null = Safe-Path $install $relative
        $null = $affected.Add($relative)
    }
    $previous = Read-Manifest $install
    if ($previous.executable -ne $job.executable) { throw 'Installed manifest mismatch.' }
    foreach ($relative in $previous.files) { $null = $affected.Add($relative) }
    foreach ($relative in $affected) { $null = Safe-Path $install $relative }
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'ready'), 'ready')
    $parent = Get-Process -Id $job.parentId -ErrorAction SilentlyContinue
    if ($parent -and !$parent.WaitForExit(120000)) { throw 'Application did not exit; update cancelled.' }
    $guard = [IO.File]::Open((Join-Path $install '.identity-update.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    # If another instance is still using this build, leave every installed file untouched.
    foreach ($process in @(Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($exe)) -ErrorAction SilentlyContinue)) {
        if ($process.Path -eq $exe) { throw 'Another application instance is still running.' }
    }
    foreach ($relative in $affected) {
        $target = Safe-Path $install $relative
        if (Test-Path -LiteralPath $target -PathType Container) { throw "A directory blocks the update: $relative" }
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            $saved = Safe-Path $backup $relative
            $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($saved))
            [IO.File]::Copy($target, $saved, $false)
            $null = $backedUp.Add($relative)
        }
    }
    $applying = $true
    foreach ($relative in $affected) {
        $target = Safe-Path $install $relative
        $changed.Add($relative)
        if ($next.files -contains $relative) {
            $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))
            [IO.File]::Copy((Safe-Path $source $relative), $target, $true)
        } elseif (Test-Path -LiteralPath $target -PathType Leaf) {
            Remove-Item -LiteralPath $target -Force
        }
    }
    $committed = $true
    $guard.Dispose(); $guard = $null
    Start-Process -FilePath $exe -WorkingDirectory $install -WindowStyle Normal
    # Only remove our verified generated temp directory after successful installation/restart.
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    $workRoot = [IO.Path]::GetFullPath($PSScriptRoot)
    if ($workRoot.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($workRoot) -match '^IdentityDispenserUpdate-[a-f0-9]{32}$') {
        try { Remove-Item -LiteralPath $workRoot -Recurse -Force } catch { }
    }
} catch {
    $failure = $_.Exception.Message
    if ($applying -and !$committed) {
        $restoreErrors = [System.Collections.Generic.List[string]]::new()
        foreach ($relative in $changed) {
            try {
                $target = Safe-Path $install $relative
                if ($backedUp.Contains($relative)) {
                    $saved = Safe-Path $backup $relative
                    # A failed write may leave the original intact (e.g. a locked DLL).
                    if (!(Test-Path -LiteralPath $target -PathType Leaf) -or
                        (Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $saved).Hash) {
                        [IO.File]::Copy($saved, $target, $true)
                    }
                }
                elseif (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target -Force }
            } catch { $restoreErrors.Add("${relative}: $($_.Exception.Message)") }
        }
        if ($restoreErrors.Count) { $failure += "`nRestore failed: $($restoreErrors -join '; ')`nBackup: $backup" }
        else { $failure += "`nPrevious files restored. Please start the application again." }
    }
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'error.txt'), $failure)
    if (!$NoDialog -and $job -and !(Get-Process -Id $job.parentId -ErrorAction SilentlyContinue)) {
        Add-Type -AssemblyName System.Windows.Forms
        $null = [Windows.Forms.MessageBox]::Show("$failure`nDetails: $PSScriptRoot", 'Identity Dispenser update failed')
    }
    exit 1
} finally { if ($guard) { $guard.Dispose() } }
