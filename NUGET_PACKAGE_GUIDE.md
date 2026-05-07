# ChartApp NuGet Package - Build & Distribution Guide

## Overview

This guide provides instructions for building, testing, and distributing the ChartApp WPF library as a NuGet package.

## Prerequisites

- **.NET 10 SDK** - Download from https://dotnet.microsoft.com/download/dotnet/10.0
- **Visual Studio 2022** or **VS Code**
- **Git** (optional, for version control)
- **NuGet Account** (for publishing to nuget.org)

## Package Information

### Package Metadata
- **Package ID**: `ChartApp.WPF`
- **Author**: ChartApp Contributors
- **License**: MIT
- **Copyright**: © 2026 ChartApp Contributors. All rights reserved.
- **Target Framework**: `net10.0-windows`
- **Repository**: https://github.com/hiteshajmermodani-dot/ChartApp

*Note: Update the repository URL and copyright year in `ChartApp.csproj` before publishing.*

## Copyright and Release Notes Configuration

### Copyright Metadata

Add copyright information to `ChartApp\ChartApp.csproj`:

```xml
<PropertyGroup>
    <PackageCopyright>© 2026 ChartApp Contributors. All rights reserved.</PackageCopyright>
</PropertyGroup>
```

This will display on the NuGet.org package page under the "Legal" section.

### Release Notes

Release notes are displayed on the package's NuGet.org page and help users understand what changed in each version.

#### Option 1: Inline Release Notes (Recommended for small updates)

Add directly to `.csproj`:

```xml
<PropertyGroup>
    <PackageReleaseNotes>
        ## Version 1.0.1
        - Fixed Y-axis margin calculation for multi-axis scenarios
        - Improved zoom/pan performance with cached axis ranges
        - Updated documentation and API examples

        ## Version 1.0.0
        - Initial public release
        - Support for multiple chart types (Line, Scatter, Bubble, Box Plot, Histogram, 3D)
        - Multi-axis support with independent zoom/pan
        - Live data streaming capabilities
        - Interactive tracking and tooltips
    </PackageReleaseNotes>
</PropertyGroup>
```

#### Option 2: External Release Notes File (Recommended for larger projects)

Create `RELEASE_NOTES.md`:

```markdown
# Release Notes

## Version 1.0.1 (2026-01-15)

### Features
- Improved Y-axis label measurement for zoom states
- Enhanced margin calculation for stacked axes
- Optimized axis range caching

### Bug Fixes
- Fixed null reference exception in box plot mode
- Corrected X-axis grid line positioning
- Resolved theme brush application for secondary axes

### Performance
- 30% improvement in axis range computation
- Reduced memory allocations in label width measurement

---

## Version 1.0.0 (2026-01-01)

### Features
- Initial public release
- Full WPF charting library with 6+ chart types
- Multi-axis support
- Live data updates
- Interactive tooltips and tracking

### Supported Chart Types
- Line Plot
- Scatter Plot
- Bubble Plot
- Box Plot
- Histogram
- 3D Surface and Line charts

---
```

Reference the file in `.csproj`:

```xml
<PropertyGroup>
    <PackageReleaseNotesFile>RELEASE_NOTES.md</PackageReleaseNotesFile>
</PropertyGroup>
```

Also add the file to the package:

```xml
<ItemGroup>
    <None Include="RELEASE_NOTES.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

### Complete Metadata Example

Here's a complete `.csproj` PropertyGroup with all metadata:

```xml
<PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- NuGet Package Metadata -->
    <PackageId>ChartApp.WPF</PackageId>
    <Version>1.0.1</Version>
    <Title>ChartApp WPF Chart Control Library</Title>
    <Authors>ChartApp Contributors</Authors>
    <Description>A comprehensive WPF charting library supporting multiple chart types including Line, Scatter, Bubble, Box Plot, Histogram, and 3D charts. Features include zoom, pan, multi-axis support, live data streaming, annotations, and interactive tracking.</Description>
    <PackageProjectUrl>https://github.com/hiteshajmermodani-dot/ChartApp</PackageProjectUrl>
    <RepositoryUrl>https://github.com/hiteshajmermodani-dot/ChartApp</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>true</PackageRequireLicenseAcceptance>
    <PackageCopyright>© 2026 ChartApp Contributors. All rights reserved.</PackageCopyright>
    <PackageReadmeFile>NUGET_README.md</PackageReadmeFile>
    <PackageReleaseNotesFile>RELEASE_NOTES.md</PackageReleaseNotesFile>
    <PackageTags>wpf;chart;charting;visualization;scatter;line;bubble;3d;plotting</PackageTags>

    <!-- Symbol and Source Package -->
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
</PropertyGroup>
```

### Including Files in Package

Update your `.csproj` ItemGroup to include README, Release Notes, and License:

```xml
<ItemGroup>
    <None Include="NUGET_README.md" Pack="true" PackagePath="\" />
    <None Include="RELEASE_NOTES.md" Pack="true" PackagePath="\" />
    <None Include="LICENSE" Pack="true" PackagePath="\" />
