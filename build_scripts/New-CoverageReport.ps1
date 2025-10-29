# ---------------------------
# Paths
# ---------------------------
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$coverageReportDir = Join-Path $solutionRoot 'build\tests\coverage_report'
$testResultsDir = Join-Path $solutionRoot 'build\tests\TestResults'
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
# Build projects 
# ---------------------------
& $solutionRoot\build_scripts\New-SolutionBuild.ps1
if ($LastExitCode -ne 0) {
    Write-Error "Failed to build solution"
    Exit 1
}

# ---------------------------
# Run tests with coverage
# ---------------------------
Push-Location $solutionRoot
try {
    dotnet test `
    --settings $runSettings `
    --collect "XPlat Code Coverage" `
    --results-directory $testResultsDir
} finally {
    Pop-Location
}

if ($LastExitCode -ne 0) {
    Write-Error "Failed to run the tests with coverage"
    Exit 1
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

if ($LastExitCode -ne 0) {
    Write-Error "Failed to generate the coverage report"
    Exit 1
}

# ---------------------------
# Open report
# ---------------------------
Start-Process (Join-Path $coverageReportDir 'index.html')