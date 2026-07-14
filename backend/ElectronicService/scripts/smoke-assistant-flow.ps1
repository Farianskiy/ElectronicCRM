param(
    [string]$ApiBaseUrl = "http://localhost:8080",
    [string]$PgContainer = "postgres",
    [string]$PgUser = "electronic_user",
    [string]$PgPassword = "electronic_password",
    [string]$PgDatabase = "electronic_service_db"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)

    Write-Host ""
    Write-Host "==== $Message ====" -ForegroundColor Cyan
}

function Write-Ok {
    param([string]$Message)

    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)

    Write-Host "[FAIL] $Message" -ForegroundColor Red
    throw $Message
}

function New-StringFromCodePoints {
    param([int[]]$CodePoints)

    $chars = $CodePoints | ForEach-Object { [char]$_ }

    return [string]::Concat($chars)
}

function New-ChentWord {
    return New-StringFromCodePoints @(0x0447, 0x0435, 0x043D, 0x0442)
}

function New-ChentAssistantMessage {
    return New-StringFromCodePoints @(
        0x043D, 0x0430, 0x0439, 0x0434, 0x0438,
        0x0020,
        0x0430, 0x0432, 0x0442, 0x043E, 0x043C, 0x0430, 0x0442,
        0x0020,
        0x0447, 0x0435, 0x043D, 0x0442,
        0x0020,
        0x0031, 0x043F,
        0x0020,
        0x0031, 0x0036, 0x0430
    )
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [string]$Token = $null
    )

    $headers = @{}

    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }

    try {
        if ($Body -ne $null) {
            $json = $Body | ConvertTo-Json -Depth 20
            $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($json)

            return Invoke-RestMethod `
                -Method $Method `
                -Uri $Url `
                -Headers $headers `
                -ContentType "application/json; charset=utf-8" `
                -Body $bodyBytes
        }

        return Invoke-RestMethod `
            -Method $Method `
            -Uri $Url `
            -Headers $headers
    }
    catch {
        Write-Host ""
        Write-Host "[HTTP ERROR] $Method $Url" -ForegroundColor Red

        $response = $_.Exception.Response

        if ($response -ne $null) {
            $stream = $response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
            $errorBody = $reader.ReadToEnd()

            if ($errorBody) {
                Write-Host "Response body:" -ForegroundColor Red
                Write-Host $errorBody -ForegroundColor Red
            }
        }

        throw
    }
}

function Try-Invoke-Api {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [string]$Token = $null
    )

    try {
        return Invoke-Api -Method $Method -Url $Url -Body $Body -Token $Token
    }
    catch {
        return $null
    }
}

function Ensure-Regular-User {
    Write-Step "Ensure regular user"

    $existingLogin = Try-Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/auth/login" `
        -Body @{
            email = "smoke.regular@test.local"
            password = "Regular12345!"
        }

    if ($existingLogin -ne $null) {
        Write-Ok "Regular user already exists"
        return
    }

    Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/users/regular" `
        -Body @{
            displayName = "Smoke Regular"
            email = "smoke.regular@test.local"
            password = "Regular12345!"
        } | Out-Null

    Write-Ok "Regular user created"
}

function Ensure-Technical-User {
    Write-Step "Ensure technical user"

    $existingLogin = Try-Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/auth/login" `
        -Body @{
            email = "smoke.tech@test.local"
            password = "Technical12345!"
        }

    if ($existingLogin -ne $null) {
        Write-Ok "Technical user already exists"
        return
    }

    Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/users/technical" `
        -Body @{
            displayName = "Smoke Technical"
            email = "smoke.tech@test.local"
            password = "Technical12345!"
        } | Out-Null

    Write-Ok "Technical user created"
}

