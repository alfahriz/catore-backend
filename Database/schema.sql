--
-- Catore database schema -- REV 2 (2026-09-12, hasil kurasi ulang)
--
-- CATATAN PENTING: file ini adalah TARGET schema hasil diskusi, BUKAN dump dari
-- DB aktual. DB lokal "catore" dan kode C# backend MASIH pakai schema LAMA
-- (uuid, nama tabel/kolom asli) -- lihat skema lama di git history file ini.
-- File ini dan seed.sql direview dulu sebelum eksekusi migration beneran.
--
-- Perubahan besar dari schema lama:
-- - Semua PK/FK: uuid -> bigint (bigserial utk auto-increment)
-- - Rename 8 tabel + kolom (lihat komentar "was" di tiap tabel)
-- - Tabel BARU: mparam (lookup generik), mconsumption (bank item, dari
--   pemecahan consumptionentry), mparamnotif (template pesan notifikasi)
-- - consumptionentry lama DIPECAH jadi mconsumption (master) + tconsumption (transaksi)
-- - 2 kolom boolean di-INVERT logikanya (bukan cuma rename): mprofile.isRecomendGoalUsed
--   (kebalikan goalweightismanual), mprofile.isActive (kebalikan isdeleted)
-- - tweightlog: 1 row = 1 MINGGU per user (checkpointDate = Jumat, upsert), bukan
--   1 row per input harian kayak sebelumnya
-- - tdailyrecord: tambah kolom actualCalories (cache SUM dari tconsumption)
--
-- Prefix: m = master (referensi relatif statis), t = transaksi/state
--

SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;

-- ============================================================
-- MASTER: muser (was: useraccount)
-- ============================================================
CREATE TABLE muser (
    "userPk"                       bigserial PRIMARY KEY,
    email                           text NOT NULL,
    password                        text NOT NULL,               -- hash BCrypt
    "jwtRefreshToken"               text,
    "jwtRefreshTokenExpiredAt"      timestamptz,
    "sessionId"                     uuid,                          -- tetap uuid, bukan PK row DB
    "resetToken"                    text,
    "resetTokenExpiredAt"           timestamptz,
    "isEmailVerif"                  boolean NOT NULL DEFAULT false,
    "emailVerifiedToken"            text,
    "emailVerifiedTokenExpiredAt"   timestamptz,
    "createdOn"                     timestamptz NOT NULL DEFAULT now(),
    "createdBy"                     bigint,                        -- = userPk sendiri (self-signup)
    "modifiedOn"                    timestamptz NOT NULL DEFAULT now(),
    "modifiedBy"                    text                           -- userPk (self-edit) atau "SYSTEM"/job
);
CREATE UNIQUE INDEX "IX_muser_email" ON muser (email);

-- ============================================================
-- MASTER: mparam (BARU) -- lookup generik serbaguna
-- ============================================================
-- name/value SWAP makna dari revisi awal (2026-09-14): name = label/kode Title
-- Case dipakai kode & tampilan (mis. "Male", "Moderately active", "Kilogram"),
-- value = angka urut PER paramType mulai dari 1 lagi tiap kategori ganti
-- (GENDER: 1,2 / ACTIVITY_LEVEL: 1,2,3,4) -- value CUMA INFORMATIF, BUKAN kunci
-- relasi. FK dari tabel lain SELALU ke paramPK (unik global), TIDAK PERNAH ke
-- value (value ambigu lintas paramType berbeda).
CREATE TABLE mparam (
    "paramPK"       bigserial PRIMARY KEY,
    "paramType"     text NOT NULL,   -- GENDER / ACTIVITY_LEVEL / METRIC_UNIT / MEAL_TYPE / DEFICIT_CATEGORY / NOTIF_CATEGORY, dst
    name            text NOT NULL,   -- label/kode Title Case (mis. "Male", "Moderately active", "Kilogram")
    value           integer NOT NULL,-- angka urut per paramType (informatif doang, BUKAN FK)
    "createdOn"     timestamptz NOT NULL DEFAULT now(),
    "createdBy"     bigint           -- userId atau sistem/seed
);
CREATE INDEX "IX_mparam_paramType" ON mparam ("paramType");

