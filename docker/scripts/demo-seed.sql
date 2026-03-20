-- Demo seed script for CountOrSell demo environment.
-- Populates a fresh migrated database with demo users and
-- pre-seeded collection data covering all demo sets.
--
-- Demo sets: lea, 2ed, vis, eoe, fdn, ecl, tla, fin, dsk,
--            usg, ulg, uns, p23, tdm
--
-- Usage (against a freshly migrated database):
--   psql "$POSTGRES_CONNECTION" -f docker/scripts/demo-seed.sql
--
-- All passwords below are for demo use only and are not
-- sensitive. The admin password satisfies the 15-character
-- minimum enforced by the application.

-- -------------------------------------------------------
-- Demo admin account
-- -------------------------------------------------------
-- Username: demo-admin
-- Password: demoAdminPass1! (bcrypt hash below is a placeholder)
-- This account is a non-builtin admin created for the demo.
-- -------------------------------------------------------

INSERT INTO "Users" (
    "Id",
    "Username",
    "DisplayName",
    "PasswordHash",
    "Role",
    "State",
    "AuthType",
    "IsBuiltinAdmin",
    "CreatedAt",
    "LastLoginAt"
) VALUES (
    '11111111-1111-1111-1111-111111111111',
    'demo-admin',
    'Demo Admin',
    -- placeholder hash; replace with real bcrypt hash of 'demoAdminPass1!'
    '$2a$12$placeholder_admin_hash_replace_me_before_use',
    1, -- Admin
    0, -- Active
    0, -- Local
    false,
    NOW(),
    NULL
) ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------
-- Demo general user account
-- -------------------------------------------------------
-- Username: demo-user
-- Password: demoUserPass1!
-- -------------------------------------------------------

INSERT INTO "Users" (
    "Id",
    "Username",
    "DisplayName",
    "PasswordHash",
    "Role",
    "State",
    "AuthType",
    "IsBuiltinAdmin",
    "CreatedAt",
    "LastLoginAt"
) VALUES (
    '22222222-2222-2222-2222-222222222222',
    'demo-user',
    'Demo User',
    -- placeholder hash; replace with real bcrypt hash of 'demoUserPass1!'
    '$2a$12$placeholder_user_hash_replace_me_before_use',
    0, -- GeneralUser
    0, -- Active
    0, -- Local
    false,
    NOW(),
    NULL
) ON CONFLICT ("Id") DO NOTHING;

-- -------------------------------------------------------
-- Demo user preferences
-- -------------------------------------------------------

INSERT INTO "UserPreferences" (
    "UserId",
    "SetCompletionRegularOnly",
    "DefaultPage"
) VALUES (
    '22222222-2222-2222-2222-222222222222',
    false,
    NULL
) ON CONFLICT ("UserId") DO NOTHING;

-- -------------------------------------------------------
-- Sample collection entries for demo-user
-- Covers a representative spread across demo sets.
-- CardIdentifier format: ^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$
-- -------------------------------------------------------

INSERT INTO "CollectionEntries" (
    "Id",
    "UserId",
    "CardIdentifier",
    "TreatmentKey",
    "Quantity",
    "Condition",
    "Autographed",
    "AcquisitionDate",
    "AcquisitionPrice",
    "Notes",
    "CreatedAt",
    "UpdatedAt"
) VALUES
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'eoe001', 'regular', 1, 0, false, NOW(), 1.50, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'eoe019', 'regular', 2, 0, false, NOW(), 0.75, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'eoe042', 'foil',    1, 0, false, NOW(), 3.00, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'fdn001', 'regular', 1, 0, false, NOW(), 2.00, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'fdn015', 'regular', 1, 1, false, NOW(), 1.25, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'dsk001', 'regular', 3, 0, false, NOW(), 0.50, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'dsk099', 'foil',    1, 0, false, NOW(), 4.50, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'lea001', 'regular', 1, 2, false, NOW(), 25.00, 'Alpha Black Lotus', NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', '2ed001', 'regular', 2, 0, false, NOW(), 0.25, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'usg001', 'regular', 1, 0, false, NOW(), 1.00, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'p23001', 'foil',    1, 0, false, NOW(), 8.00, NULL, NOW(), NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'tdm001', 'regular', 1, 0, false, NOW(), 1.50, NULL, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- -------------------------------------------------------
-- Sample wishlist entries for demo-user
-- -------------------------------------------------------

INSERT INTO "WishlistEntries" (
    "Id",
    "UserId",
    "CardIdentifier",
    "CreatedAt"
) VALUES
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'ecl001', NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'tla005', NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'fin010', NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'vis001', NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'ulg001', NOW()),
    (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'uns001', NOW())
ON CONFLICT DO NOTHING;