</ItemGroup>
```

## Building the Package

### Method 1: Using PowerShell Script (Recommended)

The easiest way to build and pack the NuGet package:

```powershell
# Navigate to workspace root
cd C:\Workspace\ChartApp\

# Run the build script with default version
.\Build-NuGetPackage.ps1

# Or specify a custom version
.\Build-NuGetPackage.ps1 -Version "1.0.1" -OutputPath "C:\Packages"

# To publish immediately (requires API key)
.\Build-NuGetPackage.ps1 -Version "1.0.0" -PublishApiKey "oy2xxxxx"
```

**What the script does:**
1. Restores NuGet dependencies
2. Builds the project in Release mode
3. Creates a `.nupkg` package file
4. (Optional) Publishes to NuGet.org

### Method 2: Using dotnet CLI

```powershell
# Build the project
dotnet build ChartApp\ChartApp.csproj -c Release -p:Version=1.0.0

# Create the NuGet package
dotnet pack ChartApp\ChartApp.csproj `
    -c Release `
    -p:Version=1.0.0 `
    -o ./nupkg `
    --include-symbols
```

### Method 3: Using Visual Studio

1. Open `ChartApp.sln` in Visual Studio
2. Right-click **ChartApp** project → **Properties**
3. In the **Package** tab, configure:
   - Package ID: `ChartApp.WPF`
   - Version: `1.0.0`
   - Title: `ChartApp WPF Chart Control Library`
   - Authors: `ChartApp Contributors`
   - Description: *(already configured in .csproj)*
4. Right-click **ChartApp** → **Pack**
5. Output will be in `bin\Release\net10.0-windows\`

## Package Output

After building, the package files will be in the `./nupkg/` directory:

```
nupkg/
├── ChartApp.WPF.1.0.0.nupkg          # Main package (DLL + resources)
├── ChartApp.WPF.1.0.0.snupkg         # Symbol package (PDBs)
└── ChartApp.WPF.1.0.0.symbols.nupkg  # Alternative symbol package
```

### Package Contents

The `.nupkg` file includes:
- `lib/net10.0-windows/ChartApp.dll` - Main library assembly
- `lib/net10.0-windows/ChartApp.pdb` - Debug symbols
- `lib/net10.0-windows/ChartApp.resources/` - XAML themes and resources
- `README.md` - Package documentation (from NUGET_README.md)
- `LICENSE` - MIT license text

## Testing the Package Locally

Before publishing, test the package in a local NuGet feed:

### Step 1: Create a local NuGet source

```powershell
# Create a folder for local packages
mkdir C:\LocalNuGetFeed

# Register it as a NuGet source
dotnet nuget add source `
    -n LocalFeed `
    C:\LocalNuGetFeed
```

### Step 2: Copy package to local feed

```powershell
copy .\nupkg\ChartApp.WPF.1.0.0.nupkg C:\LocalNuGetFeed\
```

### Step 3: Create a test project

```powershell
# Create a test WPF app
dotnet new wpf -n TestChartApp
cd TestChartApp

# Add reference to local package
dotnet add package ChartApp.WPF `
    -s C:\LocalNuGetFeed `
    --version 1.0.0

# Verify it resolves correctly
dotnet restore --verbose
```

### Step 4: Verify functionality

Update `MainWindow.xaml`:

```xaml
<Window x:Class="TestChartApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:chart="clr-namespace:ChartApp.Controls;assembly=ChartApp">
    <Grid>
        <chart:ChartControl x:Name="MyChart" ChartType="LinePlot" />
    </Grid>
</Window>
```

Update `MainWindow.xaml.cs`:

```csharp
using ChartApp.Models;
using System.Collections.ObjectModel;
using System.Windows.Media;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Load sample data
        MyChart.Series = new ObservableCollection<DataSeries>
        {
            new DataSeries
            {
                Name = "Test Series",
                YValues = new List<double> { 1, 2, 3, 4, 5 },
                Stroke = Brushes.Blue,
                Thickness = 1.5,
                YAxisId = "Y1",
                XAxisId = "X1"
            }
        };
    }
}
```

Build and run:
```powershell
dotnet run
```

## Publishing to NuGet.org

### Prerequisites
1. Register a free account at https://www.nuget.org/users/account/LogOn
2. Generate an API key: https://www.nuget.org/account/ApiKeys

### Publishing Steps

#### Option 1: Using the PowerShell script

```powershell
.\Build-NuGetPackage.ps1 -Version "1.0.0" -PublishApiKey "oy2xxxxxxxxxxxxxxxxxxxxxxxx"
```

#### Option 2: Manual publishing

```powershell
dotnet nuget push .\nupkg\ChartApp.WPF.1.0.0.nupkg `
    -k oy2xxxxxxxxxxxxxxxxxxxxxxxx `
    -s https://api.nuget.org/v3/index.json
