ALTER TABLE public."Users"
    ADD COLUMN IF NOT EXISTS "RefreshTokenHash" character varying(100),
    ADD COLUMN IF NOT EXISTS "RefreshTokenExpiresAt" timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "RefreshTokenRevokedAt" timestamp with time zone;

