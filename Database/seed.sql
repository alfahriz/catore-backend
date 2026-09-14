-- Seed dummy REV 2 -- sesuai schema baru hasil kurasi 2026-09-12.
-- PK bigint (bigserial, auto-assign -- pakai RETURNING/CTE biar FK ikut konsisten).
-- Login dummy: rafi.dummy@catore.test / Password123!

-- ==================== mparam (lookup) ====================
-- name = kode singkat dipakai kode, value = angka urut PER paramType (mulai 1
-- lagi tiap kategori ganti) -- lihat catatan swap name/value di schema.sql.
INSERT INTO mparam ("paramType", name, value, "createdOn", "createdBy") VALUES
  ('GENDER', 'Male', 1, now(), NULL),
  ('GENDER', 'Female', 2, now(), NULL),
  ('ACTIVITY_LEVEL', 'Sedentary', 1, now(), NULL),
  ('ACTIVITY_LEVEL', 'Lightly active', 2, now(), NULL),
  ('ACTIVITY_LEVEL', 'Moderately active', 3, now(), NULL),
  ('ACTIVITY_LEVEL', 'Very active', 4, now(), NULL),
  ('METRIC_UNIT', 'Kilogram', 1, now(), NULL),
  ('METRIC_UNIT', 'Pound', 2, now(), NULL),
  ('MEAL_TYPE', 'Breakfast', 1, now(), NULL),
  ('MEAL_TYPE', 'Lunch', 2, now(), NULL),
  ('MEAL_TYPE', 'Dinner', 3, now(), NULL),
  ('MEAL_TYPE', 'Snack', 4, now(), NULL),
  ('DEFICIT_CATEGORY', 'Soft', 1, now(), NULL),
  ('DEFICIT_CATEGORY', 'Mid', 2, now(), NULL),
  ('DEFICIT_CATEGORY', 'Hard', 3, now(), NULL),
  ('NOTIF_CATEGORY', 'Reminder', 1, now(), NULL),
  ('NOTIF_CATEGORY', 'Freeze', 2, now(), NULL),
  ('NOTIF_CATEGORY', 'Account', 3, now(), NULL),
  ('NOTIF_CATEGORY', 'Achievement', 4, now(), NULL),
  ('RECORD_FROM', 'Lazy Create', 1, now(), NULL),
  ('RECORD_FROM', 'Freeze', 2, now(), NULL);

-- ==================== mparamnotif (template pesan notifikasi) ====================
-- Placeholder {xxx} disubstitusi backend dari Dictionary<string,string> params
-- saat SendPush dipanggil dgn key ini -- lihat NotificationService.cs utk method aslinya.
INSERT INTO mparamnotif (key, type, title, body, "isActive", "createdOn", "modifiedOn")
SELECT v.key, (SELECT "paramPK" FROM mparam WHERE "paramType"='NOTIF_CATEGORY' AND name=v.category), v.title, v.body, true, now(), now()
FROM (VALUES
  ('DAILY_REMINDER', 'Reminder', 'Daily reminder', 'Don''t forget to log your meals today.'),
  ('DAILY_REMINDER_SAFETY_FLOOR', 'Reminder', 'Daily reminder', 'Don''t forget to log today — your limit is close to the safety floor.'),
  ('GRACE_WINDOW_COUNTDOWN', 'Reminder', 'Grace window active', 'You have {missingDays} day(s) missing. Log by {oldestDeadline} to keep your streak.'),
  ('WIPE', 'Account', 'Account wiped', 'Your data has been reset after missing the grace window.'),
  ('FORCE_LOGOUT', 'Account', 'Logged out', 'Your account was signed in on another device.'),
  ('FREEZE_USED', 'Freeze', '{label} used', '{label} was used automatically. {remaining} remaining.'),
  ('FREEZE_GAINED', 'Freeze', '{label} earned', 'You''ve earned a new {label} token.'),
  ('PASSWORD_RESET_REQUESTED', 'Account', 'Password reset requested', 'Check your email to reset your password.'),
  ('WEIGH_IN_REMINDER', 'Reminder', 'Weigh-in reminder', 'Don''t forget to log your weight this week.'),
  ('GOAL_ACHIEVED', 'Achievement', 'Goal achieved!', 'Congratulations, you''ve reached your goal weight. Your streak ({frozenStreakCount}) is now frozen.')
) AS v(key, category, title, body);

-- ==================== muser + mprofile + tweightlog (CTE biar FK auto-link ke bigserial) ====================
WITH new_user AS (
  INSERT INTO muser (email, password, "isEmailVerif", "createdOn", "modifiedOn")
  VALUES ('rafi.dummy@catore.test', '$2a$11$106KUkmj3rKe2SkeXcUAiuUoPRlpf0OIJFNpSvqFOPl9J5GNurEm.', true, now(), now())
  RETURNING "userPk"
),
new_profile AS (
  INSERT INTO mprofile ("userId", name, gender, age, height, weight, "goalWeight", "isRecomendGoalUsed", "baseActLevel", "metricParam", timezone, "isActive", "isUpgraded", "createdOn", "createdBy", "modifiedOn")
  SELECT "userPk", 'Rafi',
    (SELECT "paramPK" FROM mparam WHERE "paramType"='GENDER' AND name='Male'),
    27, 175, 77.4, 68, false,
    (SELECT "paramPK" FROM mparam WHERE "paramType"='ACTIVITY_LEVEL' AND name='Moderately active'),
    (SELECT "paramPK" FROM mparam WHERE "paramType"='METRIC_UNIT' AND name='Kilogram'),
    'Asia/Jakarta', true, true, now(), "userPk", now()
  FROM new_user
  RETURNING "userId"
)
INSERT INTO tweightlog ("userId", "checkpointDate", weight, "createdOn", "isDeleted")
SELECT "userId", v."checkpointDate", v.weight, v."checkpointDate"::timestamptz, false
FROM new_profile, (VALUES
  ('2026-01-22'::date, 81.95),
  ('2026-01-29'::date, 82.11),
  ('2026-02-05'::date, 81.84),
  ('2026-02-12'::date, 81.63),
  ('2026-02-19'::date, 81.37),
  ('2026-02-26'::date, 81.14),
  ('2026-03-05'::date, 81.16),
  ('2026-03-12'::date, 80.92),
  ('2026-03-19'::date, 80.68),
  ('2026-03-26'::date, 80.42),
  ('2026-04-02'::date, 80.2),
  ('2026-04-09'::date, 80.12),
  ('2026-04-16'::date, 79.92),
  ('2026-04-23'::date, 79.65),
  ('2026-04-30'::date, 79.45),
  ('2026-05-07'::date, 79.19),
  ('2026-05-14'::date, 79.17),
  ('2026-05-21'::date, 78.91),
  ('2026-05-28'::date, 78.68),
  ('2026-06-04'::date, 78.43),
  ('2026-06-11'::date, 78.18),
  ('2026-06-18'::date, 78.33),
  ('2026-06-25'::date, 78.06),
  ('2026-07-02'::date, 77.85),
  ('2026-07-09'::date, 77.58),
  ('2026-07-16'::date, 77.4)
) AS v("checkpointDate", weight);
