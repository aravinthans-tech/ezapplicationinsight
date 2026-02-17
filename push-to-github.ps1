# PowerShell script to push TelemetryAPI to GitHub
# Replace YOUR_USERNAME with your GitHub username

$githubUsername = Read-Host "Enter your GitHub username"
$repoName = "ezapplicationinsight"

Write-Host "Setting up remote repository..." -ForegroundColor Green
git remote add origin "https://github.com/$githubUsername/$repoName.git" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Remote already exists, updating..." -ForegroundColor Yellow
    git remote set-url origin "https://github.com/$githubUsername/$repoName.git"
}

Write-Host "Pushing to GitHub..." -ForegroundColor Green
git push -u origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "Successfully pushed to GitHub!" -ForegroundColor Green
    Write-Host "Repository URL: https://github.com/$githubUsername/$repoName" -ForegroundColor Cyan
} else {
    Write-Host "Failed to push. Make sure:" -ForegroundColor Red
    Write-Host "1. The repository exists on GitHub" -ForegroundColor Yellow
    Write-Host "2. You have push access" -ForegroundColor Yellow
    Write-Host "3. You're authenticated with GitHub" -ForegroundColor Yellow
}

