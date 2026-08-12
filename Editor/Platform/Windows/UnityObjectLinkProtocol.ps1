[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('install', 'uninstall', 'status', 'dispatch')]
    [string]$Command = 'status',

    [Parameter(Mandatory = $false)]
    [string]$Scheme = '',

    [Parameter(Mandatory = $false)]
    [string]$Uri = ''
)

$ErrorActionPreference = 'Stop'
$ProductRoot = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'UnityObjectLink'
$StableScript = Join-Path $ProductRoot 'bin\UnityObjectLinkProtocol.ps1'
$RegistrationDirectory = Join-Path $ProductRoot 'registrations'

function Assert-Scheme([string]$Value) {
    if ($Value -notmatch '^[A-Za-z][A-Za-z0-9+.-]{0,31}$') {
        throw 'Scheme must follow RFC 3986 syntax and contain at most 32 ASCII characters.'
    }

    return $Value.ToLowerInvariant()
}

function Get-ProtocolKey([string]$Value) {
    return "HKCU:\Software\Classes\$Value"
}

function Get-HandlerCommand() {
    $powerShell = Join-Path $PSHOME 'powershell.exe'
    return "`"$powerShell`" -NoProfile -ExecutionPolicy Bypass -File `"$StableScript`" -Command dispatch -Uri `"%1`""
}

function Test-OwnedRegistration([string]$Value) {
    $commandKey = Join-Path (Get-ProtocolKey $Value) 'shell\open\command'
    if (-not (Test-Path -LiteralPath $commandKey)) {
        return $false
    }

    $registeredCommand = (Get-Item -LiteralPath $commandKey).GetValue('')
    return $registeredCommand -is [string] -and [string]::Equals($registeredCommand, (Get-HandlerCommand), [StringComparison]::OrdinalIgnoreCase)
}

function Parse-Link([string]$RawUri) {
    if ([string]::IsNullOrEmpty($RawUri) -or $RawUri.Length -gt 8192 -or $RawUri -match '[\x00-\x1F\x7F]') {
        throw 'URI is empty, too long, or contains control characters.'
    }

    $parsed = [System.Uri]$RawUri
    $parsedScheme = Assert-Scheme $parsed.Scheme
    if (-not $parsed.IsAbsoluteUri -or $parsed.Host -ine 'select' -or ($parsed.AbsolutePath -ne '' -and $parsed.AbsolutePath -ne '/') -or $parsed.Fragment -ne '' -or $parsed.UserInfo -ne '' -or -not $parsed.IsDefaultPort) {
        throw 'URI does not use the supported select action.'
    }

    $parameters = @{}
    $pairs = $parsed.Query.TrimStart('?').Split('&')
    foreach ($pair in $pairs) {
        $separator = $pair.IndexOf('=')
        if ($separator -le 0 -or $separator -ne $pair.LastIndexOf('=')) {
            throw 'Malformed URI query parameter.'
        }

        $encodedName = $pair.Substring(0, $separator)
        $encodedValue = $pair.Substring($separator + 1)
        if ($encodedName -match '%(?![0-9A-Fa-f]{2})' -or $encodedValue -match '%(?![0-9A-Fa-f]{2})') {
            throw 'Invalid percent encoding.'
        }

        $name = [System.Uri]::UnescapeDataString($encodedName)
        $value = [System.Uri]::UnescapeDataString($encodedValue)
        if ($parameters.ContainsKey($name)) {
            throw 'Duplicate URI query parameter.'
        }

        $parameters[$name] = $value
    }

    if ($parameters.Count -ne 3 -or $parameters['v'] -ne '1' -or -not $parameters.ContainsKey('project') -or -not $parameters.ContainsKey('object')) {
        throw 'URI must contain exactly one v, project, and object parameter.'
    }

    $projectId = $parameters['project']
    if ($projectId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$' -or $projectId.Contains('..')) {
        throw 'Invalid Project ID.'
    }

    if ($parameters['object'].Length -gt 512 -or
        $parameters['object'] -match '[\x00-\x1F\x7F]' -or
        -not $parameters['object'].StartsWith('GlobalObjectId_V1-', [StringComparison]::Ordinal)) {
        throw 'Invalid GlobalObjectId.'
    }

    return @{
        Scheme = $parsedScheme
        ProjectId = $projectId
    }
}

