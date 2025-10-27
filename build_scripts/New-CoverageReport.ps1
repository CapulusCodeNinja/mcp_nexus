# ---------------------------
# Paths
# ---------------------------
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$coverageReportDir = Join-Path $solutionRoot 'bin\coverage_report'
$runSettings = (Resolve-Path (Join-Path $solutionRoot 'coverlet.runsettings')).Path

# ---------------------------
# Clean old reports
# ---------------------------
if (Test-Path $coverageReportDir) {
    Remove-Item -Recurse -Force $coverageReportDir
}

# Remove any old TestResults directories under the solution root
Get-ChildItem -Directory -Recurse -Filter 'TestResults' -Path $solutionRoot | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# ---------------------------
# Run tests with coverage
# ---------------------------
Push-Location $solutionRoot
dotnet test --settings $runSettings --collect:"XPlat Code Coverage"
Pop-Location

# ---------------------------
# Find coverage files
# ---------------------------
$coverageFiles = (Get-ChildItem -File -Recurse -Filter 'coverage.cobertura.xml' -Path $solutionRoot).FullName -join ';'

# ---------------------------
# Generate report
# ---------------------------
reportgenerator -verbosity:"Verbose" `
    -title:"Nexus - Unit Tests Coverage Report" `
    -reports:"$coverageFiles" `
    -targetdir:$coverageReportDir `
    -reporttypes:"Html"

# ---------------------------
# Open report
# ---------------------------
Start-Process (Join-Path $coverageReportDir 'index.html')