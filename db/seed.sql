INSERT INTO "Users" ("Id", "Name", "Email", "PasswordHash")
SELECT gen_random_uuid(), v.name, v.email, v.hash
FROM (VALUES
  ('Olivia Originator', 'originator@example.test', 'dev-only-placeholder-hash'),
  ('Dan Dom',           'dom@example.test',         'dev-only-placeholder-hash'),
  ('Oscar Operator',    'operator@example.test',    'dev-only-placeholder-hash')
) AS v(name, email, hash)
WHERE NOT EXISTS (
  SELECT 1 FROM "Users" u WHERE u."Email" = v.email
);