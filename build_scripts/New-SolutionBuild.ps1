#Requires -Version 6.0
<#
.SYNOPSIS
    Builds the entire solution in both Release and Debug configurations.

.DESCRIPTION
    This script performs a complete solution build with the following steps:
    1. Cleans the build directory to ensure a fresh build
    2. Builds all projects in Release configuration
    3. Builds all projects in Debug configuration

    This dual-configuration build ensures:
    - Release builds are optimized and ready for deployment
    - Debug builds include symbols and are ready for debugging/testing
    - Both configurations are in sync and build successfully

    The script automatically restores NuGet packages as needed and provides
    detailed error messages if any build step fails.

.PARAMETER Configuration
    Build configuration(s) to use. Valid values: 'Release', 'Debug', 'Both'
    Default: 'Both' (builds both configurations)

.PARAMETER Clean
    If specified, cleans the build directory before building. Default: true

.PARAMETER SkipRestore
    If specified, skips NuGet package restore (assumes packages are already restored).
    Default: false

.PARAMETER Verbosity
    MSBuild verbosity level. Valid values: 'quiet', 'minimal', 'normal', 'detailed', 'diagnostic'
    Default: 'minimal'

.EXAMPLE
    .\New-SolutionBuild.ps1
    
    Builds the solution in both Release and Debug configurations (default).

.EXAMPLE
    .\New-SolutionBuild.ps1 -Configuration Release
    
    Builds only the Release configuration.

.EXAMPLE
    .\New-SolutionBuild.ps1 -Configuration Debug -SkipRestore
    
    Builds only Debug configuration without restoring packages.

.EXAMPLE
    .\New-SolutionBuild.ps1 -Clean:$false -Verbosity detailed
    
    Builds without cleaning, using detailed output.

.NOTES
    File Name      : New-SolutionBuild.ps1
    Prerequisite   : .NET 8 SDK or later
    Usage          : Run from repository root or build_scripts directory
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('Release', 'Debug', 'Both')]
    [string]$Configuration = 'Both',

    [Parameter(Mandatory = $false)]
    [bool]$Clean = $true,

    [Parameter(Mandatory = $false)]
    [switch]$SkipRestore,

    [Parameter(Mandatory = $false)]
    [ValidateSet('quiet', 'minimal', 'normal', 'detailed', 'diagnostic')]
    [string]$Verbosity = 'minimal'
)

# ============================================================================
# STEP 1: Initialize Paths
# ============================================================================
# Determine repository root and build output directory.

Write-Host "`n[Solution Build]" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$buildDir = Join-Path $solutionRoot 'build'

Write-Host "  Solution Root: $solutionRoot" -ForegroundColor Gray
Write-Host "  Build Output:  $buildDir" -ForegroundColor Gray

# ============================================================================
# STEP 2: Clean Build Directory (Optional)
# ============================================================================
# Remove existing build artifacts to ensure a clean build.

if ($Clean -and (Test-Path $buildDir)) {
    Write-Host "`n  Cleaning build directory..." -ForegroundColor Yellow
    
    try {
        Remove-Item -Recurse -Force $buildDir -ErrorAction Stop
        Write-Host "  ✓ Build directory cleaned" -ForegroundColor Green
    }
    catch {
        Write-Host "  WARNING: Failed to clean build directory: $_" -ForegroundColor Yellow
        Write-Host "  Continuing with build..." -ForegroundColor Gray
    }
}
elseif (-not $Clean) {
    Write-Host "`n  Skipping clean (incremental build)" -ForegroundColor Gray
}

# ============================================================================
# STEP 3: Build Solution
# ============================================================================
# Build the solution in the specified configuration(s).

# Helper function to build a specific configuration
function Build-Configuration {
    param(
        [string]$Config,
        [string]$SolutionPath,
        [string]$VerbosityLevel,
        [bool]$SkipPackageRestore
    )
    
    Write-Host "`n[Building $Config Configuration]" -ForegroundColor Cyan
    
    # Build command arguments
    $buildArgs = @(
        'build',
        '--configuration', $Config,
        '--verbosity', $VerbosityLevel
    )
    
    # Add no-restore flag if packages are already restored
    if ($SkipPackageRestore) {
        $buildArgs += '--no-restore'
        Write-Host "  Skipping package restore" -ForegroundColor Gray
    }
    
    # Execute build
    Push-Location $SolutionPath
    try {
        Write-Host "  Running: dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
        & dotnet $buildArgs
        
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
        
        Write-Host "`n  ✓ $Config build completed successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "`n  ✗ $Config build FAILED: $_" -ForegroundColor Red
        return $false
    }
    finally {
        Pop-Location
    }
}

# Determine which configurations to build
$configsToBuild = @()
if ($Configuration -eq 'Both') {
    $configsToBuild = @('Release', 'Debug')
    Write-Host "`n  Building both Release and Debug configurations" -ForegroundColor Cyan
}
else {
    $configsToBuild = @($Configuration)
    Write-Host "`n  Building $Configuration configuration only" -ForegroundColor Cyan
}

# Build each configuration
$buildResults = @{}
$allSucceeded = $true

foreach ($config in $configsToBuild) {
    $success = Build-Configuration `
        -Config $config `
        -SolutionPath $solutionRoot `
        -VerbosityLevel $Verbosity `
        -SkipPackageRestore $SkipRestore.IsPresent
    
    $buildResults[$config] = $success
    $allSucceeded = $allSucceeded -and $success
}

# ============================================================================
# STEP 4: Summary
# ============================================================================
# Display build summary and exit with appropriate code.

Write-Host "`n=== Build Summary ===" -ForegroundColor Cyan

foreach ($config in $buildResults.Keys) {
    $status = if ($buildResults[$config]) { "✓ PASSED" } else { "✗ FAILED" }
    $color = if ($buildResults[$config]) { "Green" } else { "Red" }
    Write-Host "  $config`: $status" -ForegroundColor $color
}

if ($allSucceeded) {
    Write-Host "`n✓ All builds completed successfully!`n" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "`n✗ One or more builds FAILED. Check output above for details.`n" -ForegroundColor Red
    exit 1
}
