# Quick Setup Script for Python Flask Server with Supabase

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Flask Face API - Supabase Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if in correct directory
if (-not (Test-Path "app.py")) {
    Write-Host "ERROR: Please run this script from the face_api_demo directory!" -ForegroundColor Red
    Write-Host "Usage: cd D:\Capstone\Test\face_api_demo && .\setup_supabase.ps1" -ForegroundColor Yellow
    exit 1
}

# Install dependencies
Write-Host "Step 1: Installing Python dependencies..." -ForegroundColor Green
pip install supabase==2.3.0 python-dotenv==1.0.0

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to install dependencies" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Dependencies installed successfully!" -ForegroundColor Green
Write-Host ""

# Create .env file if it doesn't exist
if (-not (Test-Path ".env")) {
    Write-Host "Step 2: Creating .env configuration file..." -ForegroundColor Green
    
    if (Test-Path ".env.example") {
        Copy-Item ".env.example" ".env"
        Write-Host "✅ Created .env file from template" -ForegroundColor Green
    } else {
        # Create default .env
        @"
# Supabase Configuration
SUPABASE_URL=YOUR_SUPABASE_PROJECT_URL
SUPABASE_KEY=YOUR_SUPABASE_ANON_KEY
SUPABASE_BUCKET=student-photos
SUPABASE_ENABLED=false

# Flask Configuration
FLASK_DEBUG=True
FLASK_PORT=5000
"@ | Out-File -FilePath ".env" -Encoding UTF8
        Write-Host "✅ Created default .env file" -ForegroundColor Green
    }
    Write-Host ""
} else {
    Write-Host "ℹ️  .env file already exists, skipping..." -ForegroundColor Yellow
    Write-Host ""
}

# Prompt for Supabase configuration
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Supabase Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Do you want to configure Supabase now? (Y/N)" -ForegroundColor Yellow
$configNow = Read-Host

if ($configNow -eq "Y" -or $configNow -eq "y") {
    Write-Host ""
    Write-Host "Please enter your Supabase credentials:" -ForegroundColor Green
    Write-Host ""
    
    $url = Read-Host "Supabase Project URL (https://xxxxx.supabase.co)"
    $key = Read-Host "Supabase Anon Key"
    $bucket = Read-Host "Storage Bucket Name (default: student-photos)"
    
    if ([string]::IsNullOrWhiteSpace($bucket)) {
        $bucket = "student-photos"
    }
    
    $enabled = Read-Host "Enable Supabase? (true/false, default: true)"
    if ([string]::IsNullOrWhiteSpace($enabled)) {
        $enabled = "true"
    }
    
    # Update .env file
    $envContent = @"
# Supabase Configuration
SUPABASE_URL=$url
SUPABASE_KEY=$key
SUPABASE_BUCKET=$bucket
SUPABASE_ENABLED=$enabled

# Flask Configuration
FLASK_DEBUG=True
FLASK_PORT=5000
"@
    
    $envContent | Out-File -FilePath ".env" -Encoding UTF8
    Write-Host ""
    Write-Host "✅ Supabase configuration saved to .env" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "ℹ️  Skipping Supabase configuration" -ForegroundColor Yellow
    Write-Host "You can manually edit the .env file later" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. If you skipped configuration, edit .env file with your Supabase credentials" -ForegroundColor White
Write-Host "2. Start the Flask server: python app.py" -ForegroundColor White
Write-Host "3. Test the API: curl http://localhost:5000/health" -ForegroundColor White
Write-Host ""
Write-Host "Documentation: SUPABASE_PYTHON_GUIDE.md" -ForegroundColor Cyan
Write-Host ""
