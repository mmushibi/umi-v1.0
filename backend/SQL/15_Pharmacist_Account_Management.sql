-- Pharmacist Account Management Migration
-- This migration adds the PharmacistProfile table for managing pharmacist-specific account data

-- Create PharmacistProfile table
CREATE TABLE IF NOT EXISTS "PharmacistProfiles" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(450) NOT NULL,
    "TenantId" VARCHAR(6) NOT NULL,
    "FirstName" VARCHAR(100),
    "LastName" VARCHAR(100),
    "Email" VARCHAR(100),
    "Phone" VARCHAR(20),
    "LicenseNumber" VARCHAR(100),
    "EmailNotifications" BOOLEAN DEFAULT FALSE,
    "ClinicalAlerts" BOOLEAN DEFAULT FALSE,
    "SessionTimeout" INTEGER DEFAULT 30,
    "Language" VARCHAR(10) DEFAULT 'en',
    "TwoFactorEnabled" BOOLEAN DEFAULT FALSE,
    "TwoFactorSecret" VARCHAR(500),
    "PasswordChangedAt" TIMESTAMP,
    "ForcePasswordChange" BOOLEAN DEFAULT FALSE,
    "ProfilePicture" VARCHAR(500),
    "Signature" VARCHAR(500),
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance and uniqueness
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PharmacistProfiles_UserId" ON "PharmacistProfiles" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_PharmacistProfiles_TenantId" ON "PharmacistProfiles" ("TenantId");
CREATE INDEX IF NOT EXISTS "IX_PharmacistProfiles_Email" ON "PharmacistProfiles" ("Email");
CREATE INDEX IF NOT EXISTS "IX_PharmacistProfiles_LicenseNumber" ON "PharmacistProfiles" ("LicenseNumber");
CREATE INDEX IF NOT EXISTS "IX_PharmacistProfiles_IsActive" ON "PharmacistProfiles" ("IsActive");

-- Create foreign key constraints
ALTER TABLE "PharmacistProfiles" 
ADD CONSTRAINT "FK_PharmacistProfiles_Users" 
FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") 
ON DELETE CASCADE;

ALTER TABLE "PharmacistProfiles" 
ADD CONSTRAINT "FK_PharmacistProfiles_Tenants" 
FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("TenantId") 
ON DELETE CASCADE;

-- Create trigger to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_pharmacist_profile_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER "PharmacistProfiles_UpdatedAt_Trigger"
BEFORE UPDATE ON "PharmacistProfiles"
FOR EACH ROW
EXECUTE FUNCTION update_pharmacist_profile_updated_at();

-- Insert default pharmacist profiles for existing users (optional)
-- This will create profiles for existing pharmacist users
INSERT INTO "PharmacistProfiles" ("UserId", "TenantId", "FirstName", "LastName", "Email", "LicenseNumber", "CreatedAt", "UpdatedAt")
SELECT 
    u."UserId",
    u."TenantId", 
    u."FirstName",
    u."LastName",
    u."Email",
    '' as "LicenseNumber",
    CURRENT_TIMESTAMP as "CreatedAt",
    CURRENT_TIMESTAMP as "UpdatedAt"
FROM "Users" u
WHERE u."Role" = 'Pharmacist'
AND NOT EXISTS (
    SELECT 1 FROM "PharmacistProfiles" p 
    WHERE p."UserId" = u."UserId" 
    AND p."TenantId" = u."TenantId"
);

-- Add comments for documentation
COMMENT ON TABLE "PharmacistProfiles" IS 'Stores pharmacist-specific profile information and settings';
COMMENT ON COLUMN "PharmacistProfiles"."UserId" IS 'Foreign key reference to the Users table';
COMMENT ON COLUMN "PharmacistProfiles"."TenantId" IS 'Foreign key reference to the Tenants table';
COMMENT ON COLUMN "PharmacistProfiles"."EmailNotifications" IS 'Whether the pharmacist wants to receive email notifications';
COMMENT ON COLUMN "PharmacistProfiles"."ClinicalAlerts" IS 'Whether the pharmacist wants to receive clinical alerts';
COMMENT ON COLUMN "PharmacistProfiles"."SessionTimeout" IS 'Session timeout in minutes';
COMMENT ON COLUMN "PharmacistProfiles"."TwoFactorEnabled" IS 'Whether two-factor authentication is enabled';
COMMENT ON COLUMN "PharmacistProfiles"."PasswordChangedAt" IS 'Timestamp when the password was last changed';
