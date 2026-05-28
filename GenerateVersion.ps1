$assemblyInfoPath = "$PSScriptRoot\Properties\AssemblyInfo.cs"

if (-not (Test-Path $assemblyInfoPath)) {
    Write-Error "AssemblyInfo.cs not found: $assemblyInfoPath"
    exit 1
}

# Get current date
$now = Get-Date
$month = $now.Month.ToString("D1")  # One digit for month (1-12)
$day = $now.Day.ToString("D2")     # Two digits for day (01-31)
$dayOfMonth = $month + $day        # Example: 417 for April 17

# Read current version from file to get revision
$content = Get-Content $assemblyInfoPath -Raw
$match = $content | Select-String '\[assembly: AssemblyVersion\("1\.\d+\.(?<MM>\d+)\.(?<RR>\d+)"\)\]'
if ($match) {
    $currentRevision = [int]$match.Matches[0].Groups["RR"].Value
    $revision = $currentRevision + 1
} else {
    $revision = 73  # Default revision if not found
}

# Format version: 1.YY.MMM.RR (MM = month+day, RR = revision)
$year = $now.ToString("yy")
$version = "1.$year.$dayOfMonth.$revision"

# Replace AssemblyVersion and AssemblyFileVersion
$content = $content -replace '\[assembly: AssemblyVersion\("\d+\.\d+\.\d+\.\d+"\)\]', "[assembly: AssemblyVersion(`"$version`")]"
$content = $content -replace '\[assembly: AssemblyFileVersion\("\d+\.\d+\.\d+\.\d+"\)\]', "[assembly: AssemblyFileVersion(`"$version`")]"

# Write back
Set-Content -Path $assemblyInfoPath -Value $content -Encoding UTF8

Write-Host "Version updated: $version" -ForegroundColor Green