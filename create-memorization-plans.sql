BEGIN;

CREATE TABLE IF NOT EXISTS "MemorizationPlans" (
    "MemorizationPlanId" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "CenterId" integer NOT NULL,
    "StudentId" integer NOT NULL,
    "Type" smallint NOT NULL CHECK ("Type" IN (0, 1)),
    "FromSurah" smallint NOT NULL CHECK ("FromSurah" BETWEEN 1 AND 114),
    "ToSurah" smallint NOT NULL CHECK ("ToSurah" BETWEEN 1 AND 114),
    "PagesPerSession" smallint NOT NULL CHECK ("PagesPerSession" > 0),
    "TargetRounds" smallint NOT NULL CHECK ("TargetRounds" > 0),
    "StartDate" date NOT NULL,
    "DaysOfWeek" integer[] NOT NULL DEFAULT ARRAY[0, 2, 4]::integer[],
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedByUserId" integer NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp without time zone,
    CONSTRAINT "MemorizationPlans_SurahRange_check" CHECK ("FromSurah" <= "ToSurah"),
    CONSTRAINT "MemorizationPlans_DaysOfWeek_check" CHECK (
        cardinality("DaysOfWeek") BETWEEN 1 AND 7
        AND "DaysOfWeek" <@ ARRAY[0, 1, 2, 3, 4, 5, 6]::integer[]
    ),
    CONSTRAINT "MemorizationPlans_CenterId_fkey"
        FOREIGN KEY ("CenterId") REFERENCES "Centers" ("CenterId") ON DELETE RESTRICT,
    CONSTRAINT "MemorizationPlans_StudentId_fkey"
        FOREIGN KEY ("StudentId") REFERENCES "Students" ("StudentId") ON DELETE RESTRICT,
    CONSTRAINT "MemorizationPlans_CreatedByUserId_fkey"
        FOREIGN KEY ("CreatedByUserId") REFERENCES "Users" ("UserId") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_MemorizationPlans_Student_StartDate"
    ON "MemorizationPlans" ("StudentId", "StartDate");

COMMIT;
