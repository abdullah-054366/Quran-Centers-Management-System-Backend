#!/usr/bin/env bash
# نسخ احتياطي لقاعدة halaqa_db من حاوية Docker إلى مجلد backups/
set -euo pipefail
mkdir -p backups
FILE="backups/halaqa_db_$(date +%Y%m%d_%H%M%S).sql"
docker exec halaqa-postgres pg_dump -U halaqa_admin -d halaqa_db > "$FILE"
echo "تم: $FILE"
