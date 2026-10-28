
INSERT INTO "Notifications"
    ("Id", "RecipientUserId", "OperationalRestrictionId", "Type",
     "Title", "Message", "RequiresAction", "CreatedAt", "ReadAt", "ResolvedAt")
SELECT
    v.notification_id::uuid,
    u."Id",
    NULL,
    2,
    v.title,
    v.message,
    FALSE,
    NOW() - v.created_ago,
    CASE WHEN v.is_read THEN NOW() - INTERVAL '10 minutes' ELSE NULL END,
    NULL
FROM (VALUES
    ('11111111-1111-4111-8111-111111111101', 'originator@example.test',
     'Welcome to Metro', 'This is a test notification for Olivia.',
     INTERVAL '2 hours', FALSE),
    ('11111111-1111-4111-8111-111111111102', 'dom@example.test',
     'System update', 'This is a read test notification for Dan.',
     INTERVAL '1 day', TRUE),
    ('11111111-1111-4111-8111-111111111103', 'operator@example.test',
     'New message', 'This is an unread test notification for Oscar.',
     INTERVAL '30 minutes', FALSE)
) AS v(notification_id, email, title, message, created_ago, is_read)
JOIN "Users" u ON u."Email" = v.email
ON CONFLICT ("Id") DO NOTHING;