function Login-User {
    param(
        [string]$Email,
        [string]$Password,
        [string]$ExpectedUserType
    )

    $loginResponse = Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/auth/login" `
        -Body @{
            email = $Email
            password = $Password
        }

    if (-not $loginResponse.accessToken) {
        Write-Fail "Login failed for $Email. accessToken is empty."
    }

    if ($loginResponse.userType -ne $ExpectedUserType) {
        Write-Fail "Expected user type $ExpectedUserType for $Email, but got $($loginResponse.userType)."
    }

    Write-Ok "$Email logged in as $($loginResponse.userType)"

    return $loginResponse.accessToken
}

function Reset-Assistant-Test-Data {
    Write-Step "Reset assistant test data"

    $scriptPath = Join-Path $PSScriptRoot "dev-reset-assistant-test-data.sql"

    if (-not (Test-Path $scriptPath)) {
        Write-Fail "Reset SQL file was not found: $scriptPath"
    }

    docker cp $scriptPath "${PgContainer}:/tmp/dev-reset-assistant-test-data.sql"

    docker exec `
        -i `
        -e PGPASSWORD=$PgPassword `
        $PgContainer `
        psql `
        -U $PgUser `
        -d $PgDatabase `
        -v ON_ERROR_STOP=1 `
        -f /tmp/dev-reset-assistant-test-data.sql | Out-Host

    Write-Ok "Assistant test data reset completed"
}

function Test-Health {
    Write-Step "Backend health"

    $health = Invoke-RestMethod -Method "GET" -Uri "$ApiBaseUrl/health"

    if ($health -ne "Healthy") {
        Write-Fail "Expected Healthy, but got $health"
    }

    Write-Ok "Backend health is Healthy"
}

