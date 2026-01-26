# CodeSpirit Login Tests Setup Script

Write-Host "============================================================"
Write-Host "CodeSpirit Login Tests Environment Setup"
Write-Host "============================================================"
Write-Host ""

# 1. Install dotnet-script
Write-Host "[1/3] Checking dotnet-script tool..."
$dotnetScriptInstalled = dotnet tool list -g | Select-String "dotnet-script"

if ($dotnetScriptInstalled) {
    Write-Host "    OK dotnet-script is installed"
} else {
    Write-Host "    Installing dotnet-script..."
    dotnet tool install -g dotnet-script
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    OK dotnet-script installed successfully"
    } else {
        Write-Host "    ERROR dotnet-script installation failed"
        exit 1
    }
}

Write-Host ""

# 2. Install Microsoft.Playwright.CLI
Write-Host "[2/3] Checking Playwright CLI tool..."
$playwrightInstalled = dotnet tool list -g | Select-String "Microsoft.Playwright.CLI"

if ($playwrightInstalled) {
    Write-Host "    OK Playwright CLI is installed"
} else {
    Write-Host "    Installing Playwright CLI..."
    dotnet tool install -g Microsoft.Playwright.CLI
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    OK Playwright CLI installed successfully"
    } else {
        Write-Host "    ERROR Playwright CLI installation failed"
        exit 1
    }
}

Write-Host ""

# 3. Install Chromium browser
Write-Host "[3/3] Installing Chromium browser..."
playwright install chromium

if ($LASTEXITCODE -eq 0) {
    Write-Host "    OK Chromium browser installed successfully"
} else {
    Write-Host "    ERROR Chromium browser installation failed"
    exit 1
}

Write-Host ""
Write-Host "============================================================"
Write-Host "Setup completed successfully!"
Write-Host "============================================================"
Write-Host ""
Write-Host "Tools installed:"
Write-Host "  - dotnet-script (C# script runner)"
Write-Host "  - Microsoft.Playwright.CLI (Browser automation)"
Write-Host "  - Chromium browser (v131.0.6778.33)"
Write-Host ""
Write-Host "Now you can run the login test scripts:"
Write-Host ""
Write-Host "System Admin Login:"
Write-Host "  dotnet script login-system.cs"
Write-Host "  dotnet script login-system.cs -- https://localhost:7120 systemadmin CodeSpirit@2025"
Write-Host ""
Write-Host "Tenant Admin Login:"
Write-Host "  dotnet script login-tenant.cs"
Write-Host "  dotnet script login-tenant.cs -- https://localhost:7120 default admin 123@Admin"
Write-Host ""
Write-Host "Note: Make sure Aspire application is running (aspire run)"
Write-Host ""
