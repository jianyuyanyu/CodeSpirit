# CodeSpirit Central Package Management Guide

> **Date**: 2026-01-20  
> **Version**: CodeSpirit v2.0+

---

## Overview

CodeSpirit has fully adopted **Central Package Management** (CPM) to manage all NuGet package versions across the solution through a single `Directory.Packages.props` file.

### Key Features

✅ **Unified Version Management**: All package versions defined in one place  
✅ **Avoid Version Conflicts**: Ensures consistent package versions across projects  
✅ **Simplified Maintenance**: Update package versions by modifying a single file  
✅ **Reduce Redundancy**: Automatically leverage transitive dependencies  
✅ **Improved Readability**: Cleaner project files with only direct dependencies

---

## What is Central Package Management

Central Package Management is a NuGet feature that allows defining all package versions at the solution level in `Directory.Packages.props`, while project files (`.csproj`) only reference package names without versions.

### Traditional vs Centralized

#### ❌ Traditional (Decentralized)

```xml
<!-- ProjectA.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- ProjectB.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.2" /> <!-- Conflict! -->
```

#### ✅ Centralized

```xml
<!-- Directory.Packages.props (Solution Root) -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>

<!-- ProjectA.csproj -->
<PackageReference Include="Newtonsoft.Json" /> <!-- No version needed -->

<!-- ProjectB.csproj -->
<PackageReference Include="Newtonsoft.Json" /> <!-- Uses 13.0.3 automatically -->
```

---

## File Structure

### Location

```
CodeSpirit/
├── Directory.Packages.props  ← Solution root
├── CodeSpirit.sln
├── Src/
└── Tests/
```

### Structure

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- Aspire Packages (Unified to 13.1.0) -->
    <PackageVersion Include="Aspire.AppHost.Sdk" Version="13.1.0" />
    
    <!-- Entity Framework Core (Unified to 9.0.9) -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.9" />
    
    <!-- Microsoft Extensions (Unified to 10.0.2) -->
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.2" />
    
    <!-- Other packages... -->
  </ItemGroup>
</Project>
```

---

## Version Strategy

### Unified Major Dependencies

| Package Family | Version | Description |
|----------------|---------|-------------|
| Aspire.* | 13.1.0 | Aspire platform core packages |
| Microsoft.EntityFrameworkCore.* | 9.0.9 | EF Core data access |
| Microsoft.Extensions.* | 10.0.2 | .NET extension libraries |
| Microsoft.AspNetCore.* | 10.0.1 | ASP.NET Core framework |
| OpenTelemetry.* | 1.14.0 | Telemetry and monitoring |

### Compatibility Notes

⚠️ **Important**: Aspire 13.1.0 EF Core integration packages require EF Core 10.0.1+, but Pomelo.EntityFrameworkCore.MySql 9.0.0 doesn't support EF Core 10.

**Solution**: Use standard EF Core packages instead of Aspire integration packages, and let Pomelo pass through `CodeSpirit.Shared`.

---

## How to Use

### Adding a New Package

#### Step 1: Define version in Directory.Packages.props

```xml
<ItemGroup>
  <PackageVersion Include="Serilog.AspNetCore" Version="8.0.2" />
</ItemGroup>
```

#### Step 2: Reference in project file (no version)

```xml
<ItemGroup>
  <PackageReference Include="Serilog.AspNetCore" />
</ItemGroup>
```

### Upgrading Packages

Simply modify `Directory.Packages.props`:

```xml
<!-- Upgrade from 8.0.1 to 8.0.2 -->
<PackageVersion Include="Serilog.AspNetCore" Version="8.0.2" />
```

All projects referencing the package will automatically use the new version on next build.

---

## Transitive Dependencies Optimization

### What are Transitive Dependencies

When Project A references Project B, and Project B references NuGet Package C, Project A automatically gets Package C.

```
ProjectA → ProjectB → PackageC
```

### CodeSpirit Transitive Dependencies

```
ApiService (e.g., ConfigCenter)
  └─> CodeSpirit.Shared
        ├─> Newtonsoft.Json
        ├─> LinqKit.Core
        ├─> Pomelo.EntityFrameworkCore.MySql
        └─> AutoMapper
