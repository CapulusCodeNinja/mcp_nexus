#Requires -Version 6.0
<#
.SYNOPSIS
    Generates a comprehensive HTML code coverage report for the entire solution.

.DESCRIPTION
    This script orchestrates a complete coverage analysis workflow:
    1. Cleans previous coverage reports and test results
    2. Builds the solution in Release and Debug configurations
    3. Runs all tests with code coverage collection
    4. Aggregates coverage data from all test projects
    5. Generates a detailed HTML report using ReportGenerator
    6. Opens the report in your default browser

    The generated report includes:
    - Line and branch coverage metrics
    - Per-class and per-method coverage details
    - Visual coverage indicators
    - Historical trending (if run multiple times)
    - Risk hotspots (complex code with low coverage)

.PARAMETER Configuration
    Build configuration to use for testing. Valid values: 'Release', 'Debug'
    Default: 'Release'

.PARAMETER SkipBuild
    If specified, skips the build step (assumes solution is already built).
    Useful for quick re-runs when code hasn't changed. Default: false

.PARAMETER SkipClean
    If specified, preserves existing test results and reports.
    Useful for incremental coverage analysis. Default: false

.PARAMETER ReportTypes
    Report format(s) to generate. Common values: 'Html', 'HtmlSummary', 'Badges', 'Cobertura'
    Can specify multiple separated by semicolons. Default: 'Html'

.PARAMETER OpenReport
    If specified, automatically opens the report in the default browser. Default: true

.PARAMETER Filter
    Test filter expression to run only specific tests.
    Example: "FullyQualifiedName~winaidbg_engine"

.EXAMPLE
    .\New-CoverageReport.ps1
    
    Generates a complete coverage report (default: Release configuration, opens in browser).

.EXAMPLE
    .\New-CoverageReport.ps1 -Configuration Debug
    
    Generates coverage report using Debug configuration.

.EXAMPLE
    .\New-CoverageReport.ps1 -SkipBuild
    
    Generates report without rebuilding (uses existing binaries).

.EXAMPLE
    .\New-CoverageReport.ps1 -ReportTypes "Html;Badges;Cobertura"
    
    Generates multiple report formats.

.EXAMPLE
    .\New-CoverageReport.ps1 -Filter "FullyQualifiedName~winaidbg_engine" -OpenReport:$false
    
    Runs only engine tests, generates report without opening.

.NOTES
    File Name      : New-CoverageReport.ps1
    Prerequisite   : .NET 8 SDK, ReportGenerator tool (dotnet tool install -g dotnet-reportgenerator-globaltool)
    Output         : build/tests/coverage_report/index.html
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild,

    [Parameter(Mandatory = $false)]
    [switch]$SkipClean,

    [Parameter(Mandatory = $false)]
    [string]$ReportTypes = 'Html',

    [Parameter(Mandatory = $false)]
    [bool]$OpenReport = $true,

    [Parameter(Mandatory = $false)]
    [string]$Filter = ""
)

# ============================================================================
# STEP 1: Initialize Paths and Verify Prerequisites
# ============================================================================

Write-Host "`n[Coverage Report Generation]" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$coverageReportDir = Join-Path $solutionRoot 'build\tests\coverage_report'
$testResultsDir = Join-Path $solutionRoot 'build\tests\TestResults'
$runSettingsPath = Join-Path $solutionRoot 'coverlet.runsettings'

Write-Host "  Solution Root:    $solutionRoot" -ForegroundColor Gray
Write-Host "  Report Directory: $coverageReportDir" -ForegroundColor Gray
Write-Host "  Configuration:    $Configuration" -ForegroundColor Gray

# Verify ReportGenerator is installed
Write-Host "`n  Checking prerequisites..." -ForegroundColor Gray
$reportGenExists = Get-Command reportgenerator -ErrorAction SilentlyContinue
if (-not $reportGenExists) {
    Write-Host "  ✗ ReportGenerator not found!" -ForegroundColor Red
    Write-Host "  Install with: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
    exit 1
}
Write-Host "  ✓ ReportGenerator found" -ForegroundColor Green

# Verify run settings file exists
if (-not (Test-Path $runSettingsPath)) {
    Write-Host "  WARNING: Run settings file not found: $runSettingsPath" -ForegroundColor Yellow
    Write-Host "  Continuing without run settings..." -ForegroundColor Gray
    $runSettingsPath = $null
}
else {
    Write-Host "  ✓ Run settings found" -ForegroundColor Green
}

# ============================================================================
# STEP 2: Clean Previous Reports (Optional)
# ============================================================================

if (-not $SkipClean) {
    Write-Host "`n[1/5] Cleaning previous reports..." -ForegroundColor Cyan
    
    $itemsToClean = @(
        @{ Path = $coverageReportDir; Name = "Coverage report" },
        @{ Path = $testResultsDir; Name = "Test results" }
    )
    
    foreach ($item in $itemsToClean) {
        if (Test-Path $item.Path) {
            try {
                Remove-Item -Recurse -Force $item.Path -ErrorAction Stop
                Write-Host "  ✓ Cleaned $($item.Name)" -ForegroundColor Green
            }
            catch {
                Write-Host "  WARNING: Failed to clean $($item.Name): $_" -ForegroundColor Yellow
            }
        }
    }
}
else {
    Write-Host "`n[1/5] Skipping clean (incremental mode)" -ForegroundColor Gray
}

