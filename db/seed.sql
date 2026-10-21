INSERT INTO "Users" ("Id", "Name", "Email", "PasswordHash", "Role")
SELECT gen_random_uuid(), v.name, v.email, v.hash, v.role
FROM (VALUES
  ('Olivia Originator', 'originator@example.test', 'dev-only-placeholder-hash', 0),
  ('Dan Dom',           'dom@example.test',         'dev-only-placeholder-hash', 1),
  ('Oscar Operator',    'operator@example.test',    'dev-only-placeholder-hash', 2)
) AS v(name, email, hash, role)
WHERE NOT EXISTS (
  SELECT 1 FROM "Users" u WHERE u."Email" = v.email
);