# test-api.ps1 - Final Working Version
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$AuthUrl = "http://localhost:5001",
    [string]$FileUrl = "http://localhost:5002",
    [string]$Username = "testuser",
    [string]$Password = "password123"
)

Write-Host "🌳 TreeDrive API Test" -ForegroundColor Cyan
Write-Host "====================" -ForegroundColor Cyan
Write-Host ""

$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

# Helper function to make JSON requests
function Invoke-JsonRequest {
    param($Url, $Method = "Get", $Body = $null)
    
    try {
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Body $jsonBody -ContentType "application/json" -TimeoutSec 10
            return $response
        } else {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -TimeoutSec 10
            return $response
        }
    } catch {
        Write-Host "    ❌ Error: $($_.Exception.Message)" -ForegroundColor $Red
        if ($_.Exception.Response) {
            Write-Host "    StatusCode: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor $Yellow
        }
        return $null
    }
}

# 1. Health Checks
Write-Host "📊 Health Checks:" -ForegroundColor Cyan

$gatewayHealth = Invoke-JsonRequest -Url "$BaseUrl/health"
if ($gatewayHealth) { Write-Host "  ✅ API Gateway: Healthy" -ForegroundColor Green } 
else { Write-Host "  ❌ API Gateway: Failed" -ForegroundColor Red }

$authHealth = Invoke-JsonRequest -Url "$AuthUrl/health"
if ($authHealth) { Write-Host "  ✅ Auth Service: Healthy" -ForegroundColor Green } 
else { Write-Host "  ❌ Auth Service: Failed" -ForegroundColor Red }

$fileHealth = Invoke-JsonRequest -Url "$FileUrl/health"
if ($fileHealth) { Write-Host "  ✅ File Service: Healthy" -ForegroundColor Green } 
else { Write-Host "  ❌ File Service: Failed" -ForegroundColor Red }

Write-Host ""

# 2. Authentication Tests
Write-Host "🔐 Authentication Tests:" -ForegroundColor Cyan

# Register
Write-Host "  Testing Registration..." -ForegroundColor Yellow
$registerBody = @{ username = $Username; password = $Password }
$registerResponse = Invoke-JsonRequest -Url "$BaseUrl/api/auth/register" -Method "Post" -Body $registerBody

if ($registerResponse) {
    if ($registerResponse.success) {
        Write-Host "    ✅ Registration successful: $($registerResponse.message)" -ForegroundColor Green
    } else {
        Write-Host "    ⚠️  Registration response: $($registerResponse | ConvertTo-Json)" -ForegroundColor Yellow
    }
} else {
    Write-Host "    ℹ️  Registration may have failed or user exists" -ForegroundColor Yellow
}

# Login
Write-Host "  Testing Login..." -ForegroundColor Yellow
$loginBody = @{ username = $Username; password = $Password }
$loginResponse = Invoke-JsonRequest -Url "$BaseUrl/api/auth/login" -Method "Post" -Body $loginBody

if ($loginResponse) {
    if ($loginResponse.success -and $loginResponse.token) {
        Write-Host "    ✅ Login successful!" -ForegroundColor Green
        Write-Host "      Token: $($loginResponse.token.Substring(0, 30))..." -ForegroundColor Cyan
        $token = $loginResponse.token
        
        # 3. File Operations
        Write-Host ""
        Write-Host "📁 File Operations:" -ForegroundColor Cyan
        
        # List Files
        Write-Host "  Testing List Files..." -ForegroundColor Yellow
        try {
            $headers = @{ "Authorization" = "Bearer $token" }
            $listResponse = Invoke-RestMethod -Uri "$BaseUrl/api/files/list" -Method Get -Headers $headers -TimeoutSec 5
            Write-Host "    ✅ List files successful" -ForegroundColor Green
            if ($listResponse.totalFiles) {
                Write-Host "      Total files: $($listResponse.totalFiles)" -ForegroundColor Cyan
            }
        } catch {
            Write-Host "    ❌ List files failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
    } else {
        Write-Host "    ❌ Login failed: $($loginResponse.message)" -ForegroundColor Red
    }
} else {
    Write-Host "    ❌ Login request failed" -ForegroundColor Red
}

Write-Host ""
Write-Host "✅ Test Complete!" -ForegroundColor Cyan