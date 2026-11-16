# تشغيل Backend باستخدام Docker

هذه الإعدادات تشغّل واجهة ASP.NET Core فقط. يجب أن تكون قاعدة PostgreSQL موجودة ومتاحة للحاوية؛ لا ينشئ المشروع مخطط قاعدة البيانات تلقائياً.

## الإعداد

من مجلد `Backend/QuranCenterManager_API`:

1. انسخ `.env.docker.example` إلى `.env`.
2. عدّل `DB_CONNECTION` ليشير إلى قاعدة البيانات الحالية. عند تشغيل PostgreSQL على جهاز Windows المضيف، استخدم `Host=host.docker.internal`.
3. استبدل `JWT_SECRET` بقيمة عشوائية لا تقل عن 32 بايت. يمكن توليدها في PowerShell بالأمر:

   ```powershell
   [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
   ```

4. اترك `ASPNETCORE_ENVIRONMENT=Development` للتطوير وSwagger. عند النشر غيّرها إلى `Production`.

قبل التشغيل، طبّق `create-memorization-plans.sql` مرة واحدة على قاعدة المشروع الحالية. من PowerShell، وبعد تعديل اسم المستخدم وقاعدة البيانات:

```powershell
psql -h localhost -p 5432 -U YOUR_DATABASE_USER -d halaqa_db -f create-memorization-plans.sql
```

أو افتح الملف في pgAdmin ونفّذه على قاعدة المشروع. لا تستخدم ملف SQL القديم الموجود في `Data_halaqa_database` لهذا التحديث؛ مخططه لا يطابق نماذج EF الحالية.

## التشغيل

```powershell
docker compose -f docker-compose.backend.yml up --build -d
```

تكون الواجهة على `http://localhost:8080`. في وضع التطوير تتوفر Swagger على `http://localhost:8080/swagger`.

لإيقاف الحاوية:

```powershell
docker compose -f docker-compose.backend.yml down
```

لا تضع ملف `.env` أو كلمات المرور في Git. يستبعد Docker ملفات `appsettings*.json` من صورة التشغيل، وتُمرر سلسلة الاتصال ومفتاح JWT عبر متغيرات البيئة.
