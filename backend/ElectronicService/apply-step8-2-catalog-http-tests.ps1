$ErrorActionPreference = "Stop"

$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
$solutionRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Read-NormalizedText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return [System.IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
}

function Write-NormalizedText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $windowsContent = $Content.Replace(
        "`n",
        [Environment]::NewLine)

    [System.IO.File]::WriteAllText(
        $Path,
        $windowsContent,
        $script:utf8WithoutBom)
}

$fixturePath = Join-Path `
    $solutionRoot `
    "tests\ElectronicService.Web.IntegrationTests\Fixtures\WebIntegrationFixture.cs"

if (-not (Test-Path $fixturePath)) {
    throw "Fixture was not found: $fixturePath"
}

$content = Read-NormalizedText -Path $fixturePath

$propertyText = @'
    public IServiceProvider Services => Application.Services;

'@

if ($content.IndexOf(
        "public IServiceProvider Services => Application.Services;",
        [StringComparison]::Ordinal) -ge 0) {
    Write-Host "[SKIP] Fixture Services property is already present."
}
else {
    $marker = "    public HttpClient CreateClient()"

    $index = $content.IndexOf(
        $marker,
        [StringComparison]::Ordinal)

    if ($index -lt 0) {
        throw "Cannot update WebIntegrationFixture.cs. CreateClient marker was not found."
    }

    $content =
        $content.Substring(0, $index) +
        $propertyText +
        $content.Substring($index)

    Write-NormalizedText `
        -Path $fixturePath `
        -Content $content

    Write-Host "[OK] Expose application Services from WebIntegrationFixture"
}

Write-Host ""
Write-Host "Stage 8.2 patch was applied successfully."
Write-Host "Next command:"
Write-Host "dotnet build ElectronicService.slnx"