-- ============================================================
-- MASTER: mparamnotif (BARU) -- template pesan notifikasi
-- Dipakai backend: SendPush ganti dari terima title+body literal jadi terima
-- key + Dictionary<string,string> params -> query row ini by key -> substitusi
-- placeholder {label}/{remaining}/dst dari dictionary -> kirim ke Firebase.
-- ============================================================
CREATE TABLE mparamnotif (
    "notifPk"       bigserial PRIMARY KEY,
    key              text NOT NULL,   -- kode unik per jenis notif spesifik: FREEZE_USED, WIPE, DAILY_REMINDER, dst
    type             bigint,           -- FK -> mparam (NOTIF_CATEGORY) -- klasifikasi kategori, beda dari key
    title            text NOT NULL,   -- template judul, mis. "{label} used"
    body             text NOT NULL,   -- template isi, mis. "{label} was used automatically. {remaining} remaining."
    "isActive"      boolean NOT NULL DEFAULT true,
    "createdOn"     timestamptz NOT NULL DEFAULT now(),
    "modifiedOn"    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "FK_mparamnotif_type" FOREIGN KEY (type) REFERENCES mparam ("paramPK")
);
CREATE UNIQUE INDEX "IX_mparamnotif_key" ON mparamnotif (key);

-- ============================================================
-- MASTER: mprofile (was: profileaccount)
-- ============================================================
CREATE TABLE mprofile (
    "profilePK"             bigserial PRIMARY KEY,
    "userId"                bigint NOT NULL,                       -- FK -> muser
    name                     text NOT NULL,
    gender                   bigint,                                -- FK -> mparam (GENDER)
    age                      integer NOT NULL,
    height                   numeric NOT NULL,
    weight                   numeric NOT NULL,
    "goalWeight"             numeric,
    "isRecomendGoalUsed"     boolean NOT NULL DEFAULT false,        -- INVERT dari goalweightismanual lama
    "goalMode"               bigint,                                -- FK -> mparam (GOAL_MODE: Cutting/Bulking/Maintain), default Cutting
    "baseActLevel"           bigint,                                -- FK -> mparam (ACTIVITY_LEVEL)
    "metricParam"            bigint,                                -- FK -> mparam (METRIC_UNIT)
    timezone                 text NOT NULL,
    "isActive"               boolean NOT NULL DEFAULT true,         -- INVERT dari isdeleted lama
    "isUpgraded"             boolean NOT NULL DEFAULT false,
    "lastWipeOn"             timestamptz,
    "createdOn"              timestamptz NOT NULL DEFAULT now(),
    "createdBy"              bigint,
    "modifiedOn"             timestamptz NOT NULL DEFAULT now(),
    "modifiedBy"             text,
    CONSTRAINT "FK_mprofile_muser" FOREIGN KEY ("userId") REFERENCES muser ("userPk"),
    CONSTRAINT "FK_mprofile_gender" FOREIGN KEY (gender) REFERENCES mparam ("paramPK"),
    CONSTRAINT "FK_mprofile_baseActLevel" FOREIGN KEY ("baseActLevel") REFERENCES mparam ("paramPK"),
    CONSTRAINT "FK_mprofile_metricParam" FOREIGN KEY ("metricParam") REFERENCES mparam ("paramPK"),
    CONSTRAINT "FK_mprofile_goalMode" FOREIGN KEY ("goalMode") REFERENCES mparam ("paramPK")
);
CREATE UNIQUE INDEX "IX_mprofile_userId" ON mprofile ("userId");

-- ============================================================
-- MASTER: mconsumption (BARU) -- bank makanan/minuman, global/shared
-- Dedup key: name (trim+lowercase) + calories (exact match)
-- ============================================================
CREATE TABLE mconsumption (
    "consumptionPk"     bigserial PRIMARY KEY,
    name                 text NOT NULL,
    calories             integer NOT NULL,
    "createdOn"          timestamptz NOT NULL DEFAULT now(),
    "createdBy"          bigint NOT NULL,                          -- FK -> muser, siapa yg pertama nambahin
    CONSTRAINT "FK_mconsumption_createdBy" FOREIGN KEY ("createdBy") REFERENCES muser ("userPk")
);
CREATE UNIQUE INDEX "IX_mconsumption_dedup" ON mconsumption (lower(trim(name)), calories);

