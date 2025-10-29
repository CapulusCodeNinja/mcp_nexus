# ---------------------------
# Paths
# ---------------------------
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path
$buildtDir = Join-Path $solutionRoot 'build'

# ---------------------------
# Clean old build
# ---------------------------
if (Test-Path $buildtDir) {
    Remove-Item -Recurse -Force $buildtDir
}

# ---------------------------
# Build projects in Release configuration
# ---------------------------
Push-Location $solutionRoot
try {
    dotnet build --configuration Release
} finally {
    Pop-Location
}

if ($LastExitCode -ne 0) {
    Write-Error "Failed to build projects in Release configuration"
    Exit 1
}

# ---------------------------
# Build projects in Debug configuration
# ---------------------------
Push-Location $solutionRoot
try {
    dotnet build --configuration Debug
} finally {
    Pop-Location
}

if ($LastExitCode -ne 0) {
    Write-Error "Failed to build projects in Debug configuration"
    Exit 1
}
