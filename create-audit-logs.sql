CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "AuditLogId"  bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ActorUserId" integer NULL,
    "ActorName"   varchar(200) NOT NULL DEFAULT '',
    "ActorRole"   varchar(50)  NOT NULL DEFAULT '',
    "Action"      varchar(60)  NOT NULL,
    "EntityType"  varchar(40)  NOT NULL,
    "EntityId"    varchar(40)  NULL,
    "EntityName"  varchar(200) NULL,
    "Details"     varchar(1000) NULL,
    "CreatedAt"   timestamp without time zone NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedAt" ON "AuditLogs" ("CreatedAt" DESC);
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Entity" ON "AuditLogs" ("EntityType", "EntityId");