-- ============================================================
-- TRANSAKSI: tconsumption (was: consumptionentry, dipecah dari mconsumption)
-- ============================================================
CREATE TABLE tconsumption (
    "entryPk"           bigserial PRIMARY KEY,
    "userId"            bigint NOT NULL,                           -- FK -> muser
    "consumptionId"     bigint NOT NULL,                            -- FK -> mconsumption
    "mealType"          bigint,                                     -- FK -> mparam (MEAL_TYPE)
    "entryTimestamp"    timestamptz NOT NULL,                       -- juga basis grouping harian
    "createdOn"         timestamptz NOT NULL DEFAULT now(),
    "isDeleted"         boolean NOT NULL DEFAULT false,
    "isDeletedOn"       timestamptz,
    CONSTRAINT "FK_tconsumption_muser" FOREIGN KEY ("userId") REFERENCES muser ("userPk"),
    CONSTRAINT "FK_tconsumption_mconsumption" FOREIGN KEY ("consumptionId") REFERENCES mconsumption ("consumptionPk"),
    CONSTRAINT "FK_tconsumption_mealType" FOREIGN KEY ("mealType") REFERENCES mparam ("paramPK")
);
CREATE INDEX "IX_tconsumption_userId_entryTimestamp" ON tconsumption ("userId", "entryTimestamp");

-- ============================================================
-- TRANSAKSI: tweightlog (was: weightlog)
-- 1 row = 1 MINGGU per user. checkpointDate = Jumat minggu itu, dihitung
-- OTOMATIS sistem saat insert/update (bukan user pilih). Input Sabtu-Kamis
-- UPSERT ke row minggu berjalan; lewat Jumat, mulai row minggu baru.
-- ============================================================
CREATE TABLE tweightlog (
    "weightLogPk"       bigserial PRIMARY KEY,
    "userId"            bigint NOT NULL,                           -- FK -> muser
    "checkpointDate"    date NOT NULL,                              -- Jumat minggu itu, auto-calc
    weight               numeric(5,2) NOT NULL,
    "createdOn"         timestamptz NOT NULL DEFAULT now(),
    "modifiedOn"        timestamptz,
    "isDeleted"         boolean NOT NULL DEFAULT false,
    "isDeletedOn"       timestamptz,
    "wipeReason"        text,                                       -- 'Manual' (ganti mode) / 'LostStreak' (wipe rutin), NULL kalau belum pernah di-wipe
    CONSTRAINT "FK_tweightlog_muser" FOREIGN KEY ("userId") REFERENCES muser ("userPk")
);
CREATE UNIQUE INDEX "IX_tweightlog_userId_checkpointDate" ON tweightlog ("userId", "checkpointDate");