function Test-Catalog {
    param([string]$Token)

    Write-Step "Catalog products list"

    $productsResponse = Invoke-Api `
        -Method "GET" `
        -Url "$ApiBaseUrl/api/catalog/products?page=1&pageSize=5" `
        -Token $Token

    if ($productsResponse.totalCount -le 0) {
        Write-Fail "Catalog returned 0 products. Import catalog data before running smoke test."
    }

    if ($productsResponse.items.Count -le 0) {
        Write-Fail "Catalog totalCount is greater than 0, but current page is empty."
    }

    $firstProduct = $productsResponse.items[0]

    Write-Ok "Catalog returned $($productsResponse.totalCount) products"
    Write-Ok "First product: $($firstProduct.name)"
    Write-Ok "First product article: $($firstProduct.article)"

    Write-Step "Product details"

    $details = Invoke-Api `
        -Method "GET" `
        -Url "$ApiBaseUrl/api/catalog/products/$($firstProduct.id)" `
        -Token $Token

    if (-not $details.id) {
        Write-Fail "Product details did not return product id."
    }

    Write-Ok "Product details loaded: $($details.name)"
    Write-Ok "Characteristics count: $($details.characteristics.Count)"

    Write-Step "Catalog search by first product article"

    $articleSearch = [System.Uri]::EscapeDataString($firstProduct.article)

    $searchResponse = Invoke-Api `
        -Method "GET" `
        -Url "$ApiBaseUrl/api/catalog/products?search=$articleSearch&page=1&pageSize=5" `
        -Token $Token

    if ($searchResponse.totalCount -le 0) {
        Write-Fail "Catalog search by article returned 0 products. Search endpoint may be broken."
    }

    Write-Ok "Catalog search by article returned $($searchResponse.totalCount) products"
}

function Test-Assistant-Dictionary-Flow {
    param(
        [string]$RegularToken,
        [string]$TechnicalToken
    )

    $unknownWord = New-ChentWord
    $assistantMessage = New-ChentAssistantMessage

    Write-Step "Assistant should ask clarification for '$unknownWord'"

    $assistantResponse = Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/catalog/assistant/ask" `
        -Token $RegularToken `
        -Body @{
            message = $assistantMessage
            onlyInStock = $false
            minimumScore = 70
            page = 1
            pageSize = 20
        }

    if ($assistantResponse.needsClarification -ne $true) {
        Write-Fail "Expected needsClarification = true. Maybe '$unknownWord' still exists in dictionary."
    }

    $clarification = $assistantResponse.parsedRequest.clarification

    if ($clarification -eq $null) {
        Write-Fail "Clarification is null."
    }

    Write-Ok "Clarification found: $($clarification.unknownPhrase) -> $($clarification.suggestedTargetValue)"
    
    if ($clarification.suggestedKind -ne "Manufacturer") {
        Write-Fail "Expected suggestedKind = Manufacturer, but got $($clarification.suggestedKind)."
    }

    if ($clarification.suggestedTargetValue -ne "CHINT") {
        Write-Fail "Expected suggestedTargetValue = CHINT, but got $($clarification.suggestedTargetValue)."
    }

    Write-Step "Regular user creates dictionary suggestion"

    $suggestionResponse = Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/catalog/assistant/dictionary-suggestions" `
        -Token $RegularToken `
        -Body @{
            originalMessage = $assistantMessage
            unknownPhrase = $clarification.unknownPhrase
            suggestedKind = $clarification.suggestedKind
            suggestedTargetCode = $clarification.suggestedTargetCode
            suggestedTargetValue = $clarification.suggestedTargetValue
            confidence = $clarification.confidence
        }

    if (-not $suggestionResponse.id) {
        Write-Fail "Suggestion was not created."
    }

    Write-Ok "Suggestion created: $($suggestionResponse.id)"

    Write-Step "Technical user loads pending suggestions"

    $pendingResponse = Invoke-Api `
        -Method "GET" `
        -Url "$ApiBaseUrl/api/catalog/assistant/dictionary-suggestions?status=Pending&page=1&pageSize=20" `
        -Token $TechnicalToken

    $createdSuggestion = $pendingResponse.items |
        Where-Object { $_.id -eq $suggestionResponse.id } |
        Select-Object -First 1

    if ($createdSuggestion -eq $null) {
        Write-Fail "Created suggestion was not found in Pending suggestions."
    }

    Write-Ok "Pending suggestion found"

    Write-Step "Technical user approves suggestion"

    Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/catalog/assistant/dictionary-suggestions/$($suggestionResponse.id)/approve" `
        -Token $TechnicalToken `
        -Body @{
            reviewComment = "Smoke test approval: test alias points to CHINT."
        } | Out-Null

    Write-Ok "Suggestion approved"

    Write-Step "Assistant should understand '$unknownWord' after approval"

    $assistantAfterApprove = Invoke-Api `
        -Method "POST" `
        -Url "$ApiBaseUrl/api/catalog/assistant/ask" `
        -Token $RegularToken `
        -Body @{
            message = $assistantMessage
            onlyInStock = $false
            minimumScore = 70
            page = 1
            pageSize = 20
        }

    if ($assistantAfterApprove.needsClarification -eq $true) {
        Write-Fail "Assistant still asks clarification after approval."
    }

    Write-Ok "Assistant understands '$unknownWord' after approval"

    if ($assistantAfterApprove.products.Count -le 0) {
        Write-Fail "Assistant understood '$unknownWord', but returned 0 products after approval. Dictionary flow works, but search result is not useful."
    }

    Write-Ok "Products returned: $($assistantAfterApprove.products.Count)"
    }

Write-Step "Smoke test started"
Write-Host "API: $ApiBaseUrl"

Test-Health

Reset-Assistant-Test-Data

Ensure-Regular-User
Ensure-Technical-User

$regularToken = Login-User `
    -Email "smoke.regular@test.local" `
    -Password "Regular12345!" `
    -ExpectedUserType "Regular"

$technicalToken = Login-User `
    -Email "smoke.tech@test.local" `
    -Password "Technical12345!" `
    -ExpectedUserType "Technical"

Test-Catalog -Token $regularToken

Test-Assistant-Dictionary-Flow `
    -RegularToken $regularToken `
    -TechnicalToken $technicalToken

Write-Step "Smoke test completed"
Write-Ok "All checks passed"