# ============================================================================
# STEP 3: Build Solution (Optional)
# ============================================================================

if (-not $SkipBuild) {
    Write-Host "`n[2/5] Building solution..." -ForegroundColor Cyan
    
    $buildScript = Join-Path $scriptDir 'New-SolutionBuild.ps1'
    
    try {
        & $buildScript -Configuration $Configuration
        
        if ($LASTEXITCODE -ne 0) {
            throw "Build script exited with code $LASTEXITCODE"
        }
        
        Write-Host "  ✓ Build completed successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Build FAILED: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "`n[2/5] Skipping build (using existing binaries)" -ForegroundColor Gray
}

# ============================================================================
# STEP 4: Run Tests with Coverage
# ============================================================================

Write-Host "`n[3/5] Running tests with coverage collection..." -ForegroundColor Cyan

# Build test command arguments
$testArgs = @(
    'test',
    '--no-build',
    '--configuration', $Configuration,
    '--collect', 'XPlat Code Coverage',
    '--results-directory', $testResultsDir
)

# Add run settings if available
if ($runSettingsPath) {
    $testArgs += '--settings'
    $testArgs += $runSettingsPath
}

# Add filter if specified
if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $testArgs += '--filter'
    $testArgs += $Filter
    Write-Host "  Test Filter: $Filter" -ForegroundColor Gray
}

# Execute tests
Push-Location $solutionRoot
try {
    Write-Host "  Running: dotnet $($testArgs -join ' ')" -ForegroundColor Gray
    Write-Host ""
    
    & dotnet $testArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "`n  ✓ Tests completed successfully" -ForegroundColor Green
}
catch {
    Write-Host "`n  ✗ Tests FAILED: $_" -ForegroundColor Red
    Write-Host "  Coverage report generation aborted." -ForegroundColor Yellow
    exit 1
}
finally {
    Pop-Location
}

# ============================================================================
# STEP 5: Locate Coverage Files
# ============================================================================

Write-Host "`n[4/5] Locating coverage files..." -ForegroundColor Cyan

$coverageFiles = Get-ChildItem -Path $testResultsDir -Filter 'coverage.cobertura.xml' -Recurse -ErrorAction SilentlyContinue

if ($null -eq $coverageFiles -or $coverageFiles.Count -eq 0) {
    Write-Host "  ✗ No coverage files found in $testResultsDir" -ForegroundColor Red
    Write-Host "  Coverage report generation aborted." -ForegroundColor Yellow
    exit 1
}

Write-Host "  ✓ Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green

# Join paths with semicolon separator for ReportGenerator
$coverageFilePaths = ($coverageFiles.FullName -join ';')

# ============================================================================
# STEP 6: Generate HTML Report
# ============================================================================

Write-Host "`n[5/5] Generating coverage report..." -ForegroundColor Cyan

try {
    # Build ReportGenerator arguments
    $reportGenArgs = @(
        "-verbosity:Verbose",
        "-title:WinAiDbg - Unit Tests Coverage Report",
        "-reports:$coverageFilePaths",
        "-targetdir:$coverageReportDir",
        "-reporttypes:$ReportTypes"
    )
    
    Write-Host "  Report Types: $ReportTypes" -ForegroundColor Gray
    Write-Host "  Output: $coverageReportDir" -ForegroundColor Gray
    Write-Host ""
    
    & reportgenerator $reportGenArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "ReportGenerator failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "`n  ✓ Report generated successfully" -ForegroundColor Green
}
catch {
    Write-Host "`n  ✗ Report generation FAILED: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# STEP 7: Open Report (Optional)
# ============================================================================

$reportIndexPath = Join-Path $coverageReportDir 'index.html'

if (Test-Path $reportIndexPath) {
    Write-Host "`n=== Coverage Report Ready ===" -ForegroundColor Cyan
    Write-Host "  Location: $reportIndexPath" -ForegroundColor Green
    
    if ($OpenReport) {
        Write-Host "`n  Opening report in browser..." -ForegroundColor Gray
        try {
            Start-Process $reportIndexPath
            Write-Host "  ✓ Report opened" -ForegroundColor Green
        }
        catch {
            Write-Host "  WARNING: Failed to open report: $_" -ForegroundColor Yellow
            Write-Host "  Please open manually: $reportIndexPath" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "`n  TIP: Open the report manually or run with -OpenReport:`$true" -ForegroundColor Gray
    }
}
else {
    Write-Host "`n  WARNING: Report file not found at expected location" -ForegroundColor Yellow
    Write-Host "  Expected: $reportIndexPath" -ForegroundColor Yellow
}

Write-Host "`n✓ Coverage report generation complete!`n" -ForegroundColor Green
