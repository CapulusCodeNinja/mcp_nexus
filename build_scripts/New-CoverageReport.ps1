# ---------------------------
# Paths
# ---------------------------
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$coverageReportDir = Join-Path $solutionRoot 'build\bin\coverage_report'
$testResultsDir = Join-Path $solutionRoot 'build\bin\TestResults'
$runSettings = (Resolve-Path (Join-Path $solutionRoot 'coverlet.runsettings')).Path

# ---------------------------
# Clean old reports
# ---------------------------
if (Test-Path $coverageReportDir) {
    Remove-Item -Recurse -Force $coverageReportDir
}

if (Test-Path $testResultsDir) {
    Remove-Item -Recurse -Force $testResultsDir
}

# ---------------------------
# Build projects and run tests with coverage
# ---------------------------
Push-Location $solutionRoot
try {
    dotnet clean

    dotnet build

    dotnet test `
    --no-build `
    --settings $runSettings `
    --collect "XPlat Code Coverage" `
    --results-directory $testResultsDir
} finally {
    Pop-Location
}

# ---------------------------
# Find coverage files
# ---------------------------
$coverageFiles = (Get-ChildItem -File -Recurse -Filter 'coverage.cobertura.xml' -Path $testResultsDir).FullName -join ';'

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