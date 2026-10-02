INSERT INTO "Users" ("Email", "FullName", "Role")
SELECT v.email, v.full_name, v.role
FROM (VALUES
  ('admin@example.test',     'Alex Admin',      0),
  ('clinician@example.test', 'Casey Clinician', 1),
  ('scheduler@example.test', 'Sam Scheduler',   2)
) AS v(email, full_name, role)
WHERE NOT EXISTS (
  SELECT 1 FROM "Users" u WHERE u."Email" = v.email
);
