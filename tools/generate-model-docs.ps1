#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates metromania-model.md from a template by replacing CODE placeholders
    with C# type definitions extracted from source files.

.DESCRIPTION
    Placeholder syntax inside the template:

      <!-- CODE:relative/path.cs -->            extracts all types from the file
      <!-- CODE:relative/path.cs#TypeName -->    extracts a single named type

    Extraction strips: using directives, namespace declarations, and XML doc
    comments (/// lines). Paths are resolved relative to -RepoRoot.

.PARAMETER TemplatePath
    Path to the .template.md file.

.PARAMETER OutputPath
    Path where the generated .md file will be written.

.PARAMETER RepoRoot
    Repository root directory. Defaults to the current directory.

.EXAMPLE
    ./tools/generate-model-docs.ps1 `
        -TemplatePath src/MetroMania.PlayerTemplate/metromania-model.template.md `
        -OutputPath   src/MetroMania.PlayerTemplate/metromania-model.md
#>
param(
    [Parameter(Mandatory)][string]$TemplatePath,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$RepoRoot = (Get-Location)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-AllTypes([string]$FilePath) {
    $lines = Get-Content $FilePath
    $result = [System.Collections.Generic.List[string]]::new()
    $inLeading = $true

    foreach ($line in $lines) {
        if ($line -match '^\s*using\s+')     { continue }
        if ($line -match '^\s*namespace\s+')  { continue }
        if ($line -match '^\s*///')           { continue }
        if ($inLeading -and $line.Trim() -eq '') { continue }
        $inLeading = $false
        $result.Add($line)
    }

    while ($result.Count -gt 0 -and $result[$result.Count - 1].Trim() -eq '') {
        $result.RemoveAt($result.Count - 1)
    }
    return $result
}

function Get-SingleType([string]$FilePath, [string]$TypeName) {
    $lines = Get-Content $FilePath
    $escaped = [regex]::Escape($TypeName)

    $start = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "\b(class|record|struct|interface|enum)\s+$escaped\b") {
            $start = $i; break
        }
    }
    if ($start -lt 0) { throw "Type '$TypeName' not found in $FilePath" }

    # Single-line type (e.g. record struct or sealed record ending with ;)
    if ($lines[$start].TrimEnd().EndsWith(';')) {
        return @($lines[$start])
    }

    # Multi-line: collect until braces balance
    $collected = [System.Collections.Generic.List[string]]::new()
    $depth = 0; $opened = $false

    for ($i = $start; $i -lt $lines.Count; $i++) {
        $collected.Add($lines[$i])
        foreach ($ch in $lines[$i].ToCharArray()) {
            if ($ch -eq '{') { $depth++; $opened = $true }
            elseif ($ch -eq '}') { $depth-- }
        }
        if ($opened -and $depth -eq 0) { break }
    }

    # Strip XML doc comments and collapse consecutive blank lines
    $result = [System.Collections.Generic.List[string]]::new()
    $prevBlank = $false
    foreach ($line in $collected) {
        if ($line -match '^\s*///') { continue }
        $isBlank = $line.Trim() -eq ''
        if ($isBlank -and $prevBlank) { continue }
        $result.Add($line)
        $prevBlank = $isBlank
    }
    return $result
}

# ── Main ──────────────────────────────────────────────────────────────────────

$template = (Get-Content $TemplatePath -Raw) -replace "`r`n", "`n"
$fence = '```'

$output = [regex]::Replace($template, '<!-- CODE:(.+?) -->', {
    param($m)
    $ref = $m.Groups[1].Value.Trim()

    if ($ref -match '^(.+?)#(.+)$') {
        $relPath  = $Matches[1]
        $typeName = $Matches[2]
    } else {
        $relPath  = $ref
        $typeName = $null
    }

    $fullPath = Join-Path $RepoRoot $relPath
    if (-not (Test-Path $fullPath)) {
        throw "Source file not found: $fullPath (placeholder: CODE:$ref)"
    }

    if ($typeName) {
        $extracted = Get-SingleType -FilePath $fullPath -TypeName $typeName
    } else {
        $extracted = Get-AllTypes -FilePath $fullPath
    }

    $code = $extracted -join "`n"
    return "${fence}csharp`n${code}`n${fence}"
})

$dir = Split-Path $OutputPath -Parent
if ($dir -and -not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

[System.IO.File]::WriteAllText(
    $OutputPath,
    $output,
    [System.Text.UTF8Encoding]::new($false)
)
Write-Host "Generated: $OutputPath"
