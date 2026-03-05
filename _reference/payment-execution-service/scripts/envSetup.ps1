[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingEmptyCatchBlock', '')]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '')]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingInvokeExpression', '')]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '')]
param()

$requiredTools = @(
    @{
        Name = 'Docker Compose';
        GetVersionExpression = 'docker compose version 2> $null';
        VersionMatcher = '^Docker Compose version v(?<version>(\d+\.)+\d+)';
        MinVersion = [version]'2.1.1'
    },
    @{ Name = 'GNU Make'; GetVersionExpression = 'make --version' },
    @{
        Name = 'Docker';
        GetVersionExpression = 'docker --version 2> $null';
        VersionMatcher = '^Docker version (?<version>(\d+\.)+\d+)';
        MinVersion = [version]'19.03.5'
    },
    @{
        Name = '.NET 8 SDK';
        GetVersionExpression = 'dotnet --version';
        VersionMatcher = '(?<version>\d+\.\d+\.\d+)';
        MinVersion = [version]'8.0.100'
    },
    @{ Name = 'Bash'; GetVersionExpression = 'bash --version' }
)

function Test-Tool([string] $name, [string] $expression) {
    $output = ''
    try {
        $output = Invoke-Expression $expression
    }
    catch {}

    if ($output) {
        Write-Host "$name is correctly installed"
        return $true
    }
    Write-Host -ForegroundColor Red "$name is not installed"
    return $false
}

function Test-ToolWithVersion($tool) {
    $name = $tool.Name
    $expression = $tool.GetVersionExpression
    $versionMatcher = $tool.VersionMatcher
    $minVersion = $tool.MinVersion

    $output = ''
    try {
        $output = Invoke-Expression $expression
    }
    catch {}

    if ($output) {
        $hasCorrectVersion = $false
        if ($output -match $versionMatcher) {
            $installedVersion = [version]$matches.version
            $hasCorrectVersion = $installedVersion -ge $minVersion
        }
        if ($hasCorrectVersion) {
            Write-Host "$name version $installedVersion is correctly installed (min version = $minVersion)"
            return $true
        }
        Write-Host -ForegroundColor Red `
            "$name version $installedVersion is outdated (min version = $minVersion)"
        return $false
    }
    Write-Host -ForegroundColor Red "$name is not installed"
    return $false
}

function Install-GitHooks {
    Write-Host -ForegroundColor DarkGray "Setting up Git hooks..."
    git config core.hooksPath ./scripts/hooks
}

$isSetUp = $true

Write-Host -ForegroundColor DarkGray "Checking prerequisites..."

$requiredTools | ForEach-Object {
    $tool = $_
    if ($tool.MinVersion) {
        if (!(Test-ToolWithVersion $tool)) {
            [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignment', '')]
            $isSetUp = $false
        }
    }
    else {
        if (!(Test-Tool $tool.Name $tool.GetVersionExpression)) {
            $isSetUp = $false
        }
    }
}

Install-GitHooks

if (!$isSetUp) {
    Write-Host -ForegroundColor Red "Some prerequisites aren't installed correctly. Please check the docs/local-setup.md for guidance."
    Exit 1
}
else {
    Write-Host -ForegroundColor Green "Your local environment is correctly set up"
}
