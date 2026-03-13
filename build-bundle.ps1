# Build and Bundle Script for QuickNotes Extension
# Run this from: C:\Users\Lenovo\Desktop\QuickNotes\QuickNotes\

param([string]$Version = "0.2.1.0")

$ErrorActionPreference = "Stop"

Write-Host "=== Building QuickNotes Extension v$Version ===" -ForegroundColor Green

# Step 1: Build x64
Write-Host "`n[1/4] Building x64..." -ForegroundColor Yellow
dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=x64 -p:AppxPackageDir="AppPackages\x64\"
if ($LASTEXITCODE -ne 0) { throw "x64 build failed" }

# Step 2: Build ARM64
Write-Host "`n[2/4] Building ARM64..." -ForegroundColor Yellow
dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=ARM64 -p:AppxPackageDir="AppPackages\ARM64\"
if ($LASTEXITCODE -ne 0) { throw "ARM64 build failed" }

# Step 3: Create bundle mapping file
Write-Host "`n[3/4] Creating bundle mapping..." -ForegroundColor Yellow
$mappingContent = @"
[Files]
"QuickNotes\bin\x64\Release\net9.0-windows10.0.26100.0\win-x64\QuickNotes_$($Version)_x64.msix" "QuickNotes_$($Version)_x64.msix"
"QuickNotes\bin\ARM64\Release\net9.0-windows10.0.26100.0\win-arm64\QuickNotes_$($Version)_arm64.msix" "QuickNotes_$($Version)_arm64.msix"
"@
$mappingContent | Out-File -FilePath "bundle_mapping.txt" -Encoding ASCII

# Step 4: Create bundle
Write-Host "`n[4/4] Creating MSIX bundle..." -ForegroundColor Yellow
$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"
& $makeappx bundle /f bundle_mapping.txt /p "QuickNotes_$($Version)_Bundle.msixbundle"
if ($LASTEXITCODE -ne 0) { throw "Bundle creation failed" }

Write-Host "`n=== Build Complete! ===" -ForegroundColor Green
Write-Host "Output: QuickNotes_$($Version)_Bundle.msixbundle" -ForegroundColor Cyan
Write-Host "Upload this file to Microsoft Store Partner Center." -ForegroundColor White
