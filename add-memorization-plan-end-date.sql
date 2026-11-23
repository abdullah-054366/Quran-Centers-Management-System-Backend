-- يضيف EndDate لمدّة الخطة. NULL = خطة مفتوحة النهاية يحكمها PagesPerSession (السلوك القديم)،
-- وقيمة = خطة محدّدة بمدّة يُشتقّ فيها مقدار الجلسة من النطاق وعدد أيام الخطة في المدّة.
-- الخطط القائمة تبقى كما هي (EndDate = NULL) فلا ترحيل بيانات.
BEGIN;

ALTER TABLE "MemorizationPlans"
    ADD COLUMN IF NOT EXISTS "EndDate" date NULL;

-- true = مقدار الجلسة مشتقّ من النطاق والمدّة، وPagesPerSession تقريب للعرض فقط.
-- false (الافتراضي) = المقدار مثبّت كما كان، فخطط اليوم تبقى على سلوكها.
ALTER TABLE "MemorizationPlans"
    ADD COLUMN IF NOT EXISTS "DeriveAmount" boolean NOT NULL DEFAULT false;

-- النهاية لا تسبق البداية. التحقّق في الـ API يُنسى عند إضافة نقطة وصول أخرى؛ القيد لا يُنسى.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'MemorizationPlans_EndDate_check'
          AND conrelid = '"MemorizationPlans"'::regclass
    ) THEN
        ALTER TABLE "MemorizationPlans"
            ADD CONSTRAINT "MemorizationPlans_EndDate_check"
            CHECK ("EndDate" IS NULL OR "EndDate" >= "StartDate");
    END IF;
END $$;

COMMIT;
