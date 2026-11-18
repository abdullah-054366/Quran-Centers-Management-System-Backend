# نسخ احتياطي لقاعدة halaqa_db من حاوية Docker إلى مجلد backups
New-Item -ItemType Directory -Force -Path backups | Out-Null
$file = "backups/halaqa_db_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
docker exec halaqa-postgres pg_dump -U halaqa_admin -d halaqa_db | Out-File -Encoding utf8 $file
Write-Host "تم: $file"
