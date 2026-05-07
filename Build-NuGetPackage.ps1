#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds and packages the ChartApp WPF library as a NuGet package.

.DESCRIPTION
    This script builds the ChartApp project in Release mode and creates a NuGet package.
    The package is output to the ./nupkg directory.

.PARAMETER Version
    Version number for the package (e.g., "1.0.0"). Defaults to "1.0.0".

.PARAMETER OutputPath
    Output directory for the NuGet package. Defaults to "./nupkg".

.PARAMETER PublishApiKey
    API key required for publishing to NuGet.org (free to obtain at nuget.org). 
    If provided, the package will be pushed after creation.

.EXAMPLE
    .\Build-NuGetPackage.ps1 -Version "1.0.0"

.EXAMPLE
    .\Build-NuGetPackage.ps1 -Version "1.0.1" -OutputPath "C:\Packages" -PublishApiKey "your-api-key"
#>

param(
    [string]$Version = "1.0.0",
    [string]$OutputPath = "./nupkg",
    [string]$PublishApiKey
)

$ErrorActionPreference = "Stop"

Write-Host "ChartApp NuGet Package Builder" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Output: $OutputPath"
Write-Host ""

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
    Write-Host "Created output directory: $OutputPath" -ForegroundColor Green
}

# Paths
$projectPath = "ChartApp\ChartApp.csproj"
$nugetExe = "nuget.exe"

# Check if project exists
if (-not (Test-Path $projectPath)) {
    Write-Error "Project file not found: $projectPath"
    exit 1
}

Write-Host "Step 1: Restoring NuGet packages..."
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to restore packages"
    exit 1
}

Write-Host "Step 2: Building project (Release mode)..."
dotnet build $projectPath -c Release -p:Version=$Version
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build project"
    exit 1
}

Write-Host "Step 3: Packing NuGet package..."
dotnet pack (
    $projectPath
    -c Release
    -p:Version=$Version
    -o $OutputPath
    --include-symbols
    --include-source
)
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to pack NuGet package"
    exit 1
}

$packageFile = Get-ChildItem "$OutputPath\ChartApp.WPF.$Version.nupkg" -ErrorAction SilentlyContinue
if ($packageFile) {
    Write-Host ""
    Write-Host "✓ NuGet package created successfully!" -ForegroundColor Green
    Write-Host "  Package: $($packageFile.FullName)"
    Write-Host "  Size: $([math]::Round($packageFile.Length / 1MB, 2)) MB"
    
    if ($PublishApiKey) {
        Write-Host ""
        Write-Host "Step 4: Publishing to NuGet.org..."
        dotnet nuget push "$($packageFile.FullName)" `
            -k $PublishApiKey `
            -s https://api.nuget.org/v3/index.json
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Package published successfully!" -ForegroundColor Green
        }
        else {
            Write-Host "⚠ Publishing failed. Check the error message above." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host ""
        Write-Host "To publish this package to NuGet.org (requires free API key from nuget.org), run:"
        Write-Host "dotnet nuget push `"$($packageFile.FullName)`" -k your-api-key -s https://api.nuget.org/v3/index.json"
    }
}
else {
    Write-Error "Package file not found after packing"
    exit 1
}

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
