[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Scheme = ("unity-object-link-e2e-" + $PID),

    [Parameter(Mandatory = $false)]
    [string]$ProjectId = ("e2e-" + $PID)
)

$ErrorActionPreference = 'Stop'
if ($Scheme -notmatch '^[A-Za-z][A-Za-z0-9+.-]{0,31}$') {
    throw 'The E2E scheme is invalid.'
}
$Scheme = $Scheme.ToLowerInvariant()
if ($ProjectId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$' -or $ProjectId.Contains('..')) {
    throw 'The E2E Project ID is invalid.'
}

$Handler = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\Editor\Platform\Windows\UnityObjectLinkProtocol.ps1'))
$ProductRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'UnityObjectLink'
$StableScript = Join-Path $ProductRoot 'bin\UnityObjectLinkProtocol.ps1'
$Instance = Join-Path (Join-Path (Join-Path $ProductRoot 'instances') $Scheme) $ProjectId
$Inbox = Join-Path $Instance 'inbox'
$Heartbeat = Join-Path $Instance 'heartbeat.json'
$Installed = $false
$ForeignScheme = 'uol-foreign-' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
$ForeignProtocolKey = "HKCU:\Software\Classes\$ForeignScheme"
$ForeignCreated = $false

function Invoke-Handler([string]$Command, [string]$Uri = '') {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $Handler, '-Command', $Command, '-Scheme', $Scheme)
    if (-not [string]::IsNullOrEmpty($Uri)) {
        $arguments += @('-Uri', $Uri)
    }

    $output = & powershell.exe @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Handler command '$Command' failed: $($output -join [Environment]::NewLine)"
    }

    return ($output -join [Environment]::NewLine)
}

function Remove-EmptyDirectory([string]$Path) {
    if ([IO.Directory]::Exists($Path) -and [IO.Directory]::GetFileSystemEntries($Path).Length -eq 0) {
        [IO.Directory]::Delete($Path)
    }
}

try {
    if (-not [IO.File]::Exists($Handler)) {
        throw "Protocol handler not found: $Handler"
    }

    if (Test-Path -LiteralPath $ForeignProtocolKey) {
        throw "Refusing to reuse an existing foreign-registration test key: $ForeignProtocolKey"
    }

    $foreignCommandKey = Join-Path $ForeignProtocolKey 'shell\open\command'
    New-Item -Path $foreignCommandKey -Force | Out-Null
    $ForeignCreated = $true
    Set-Item -LiteralPath $ForeignProtocolKey -Value "URL:Foreign owner ($ForeignScheme)"
    New-ItemProperty -LiteralPath $ForeignProtocolKey -Name 'URL Protocol' -Value '' -PropertyType String -Force | Out-Null
    $foreignCommand = "`"$env:ComSpec`" /d /c echo `"$StableScript`""
    Set-Item -LiteralPath $foreignCommandKey -Value $foreignCommand
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $foreignInstallOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Handler -Command install -Scheme $ForeignScheme 2>&1
    $foreignInstallExitCode = $LASTEXITCODE
    $foreignUninstallOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Handler -Command uninstall -Scheme $ForeignScheme 2>&1
    $foreignUninstallExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    $preservedCommand = (Get-Item -LiteralPath $foreignCommandKey).GetValue('')
    if ($foreignInstallExitCode -eq 0 -or $foreignUninstallExitCode -eq 0 -or $preservedCommand -ne $foreignCommand) {
        throw "Foreign registration ownership was not preserved. Install=$foreignInstallExitCode Uninstall=$foreignUninstallExitCode"
    }

    $Installed = $true
    $installOutput = Invoke-Handler 'install'
    if ($installOutput -notmatch 'STATUS=registered') {
        throw "Install did not report registration: $installOutput"
    }

    $statusOutput = Invoke-Handler 'status'
    if ($statusOutput -notmatch 'STATUS=registered') {
        throw "Status did not confirm registration: $statusOutput"
    }

    $invalidUri = "${Scheme}://select?v=1&project=..%2Fescape&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0"
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $invalidOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Handler -Command dispatch -Uri $invalidUri 2>&1
    $invalidExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($invalidExitCode -eq 0) {
        throw "Traversal URI was unexpectedly accepted: $($invalidOutput -join [Environment]::NewLine)"
    }

    [IO.Directory]::CreateDirectory($Inbox) | Out-Null
    [IO.File]::WriteAllText($Heartbeat, '{"version":1}', (New-Object Text.UTF8Encoding($false, $true)))
    $uri = "${Scheme}://select?v=1&project=$ProjectId&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0"

    [IO.File]::SetLastWriteTimeUtc($Heartbeat, [DateTime]::UtcNow.AddSeconds(-20))
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $staleOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $Handler -Command dispatch -Uri $uri 2>&1
    $staleExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($staleExitCode -eq 0) {
        throw "A stale heartbeat was unexpectedly accepted: $($staleOutput -join [Environment]::NewLine)"
    }
    [IO.File]::SetLastWriteTimeUtc($Heartbeat, [DateTime]::UtcNow)

    Start-Process -FilePath $uri -WindowStyle Hidden
    $request = $null
    foreach ($attempt in 1..50) {
        $request = Get-ChildItem -LiteralPath $Inbox -Filter '*.request' -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -ne $request) {
            break
        }

        Start-Sleep -Milliseconds 200
    }

    if ($null -eq $request) {
        throw 'OS protocol activation did not create an inbox request within 10 seconds.'
    }

    $delivered = [Uri][IO.File]::ReadAllText($request.FullName)
    $expected = [Uri]$uri
    if ($delivered.Scheme -ne $expected.Scheme -or $delivered.Host -ne 'select' -or $delivered.Query -ne $expected.Query) {
        throw "Delivered URI was not semantically equivalent: $($delivered.AbsoluteUri)"
    }

    $uninstallOutput = Invoke-Handler 'uninstall'
    if ($uninstallOutput -notmatch 'STATUS=unregistered' -or (Test-Path -LiteralPath "HKCU:\Software\Classes\$Scheme")) {
        throw "Uninstall did not remove the test registration: $uninstallOutput"
    }
    $Installed = $false
    $afterUninstallStatus = Invoke-Handler 'status'
    if ($afterUninstallStatus -notmatch 'STATUS=not-registered') {
        throw "Status did not confirm uninstall: $afterUninstallStatus"
    }

    Write-Output "E2E_PASS=True;NEGATIVE_VALIDATION=True;STALE_HEARTBEAT=True;FOREIGN_OWNERSHIP=True;SCHEME=$Scheme;PROJECT=$ProjectId"
    Write-Output "DELIVERED_URI=$($delivered.AbsoluteUri)"
}
finally {
    if ($Installed) {
        try {
            Invoke-Handler 'uninstall' | Out-Null
        }
        catch {
            Write-Warning $_.Exception.Message
        }
    }

    if ([IO.Directory]::Exists($Inbox)) {
        foreach ($file in [IO.Directory]::GetFiles($Inbox)) {
            [IO.File]::Delete($file)
        }
    }

    if ([IO.File]::Exists($Heartbeat)) {
        [IO.File]::Delete($Heartbeat)
    }
    Remove-EmptyDirectory $Inbox
    Remove-EmptyDirectory $Instance
    Remove-EmptyDirectory (Split-Path -Parent $Instance)
    if ($ForeignCreated -and (Test-Path -LiteralPath $ForeignProtocolKey)) {
        Remove-Item -LiteralPath $ForeignProtocolKey -Recurse -Force
    }
}
