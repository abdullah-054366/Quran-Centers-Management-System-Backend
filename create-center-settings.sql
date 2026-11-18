-- إعدادات المركز الإضافية. شغّله مرة واحدة.
CREATE TABLE IF NOT EXISTS "CenterSettings" (
    "CenterId" integer PRIMARY KEY REFERENCES "Centers"("CenterId") ON DELETE CASCADE,
    "Phone" varchar(30),
    "Address" varchar(250),
    "DefaultDays" varchar(20) NOT NULL DEFAULT '0,1,2,3,4',
    "DefaultStartTime" varchar(5) NOT NULL DEFAULT '16:00',
    "DefaultEndTime" varchar(5) NOT NULL DEFAULT '18:00',
    "LateAfterMinutes" integer NOT NULL DEFAULT 10 CHECK ("LateAfterMinutes" BETWEEN 0 AND 120),
    "ExcusedCountsAsAttended" boolean NOT NULL DEFAULT false,
    "UpdatedAt" timestamp without time zone NOT NULL DEFAULT now()
);
