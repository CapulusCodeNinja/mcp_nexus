<#
.SYNOPSIS
    Quick single-project coverage analysis for rapid feedback during development.

.DESCRIPTION
    This is a lightweight coverage analysis tool designed for fast, focused checks on
    individual test projects. Unlike Analyze-AllCoverage.ps1 (which aggregates across
    the entire solution), this script analyzes a single coverage file for quick iteration.

    Use this when:
    - Working on a specific test project and want fast feedback
    - Running coverage for a single assembly
    - Need a quick sanity check without full solution analysis

    For comprehensive solution-wide analysis, use Analyze-AllCoverage.ps1 instead.

.PARAMETER CoverageFile
    Path to a specific Cobertura XML coverage file. If not provided, searches for
    the first coverage file in the TestResults directory.

.PARAMETER TopClasses
    Number of classes to show in the low coverage report. Default: 25

.PARAMETER ShowAll
    If specified, shows all classes regardless of coverage level. Default: false

.EXAMPLE
    .\Analyze-BranchCoverage.ps1
    
    Quick analysis of the first found coverage file.

.EXAMPLE
    .\Analyze-BranchCoverage.ps1 -CoverageFile "C:\path\to\coverage.cobertura.xml"
    
    Analyze a specific coverage file.

.EXAMPLE
    .\Analyze-BranchCoverage.ps1 -TopClasses 50 -ShowAll
    
    Show top 50 classes including those with 100% coverage.

.NOTES
    File Name      : Analyze-BranchCoverage.ps1
    Prerequisite   : PowerShell 5.1 or later
    Usage          : For quick single-project checks; use Analyze-AllCoverage.ps1 for full solution analysis
    Related        : Analyze-AllCoverage.ps1 (comprehensive solution-wide analysis)
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$CoverageFile,

    [Parameter(Mandatory = $false)]
    [int]$TopClasses = 25,

    [Parameter(Mandatory = $false)]
    [switch]$ShowAll
)

# ============================================================================
# STEP 1: Locate Coverage File
# ============================================================================
# Find a single coverage file for quick analysis. If no specific file is
# provided, picks the first one found in the TestResults directory.

Write-Host "`n[Quick Coverage Check]" -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($CoverageFile)) {
    Write-Host "  Searching for coverage file..." -ForegroundColor Gray
    
    $searchPath = '..\build\tests\TestResults'
    $foundFile = Get-ChildItem -Path $searchPath -Filter 'coverage.cobertura.xml' -Recurse -ErrorAction SilentlyContinue | 
                 Select-Object -First 1
    
    if ($null -eq $foundFile) {
        Write-Host "ERROR: No coverage file found in '$searchPath'" -ForegroundColor Red
        Write-Host "TIP: Run 'dotnet test --collect:""XPlat Code Coverage""' first" -ForegroundColor Yellow
        exit 1
    }
    
    $CoverageFile = $foundFile.FullName
    Write-Host "  Found: $($foundFile.Name)" -ForegroundColor Green
}
else {
    # Validate provided file path
    if (-not (Test-Path $CoverageFile)) {
        Write-Host "ERROR: Coverage file not found: $CoverageFile" -ForegroundColor Red
        exit 1
    }
    Write-Host "  Analyzing: $CoverageFile" -ForegroundColor Green
}

# ============================================================================
# STEP 2: Parse Coverage File
# ============================================================================
# Load and parse the XML coverage data from the single file.

Write-Host "`n  Parsing coverage data..." -ForegroundColor Gray

try {
    [xml]$coverage = Get-Content $CoverageFile
}
catch {
    Write-Host "ERROR: Failed to parse coverage file: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# STEP 3: Extract Class-Level Metrics
# ============================================================================
# Process each class in the coverage report and calculate percentage rates.

$results = $coverage.coverage.packages.package | ForEach-Object {
    $packageName = $_.name
    
    $_.classes.class | ForEach-Object {
        # Convert coverage rates from decimal to percentage
        $lineRate = [math]::Round([double]$_.'line-rate' * 100, 1)
        $branchRate = [math]::Round([double]$_.'branch-rate' * 100, 1)
        
        # Extract simple class name (last segment after dots)
        $simpleClassName = $_.name.Split('.')[-1]
        
        # Create result object
        [PSCustomObject]@{
            Package    = $packageName
            Class      = $simpleClassName
            LineRate   = $lineRate
            BranchRate = $branchRate
        }
    }
}

if ($null -eq $results -or $results.Count -eq 0) {
    Write-Host "WARNING: No classes found in coverage report" -ForegroundColor Yellow
    exit 0
}

# ============================================================================
# STEP 4: Display Results
# ============================================================================
# Show low-coverage classes and overall statistics.

# Report 1: Classes with Lowest Branch Coverage
Write-Host "`n=== Classes with Lowest Branch Coverage ===" -ForegroundColor Yellow

if ($ShowAll) {
    # Show all classes when -ShowAll is specified
    Write-Host "Showing all $($results.Count) classes (sorted by branch coverage)`n" -ForegroundColor Gray
    $filteredResults = $results | Sort-Object BranchRate | Select-Object -First $TopClasses
}
else {
    # Default: only show classes with less than 100% coverage
    Write-Host "Showing classes with less than 100% coverage (use -ShowAll to see all)`n" -ForegroundColor Gray
    $filteredResults = $results | 
                      Where-Object { $_.BranchRate -lt 100 } | 
                      Sort-Object BranchRate | 
                      Select-Object -First $TopClasses
}

if ($filteredResults.Count -eq 0) {
    Write-Host "  ✓ All classes have 100% branch coverage!" -ForegroundColor Green
}
else {
    $filteredResults | Format-Table Package, Class, LineRate, BranchRate -AutoSize
    
    if ($filteredResults.Count -eq $TopClasses -and -not $ShowAll) {
        Write-Host "  (Showing top $TopClasses of $($results.Count) total classes)" -ForegroundColor Gray
    }
}

# Report 2: Overall File Statistics
Write-Host "`n=== Overall Coverage ===" -ForegroundColor Cyan

# Extract overall rates from the coverage root element
$totalLineRate = [math]::Round([double]$coverage.coverage.'line-rate' * 100, 1)
$totalBranchRate = [math]::Round([double]$coverage.coverage.'branch-rate' * 100, 1)

# Status indicators for quick visual assessment
$lineStatus = if ($totalLineRate -ge 75) { "✓" } else { "✗" }
$branchStatus = if ($totalBranchRate -ge 75) { "✓" } else { "✗" }

Write-Host "  $lineStatus Line Coverage:   $totalLineRate%" -ForegroundColor $(if ($totalLineRate -ge 75) { "Green" } else { "Yellow" })
Write-Host "  $branchStatus Branch Coverage: $totalBranchRate%" -ForegroundColor $(if ($totalBranchRate -ge 75) { "Green" } else { "Yellow" })

Write-Host "`n✓ Quick check complete!" -ForegroundColor Green
Write-Host "  TIP: For full solution analysis, use Analyze-AllCoverage.ps1`n" -ForegroundColor Gray



