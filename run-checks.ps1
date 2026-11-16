# يشغّل: ترحيل قاعدة البيانات ثم بناء الباكند ثم بناء الفرونت. شغّله من PowerShell:
#   cd C:\Users\Abdullah\Desktop\Quran_Centers_Management_System_Project\Backend\QuranCenterManager_API
#   .\run-checks.ps1
$ErrorActionPreference = "Stop"
$backend  = $PSScriptRoot
$frontend = Join-Path $PSScriptRoot "..\..\Frontend" | Resolve-Path

Write-Host "1) ترحيل قاعدة البيانات (SessionId)..." -ForegroundColor Cyan
$env:PGPASSWORD = Read-Host "كلمة مرور PostgreSQL للمستخدم halaqa_admin"
psql -h localhost -p 5432 -U halaqa_admin -d halaqa_db -v ON_ERROR_STOP=1 -f (Join-Path $backend "add-memorization-session-id.sql")
Remove-Item Env:PGPASSWORD

Write-Host "2) بناء الباكند..." -ForegroundColor Cyan
dotnet build (Join-Path $backend "QuranCenterManager_API.slnx") -nologo

Write-Host "3) بناء الفرونت..." -ForegroundColor Cyan
Push-Location $frontend
npm install
npm run build
Pop-Location

Write-Host "انتهى. أرسل لي أي خطأ ظهر." -ForegroundColor Green
