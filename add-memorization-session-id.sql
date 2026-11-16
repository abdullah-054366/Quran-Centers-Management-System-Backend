-- يضيف SessionId لتجميع سجلات السور المتعددة في جلسة واحدة (تُحسب سجلاً واحداً في الحد اليومي).
-- السجلات القديمة تبقى كما هي (SessionId = NULL) وكل سجل قديم يُحسب جلسة مستقلة.
BEGIN;

ALTER TABLE "MemorizationRecords"
    ADD COLUMN IF NOT EXISTS "SessionId" uuid NULL;

CREATE INDEX IF NOT EXISTS "IX_MemorizationRecords_Student_Date_Type"
    ON "MemorizationRecords" ("StudentId", "RecordDate", "Type");

CREATE INDEX IF NOT EXISTS "IX_MemorizationRecords_SessionId"
    ON "MemorizationRecords" ("SessionId")
    WHERE "SessionId" IS NOT NULL;

COMMIT;