-- ============================================================
-- TRANSAKSI: tdailyrecord (was: dailyrecord)
-- Snapshot limit kalori harian, DIKUNCI saat hari itu terjadi.
-- actualCalories = kolom BARU, cache SUM dari tconsumption -- WAJIB
-- disinkronkan ulang di service layer saat AddEntries & WipeUserData.
-- ============================================================
CREATE TABLE tdailyrecord (
    "dailyRecordPk"     bigserial PRIMARY KEY,
    "userId"            bigint NOT NULL,                           -- FK -> muser
    "recordDate"        date NOT NULL,
    "calorieCategory"   bigint,                                     -- FK -> mparam (DEFICIT_CATEGORY atau BULKING_CATEGORY, tergantung mprofile.goalMode). RENAMED dari deficitCategory 2026-09-22
    "paToday"           boolean NOT NULL DEFAULT false,
    "effectiveTdee"     numeric NOT NULL,                           -- dikunci
    "effectiveLimit"    numeric NOT NULL,                           -- dikunci
    "actualCalories"    integer NOT NULL DEFAULT 0,                 -- BARU: cache SUM tconsumption
    "createdVia"        bigint,                                     -- FK -> mparam (RECORD_FROM: Lazy Create / Freeze)
    "isFrozen"          boolean NOT NULL DEFAULT false,
    "modifiedOn"        timestamptz NOT NULL DEFAULT now(),
    "isDeleted"         boolean NOT NULL DEFAULT false,
    "isDeletedOn"       timestamptz,
    "wipeReason"        text,                                       -- 'Manual' (ganti mode) / 'LostStreak' (wipe rutin), NULL kalau belum pernah di-wipe
    CONSTRAINT "FK_tdailyrecord_muser" FOREIGN KEY ("userId") REFERENCES muser ("userPk"),
    CONSTRAINT "FK_tdailyrecord_calorieCategory" FOREIGN KEY ("calorieCategory") REFERENCES mparam ("paramPK"),
    CONSTRAINT "FK_tdailyrecord_createdVia" FOREIGN KEY ("createdVia") REFERENCES mparam ("paramPK")
);
CREATE UNIQUE INDEX "IX_tdailyrecord_userId_recordDate" ON tdailyrecord ("userId", "recordDate");

-- ============================================================
-- STATE: tstreak (was: streakstate)
-- ============================================================
CREATE TABLE tstreak (
    "streakPk"          bigserial PRIMARY KEY,
    "userId"            bigint NOT NULL,                           -- FK -> muser
    "currentStreak"     integer NOT NULL DEFAULT 0,
    "lastLoggedDate"    date,
    "isStreakFrozen"    boolean NOT NULL DEFAULT false,             -- beda konsep dari tfreeze, lihat komentar tfreeze
    "wipeReason"        text,                                       -- 'Manual' (ganti mode) / 'LostStreak' (wipe rutin), NULL kalau belum pernah di-wipe
    "modifiedOn"        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "FK_tstreak_muser" FOREIGN KEY ("userId") REFERENCES muser ("userPk")
);
CREATE UNIQUE INDEX "IX_tstreak_userId" ON tstreak ("userId");

-- ============================================================
-- STATE: tfreeze (was: freezestate)
-- Stok token freeze. isStreakFrozen di tstreak BEDA KONSEP dari tabel ini --
-- tstreak.isStreakFrozen = counter berhenti TOTAL (akun upgraded tanpa goal),
-- tfreeze = stok token buat nutup 1 hari bolong (dipakai via ConsumeStreakFreeze).
-- ============================================================
CREATE TABLE tfreeze (
    "freezePk"                      bigserial PRIMARY KEY,
    "userId"                        bigint NOT NULL,               -- FK -> muser
    "streakFreezeCount"             integer NOT NULL DEFAULT 0,    -- 0-2, +1 tiap 30 hari beruntun
    "wipeFreezeCount"               integer NOT NULL DEFAULT 0,    -- 0-2, +1 tiap 60 hari beruntun
    "daysSinceLastStreakFreeze"     integer NOT NULL DEFAULT 0,
    "daysSinceLastWipeFreeze"       integer NOT NULL DEFAULT 0,
    "lastStreakFreezeGainedDate"    date,
    "lastWipeFreezeGainedDate"      date,
    "wipeReason"                    text,                          -- 'Manual' (ganti mode) / 'LostStreak' (wipe rutin), NULL kalau belum pernah di-wipe
    "modifiedOn"                    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "FK_tfreeze_muser" FOREIGN KEY ("userId") REFERENCES muser ("userPk")
);
CREATE UNIQUE INDEX "IX_tfreeze_userId" ON tfreeze ("userId");

-- ============================================================
-- STATE: tnotification (was: notificationsubscription)
-- ============================================================
CREATE TABLE tnotification (
    "notificationPk"    bigserial PRIMARY KEY,
    "userId"            bigint NOT NULL,                           -- FK -> muser
    "fcmToken"          text,
    "modifiedOn"        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "FK_tnotification_muser" FOREIGN KEY ("userId") REFERENCES muser ("userPk")
);
CREATE UNIQUE INDEX "IX_tnotification_userId" ON tnotification ("userId");