```

#### Option 3: Using NuGet CLI

```powershell
nuget push .\nupkg\ChartApp.WPF.1.0.0.nupkg -ApiKey oy2xxxxxxxxxxxxxxxxxxxxxxxx
```

### Verification

After publishing (may take 5-15 minutes), verify the package:

1. Visit: https://www.nuget.org/packages/ChartApp.WPF/
2. Search NuGet Package Manager in Visual Studio:
   ```
   Install-Package ChartApp.WPF
   ```
3. Test via .NET CLI:
   ```powershell
   dotnet package search ChartApp.WPF
   ```

## Version Management

### Semantic Versioning

ChartApp uses **Semantic Versioning (SemVer)**: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (e.g., API redesign)
- **MINOR**: New features (backwards compatible)
- **PATCH**: Bug fixes

**Examples:**
- `1.0.0` - Initial release
- `1.1.0` - New chart type added
- `1.1.1` - Bug fix
- `2.0.0` - Major API changes

### Updating Version

Edit `ChartApp\ChartApp.csproj`:

```xml
<Version>1.0.1</Version>
```

Then rebuild and repack.

## Documentation

### README in Package

The `NUGET_README.md` file is automatically included in the NuGet package and displayed on nuget.org.

### Update after changes:

1. Edit `ChartApp\NUGET_README.md`
2. Commit and push to repository
3. Rebuild and publish with new version number

## Release Checklist

Before publishing a new version:

- [ ] Update version in `ChartApp.csproj`
- [ ] Update `NUGET_README.md` with new features/usage examples
- [ ] Update `RELEASE_NOTES.md` with changelog for this version
- [ ] Verify `PackageCopyright` is set to current year
- [ ] Verify `PackageReleaseNotesFile` references correct file
- [ ] Verify README and Release Notes files are included in `.csproj` ItemGroup
- [ ] Run unit tests (if available)
- [ ] Test package locally with sample app
- [ ] Verify all NuGet metadata is correct in package info
- [ ] Build in Release mode: `dotnet build -c Release`
- [ ] Pack package: `dotnet pack -c Release`
- [ ] Verify `.nupkg` contains README, Release Notes, and License
- [ ] Tag release in Git: `git tag -a v1.0.1 -m "Release version 1.0.1"`
- [ ] Push to repository: `git push origin main --tags`
- [ ] Publish to NuGet.org
- [ ] Verify package on nuget.org (copyright, release notes, readme display)
- [ ] Update GitHub releases page with release notes

## Troubleshooting

### Package not found in Visual Studio

**Problem**: Package doesn't appear in NuGet Package Manager

**Solutions**:
1. Clear NuGet cache: `dotnet nuget locals all --clear`
2. Wait 15 minutes for nuget.org indexing
3. Manually refresh in Visual Studio: **Tools** → **Options** → **NuGet Package Manager** → Clear cache
4. Check API key permissions on nuget.org

### Build errors with .NET 10

**Problem**: Compilation errors related to Windows API

**Solution**:
- Ensure `<UseWPF>true</UseWPF>` is in `.csproj`
- Update to latest .NET 10 SDK: `dotnet sdk upgrade`

### Symbol package issues

**Problem**: Debugging symbols not available

**Solution**:
- Ensure `<IncludeSymbols>true</IncludeSymbols>` in `.csproj`
- Rebuild in Release mode: `dotnet build -c Release`
- Check that `.pdb` files are in bin/Release output

## Advanced: GitHub Workflows

For automated NuGet packaging on each release, create `.github/workflows/nuget-publish.yml`:

```yaml
name: Publish NuGet Package

on:
  push:
    tags:
      - 'v*'

jobs:
  publish:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - run: dotnet pack ChartApp/ChartApp.csproj -c Release -o ./nupkg
      - run: dotnet nuget push ./nupkg/*.nupkg -k ${{ secrets.NUGET_API_KEY }} -s https://api.nuget.org/v3/index.json
```

## Support

For issues with packaging or distribution:
- Check NuGet documentation: https://docs.microsoft.com/nuget/
- Review .csproj metadata
- Consult GitHub repository issues

---

**Last Updated**: 2026
**ChartApp Version**: 1.0.0+
