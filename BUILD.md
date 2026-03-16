# Build Instructions for QuickNotes Extension

## Prerequisites
- .NET 9 SDK
- Windows App SDK
- Windows SDK (with makeappx.exe)

## Step-by-Step Build Process

### Step 1: Build x64 Package
```powershell
cd QuickNotes\QuickNotes
dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=x64 -p:AppxPackageDir="AppPackages\x64\"
```

### Step 2: Build ARM64 Package
```powershell
dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=ARM64 -p:AppxPackageDir="AppPackages\ARM64\"
```

### Step 3: Create Bundle Mapping File
Create `bundle_mapping.txt` in `QuickNotes\QuickNotes\` with the following content:

```
[Files]
"AppPackages\x64\QuickNotes_0.0.1.0_Test\QuickNotes_0.0.1.0_x64.msix" "QuickNotes_0.0.1.0_x64.msix"
"AppPackages\ARM64\QuickNotes_0.0.1.0_Test\QuickNotes_0.0.1.0_arm64.msix" "QuickNotes_0.0.1.0_arm64.msix"
```

### Step 4: Create MSIX Bundle
```powershell
& "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe" bundle /f bundle_mapping.txt /p QuickNotes_0.0.1.0_Bundle.msixbundle
```

> **Note:** If the Windows SDK version is different, adjust the path accordingly. You can find `makeappx.exe` at:
> `C:\Program Files (x86)\Windows Kits\10\bin\[SDK_VERSION]\x64\makeappx.exe`

### Step 5: Verify the Bundle
```powershell
dir QuickNotes_0.0.1.0_Bundle.msixbundle
```

## Output Files

After successful build, you should have:

```
QuickNotes\QuickNotes\
├── AppPackages\
│   ├── x64\QuickNotes_0.0.1.0_Test\
│   │   └── QuickNotes_0.0.1.0_x64.msix
│   └── ARM64\QuickNotes_0.0.1.0_Test\
│       └── QuickNotes_0.0.1.0_arm64.msix
├── bundle_mapping.txt
└── QuickNotes_0.0.1.0_Bundle.msixbundle  ← Upload this to Microsoft Store
```

## Microsoft Store Submission

Upload `QuickNotes_0.0.1.0_Bundle.msixbundle` to Partner Center in the **Packages** section.

---

## One-Line Build Script (Optional)

Save as `build-for-store.ps1`:

```powershell
param([string]$Version = "0.0.1.0")

$ErrorActionPreference = "Stop"

Write-Host "Building QuickNotes Extension v$Version..." -ForegroundColor Green

# Build x64
Write-Host "Building x64..." -ForegroundColor Yellow
dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=x64 -p:AppxPackageDir="AppPackages\x64\"
if ($LASTEXITCODE -ne 0) { throw "x64 build failed" }

# Build ARM64
Write-Host "Building ARM64..." -ForegroundColor Yellow
dotnet build --configuration Release -p:GenerateAppxPackageOnBuild=true -p:Platform=ARM64 -p:AppxPackageDir="AppPackages\ARM64\"
if ($LASTEXITCODE -ne 0) { throw "ARM64 build failed" }

# Create bundle
Write-Host "Creating bundle..." -ForegroundColor Yellow
$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\makeappx.exe"
& $makeappx bundle /f bundle_mapping.txt /p "QuickNotes_$($Version)_Bundle.msixbundle"
if ($LASTEXITCODE -ne 0) { throw "Bundle creation failed" }

Write-Host "Build complete! Upload QuickNotes_$($Version)_Bundle.msixbundle to Microsoft Store." -ForegroundColor Green
```

Run with:
```powershell
.\build-for-store.ps1 -Version "0.0.1.0"
```