function Install-Protocol([string]$Value) {
    $normalizedScheme = Assert-Scheme $Value
    $existingKey = Get-ProtocolKey $normalizedScheme
    if ((Test-Path -LiteralPath $existingKey) -and -not (Test-OwnedRegistration $normalizedScheme)) {
        throw "Refusing to replace protocol '$normalizedScheme' because it is owned by another application."
    }

    $stableDirectory = Split-Path -Parent $StableScript
    New-Item -ItemType Directory -Path $stableDirectory -Force | Out-Null
    if (-not [string]::Equals([IO.Path]::GetFullPath($PSCommandPath), [IO.Path]::GetFullPath($StableScript), [StringComparison]::OrdinalIgnoreCase)) {
        Copy-Item -LiteralPath $PSCommandPath -Destination $StableScript -Force
    }

    $protocolKey = Get-ProtocolKey $normalizedScheme
    $commandKey = Join-Path $protocolKey 'shell\open\command'
    New-Item -Path $commandKey -Force | Out-Null
    Set-Item -LiteralPath $protocolKey -Value "URL:Unity Object Link ($normalizedScheme)"
    New-ItemProperty -LiteralPath $protocolKey -Name 'URL Protocol' -Value '' -PropertyType String -Force | Out-Null

    Set-Item -LiteralPath $commandKey -Value (Get-HandlerCommand)
    New-Item -ItemType Directory -Path $RegistrationDirectory -Force | Out-Null
    $marker = Join-Path $RegistrationDirectory ($normalizedScheme + '.owner')
    [IO.File]::WriteAllText($marker, $StableScript, (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "STATUS=registered;SCHEME=$normalizedScheme"
}

function Remove-RegistrationMarker([string]$Value) {
    $marker = Join-Path $RegistrationDirectory ($Value + '.owner')
    [IO.File]::Delete($marker)
    $remainingMarkers = if ([IO.Directory]::Exists($RegistrationDirectory)) {
        [IO.Directory]::GetFiles($RegistrationDirectory, '*.owner', [IO.SearchOption]::TopDirectoryOnly)
    }
    else {
        @()
    }

    if ($remainingMarkers.Count -eq 0 -and [IO.File]::Exists($StableScript)) {
        [IO.File]::Delete($StableScript)
    }
}

function Uninstall-Protocol([string]$Value) {
    $normalizedScheme = Assert-Scheme $Value
    $protocolKey = Get-ProtocolKey $normalizedScheme
    if (-not (Test-Path -LiteralPath $protocolKey)) {
        Remove-RegistrationMarker $normalizedScheme
        Write-Output "STATUS=not-registered;SCHEME=$normalizedScheme"
        return
    }

    if (-not (Test-OwnedRegistration $normalizedScheme)) {
        throw "Refusing to remove protocol '$normalizedScheme' because it is owned by another application."
    }

    Remove-Item -LiteralPath $protocolKey -Recurse -Force
    Remove-RegistrationMarker $normalizedScheme
    Write-Output "STATUS=unregistered;SCHEME=$normalizedScheme"
}

function Get-ProtocolStatus([string]$Value) {
    $normalizedScheme = Assert-Scheme $Value
    if (Test-OwnedRegistration $normalizedScheme) {
        Write-Output "STATUS=registered;SCHEME=$normalizedScheme"
    }
    elseif (Test-Path -LiteralPath (Get-ProtocolKey $normalizedScheme)) {
        Write-Output "STATUS=owned-by-another-application;SCHEME=$normalizedScheme"
    }
    else {
        Write-Output "STATUS=not-registered;SCHEME=$normalizedScheme"
    }
}

function Dispatch-Link([string]$RawUri) {
    $route = Parse-Link $RawUri
    $instanceDirectory = Join-Path (Join-Path (Join-Path $ProductRoot 'instances') $route.Scheme) $route.ProjectId
    $heartbeat = Join-Path $instanceDirectory 'heartbeat.json'
    if (-not (Test-Path -LiteralPath $heartbeat -PathType Leaf)) {
        throw 'The target Unity project is not running.'
    }

    $heartbeatAge = [DateTime]::UtcNow - (Get-Item -LiteralPath $heartbeat).LastWriteTimeUtc
    if ($heartbeatAge.TotalSeconds -gt 15 -or $heartbeatAge.TotalSeconds -lt -5) {
        throw 'The target Unity project heartbeat is stale.'
    }

    $inbox = Join-Path $instanceDirectory 'inbox'
    New-Item -ItemType Directory -Path $inbox -Force | Out-Null
    $requestName = ([DateTime]::UtcNow.ToString('yyyyMMddHHmmssfffffff') + '-' + [Guid]::NewGuid().ToString('N'))
    $temporaryPath = Join-Path $inbox ($requestName + '.tmp')
    $requestPath = Join-Path $inbox ($requestName + '.request')
    $encoding = New-Object System.Text.UTF8Encoding($false, $true)
    [IO.File]::WriteAllText($temporaryPath, $RawUri, $encoding)
    Move-Item -LiteralPath $temporaryPath -Destination $requestPath
    Write-Output "STATUS=dispatched;PROJECT=$($route.ProjectId)"
}

try {
    switch ($Command) {
        'install' { Install-Protocol $Scheme }
        'uninstall' { Uninstall-Protocol $Scheme }
        'status' { Get-ProtocolStatus $Scheme }
        'dispatch' { Dispatch-Link $Uri }
    }
}
catch {
    [Console]::Error.WriteLine("Unity Object Link: $($_.Exception.Message)")
    exit 1
}