```

Therefore, all projects referencing `CodeSpirit.Shared` don't need to explicitly reference these base packages.

### Before vs After Optimization

#### ❌ Before (Redundant References)

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" />
  <PackageReference Include="LinqKit.Core" />
  <!-- Already available through CodeSpirit.Shared -->
</ItemGroup>
```

#### ✅ After (Leveraging Transitive Dependencies)

```xml
<ItemGroup>
  <!-- Newtonsoft.Json provided by CodeSpirit.Shared -->
  <!-- LinqKit.Core provided by CodeSpirit.Shared -->
  
  <!-- Only keep direct dependencies -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
</ItemGroup>
```

---

## Common Questions

### Q1: How to check which projects use a specific package?

```powershell
# List all package references
dotnet list package

# List with transitive dependencies
dotnet list package --include-transitive

# Find specific package
dotnet list package | Select-String "Newtonsoft.Json"
```

### Q2: How to check for version conflicts?

```powershell
# Show outdated packages
dotnet list package --outdated

# Show vulnerable packages
dotnet list package --vulnerable
```

### Q3: Can I override versions in project files?

**Not recommended**. The purpose of centralized management is version unification. If you really need a special version, it's better to upgrade the global version in `Directory.Packages.props` or add comments explaining why.

---

## Migration Guide

### Migrating from Decentralized to Centralized

#### Step 1: Create Directory.Packages.props

Create `Directory.Packages.props` in solution root:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Package versions will be added in Step 2 -->
  </ItemGroup>
</Project>
```

#### Step 2: Collect All Package Versions

Use PowerShell to collect package references:

```powershell
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object {
    [xml]$csproj = Get-Content $_.FullName
    $csproj.Project.ItemGroup.PackageReference | Where-Object { $_.Version } | ForEach-Object {
        [PSCustomObject]@{
            Package = $_.Include
            Version = $_.Version
            Project = $_.BaseName
        }
    }
} | Sort-Object Package, Version | Format-Table -AutoSize
```

#### Step 3: Unify Versions

1. Identify version conflicts (different versions of same package)
2. Choose the latest stable version
3. Add to `Directory.Packages.props`

#### Step 4: Modify Project Files

Remove `Version` attribute from all `PackageReference`:

```xml
<!-- Before -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- After -->
<PackageReference Include="Newtonsoft.Json" />
```

#### Step 5: Verify Build

```powershell
dotnet clean
dotnet build
dotnet list package --include-transitive
```

#### Step 6: Optimize Transitive Dependencies

1. Identify packages available through transitive dependencies
2. Remove redundant references
3. Add comments explaining the source

---

## Best Practices

1. **Regular Dependency Updates**: Check and update package versions monthly
2. **Maintain Version Consistency**: Use same versions for package families
3. **Follow Semantic Versioning**: Major.Minor.Patch
4. **Add Meaningful Comments**: Explain special versions or constraints
5. **Use Tools**: `dotnet-outdated`, NuGet Package Explorer

---

## Summary

Central Package Management provides CodeSpirit with:

✅ **Unified Version Management**: Avoid version conflicts  
✅ **Simplified Maintenance**: Modify once, apply everywhere  
✅ **Clear Dependencies**: Easy to understand and review  
✅ **Optimized Project Files**: Cleaner and more readable

---

## References

- [NuGet Central Package Management Official Docs](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- [CodeSpirit Package Management Specification](mdc:.cursor/rules/package-management.mdc)
- [.NET Dependency Management Best Practices](https://learn.microsoft.com/dotnet/core/tools/dependencies)

---

**Last Updated**: 2026-01-20  