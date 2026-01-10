CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE repeat_schedules (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "Frequency" character varying(20) NOT NULL,
        "IntervalValue" integer NOT NULL DEFAULT 1,
        "DaysOfWeek" integer[],
        "DayOfMonth" integer,
        "StartDate" date NOT NULL,
        "EndDate" date,
        "NextOccurrence" date,
        "LastCompleted" timestamp with time zone,
        CONSTRAINT "PK_repeat_schedules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE users (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "Username" character varying(50) NOT NULL,
        "Email" character varying(255) NOT NULL,
        "PasswordHash" character varying(255),
        "DisplayName" character varying(100),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE categories (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Color" character varying(7),
        "Icon" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_categories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_categories_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE goals (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Title" character varying(255) NOT NULL,
        "Description" text,
        "TargetDate" date,
        "IsCompleted" boolean NOT NULL DEFAULT FALSE,
        "CompletedAt" timestamp with time zone,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_goals" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_goals_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE identities (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" text,
        "Color" character varying(20),
        "Icon" character varying(50),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_identities" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_identities_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE user_external_logins (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Provider" character varying(50) NOT NULL,
        "ProviderKey" character varying(255) NOT NULL,
        "ProviderDisplayName" character varying(255),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_user_external_logins" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_external_logins_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE goal_categories (
        category_id uuid NOT NULL,
        goal_id uuid NOT NULL,
        CONSTRAINT "PK_goal_categories" PRIMARY KEY (category_id, goal_id),
        CONSTRAINT "FK_goal_categories_categories_category_id" FOREIGN KEY (category_id) REFERENCES categories ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_goal_categories_goals_goal_id" FOREIGN KEY (goal_id) REFERENCES goals ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE habit_stacks (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" text,
        "IdentityId" uuid,
        "TriggerCue" character varying(255),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_habit_stacks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_habit_stacks_identities_IdentityId" FOREIGN KEY ("IdentityId") REFERENCES identities ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_habit_stacks_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE task_items (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "GoalId" uuid NOT NULL,
        "ParentTaskId" uuid,
        "Title" character varying(255) NOT NULL,
        "Description" text,
        "Status" character varying(20) NOT NULL DEFAULT 'Pending',
        "DueDate" date,
        "CompletedAt" timestamp with time zone,
        "IsRepeatable" boolean NOT NULL DEFAULT FALSE,
        "RepeatScheduleId" uuid,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "IdentityId" uuid,
        "EstimatedMinutes" integer,
        "IsTinyHabit" boolean NOT NULL DEFAULT FALSE,
        "FullVersionTaskId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_task_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_task_items_goals_GoalId" FOREIGN KEY ("GoalId") REFERENCES goals ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_task_items_identities_IdentityId" FOREIGN KEY ("IdentityId") REFERENCES identities ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_task_items_repeat_schedules_RepeatScheduleId" FOREIGN KEY ("RepeatScheduleId") REFERENCES repeat_schedules ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_task_items_task_items_FullVersionTaskId" FOREIGN KEY ("FullVersionTaskId") REFERENCES task_items ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_task_items_task_items_ParentTaskId" FOREIGN KEY ("ParentTaskId") REFERENCES task_items ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE habit_stack_items (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "HabitStackId" uuid NOT NULL,
        "CueDescription" text NOT NULL,
        "HabitDescription" text NOT NULL,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "CurrentStreak" integer NOT NULL DEFAULT 0,
        "LongestStreak" integer NOT NULL DEFAULT 0,
        "LastCompletedDate" date,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_habit_stack_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_habit_stack_items_habit_stacks_HabitStackId" FOREIGN KEY ("HabitStackId") REFERENCES habit_stacks ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE task_completions (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "TaskId" uuid NOT NULL,
        "CompletedDate" date NOT NULL,
        "CompletedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "WasGracePeriod" boolean NOT NULL DEFAULT FALSE,
        CONSTRAINT "PK_task_completions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_task_completions_task_items_TaskId" FOREIGN KEY ("TaskId") REFERENCES task_items ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE TABLE habit_stack_item_completions (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "HabitStackItemId" uuid NOT NULL,
        "CompletedDate" date NOT NULL,
        "CompletedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_habit_stack_item_completions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_habit_stack_item_completions_habit_stack_items_HabitStackIt~" FOREIGN KEY ("HabitStackItemId") REFERENCES habit_stack_items ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_categories_UserId" ON categories ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_categories_UserId_Name" ON categories ("UserId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_goal_categories_goal_id" ON goal_categories (goal_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_goals_UserId" ON goals ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_habit_stack_item_completions_HabitStackItemId" ON habit_stack_item_completions ("HabitStackItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_habit_stack_item_completions_HabitStackItemId_CompletedDate" ON habit_stack_item_completions ("HabitStackItemId", "CompletedDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_habit_stack_items_HabitStackId" ON habit_stack_items ("HabitStackId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_habit_stacks_IdentityId" ON habit_stacks ("IdentityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_habit_stacks_UserId" ON habit_stacks ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_identities_UserId" ON identities ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_task_completions_TaskId_CompletedDate" ON task_completions ("TaskId", "CompletedDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_task_items_FullVersionTaskId" ON task_items ("FullVersionTaskId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_task_items_GoalId" ON task_items ("GoalId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_task_items_IdentityId" ON task_items ("IdentityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_task_items_ParentTaskId" ON task_items ("ParentTaskId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_task_items_RepeatScheduleId" ON task_items ("RepeatScheduleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_user_external_logins_Provider_ProviderKey" ON user_external_logins ("Provider", "ProviderKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE INDEX "IX_user_external_logins_UserId" ON user_external_logins ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_Username" ON users ("Username");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251228234418_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251228234418_InitialCreate', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251229152229_RemoveTaskCompletion') THEN
    DROP TABLE task_completions;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251229152229_RemoveTaskCompletion') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251229152229_RemoveTaskCompletion', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251229162238_ChangeCompletedAtToDateOnly') THEN
    ALTER TABLE task_items ALTER COLUMN "CompletedAt" TYPE date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251229162238_ChangeCompletedAtToDateOnly') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251229162238_ChangeCompletedAtToDateOnly', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251229203657_AddHabitStackSortOrder') THEN
    ALTER TABLE habit_stacks ADD "SortOrder" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251229203657_AddHabitStackSortOrder') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251229203657_AddHabitStackSortOrder', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230091335_AddEmailLoginTokens') THEN
    CREATE TABLE email_login_tokens (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Token" character varying(64) NOT NULL,
        "Email" character varying(255) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UsedAt" timestamp with time zone,
        CONSTRAINT "PK_email_login_tokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_email_login_tokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230091335_AddEmailLoginTokens') THEN
    CREATE INDEX "IX_email_login_tokens_Email" ON email_login_tokens ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230091335_AddEmailLoginTokens') THEN
    CREATE UNIQUE INDEX "IX_email_login_tokens_Token" ON email_login_tokens ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230091335_AddEmailLoginTokens') THEN
    CREATE INDEX "IX_email_login_tokens_UserId" ON email_login_tokens ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230091335_AddEmailLoginTokens') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251230091335_AddEmailLoginTokens', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230131453_AddJournalFeature') THEN
    CREATE TABLE journal_entries (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "Title" character varying(255) NOT NULL,
        "Description" text,
        "EntryDate" date NOT NULL,
        "HabitStackId" uuid,
        "TaskItemId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_journal_entries" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_JournalEntry_SingleLink" CHECK ("HabitStackId" IS NULL OR "TaskItemId" IS NULL),
        CONSTRAINT "FK_journal_entries_habit_stacks_HabitStackId" FOREIGN KEY ("HabitStackId") REFERENCES habit_stacks ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_journal_entries_task_items_TaskItemId" FOREIGN KEY ("TaskItemId") REFERENCES task_items ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_journal_entries_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230131453_AddJournalFeature') THEN
    CREATE TABLE journal_images (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "JournalEntryId" uuid NOT NULL,
        "FileName" character varying(255) NOT NULL,
        "S3Key" character varying(512) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "FileSizeBytes" bigint NOT NULL,
        "SortOrder" integer NOT NULL DEFAULT 0,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_journal_images" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_journal_images_journal_entries_JournalEntryId" FOREIGN KEY ("JournalEntryId") REFERENCES journal_entries ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230131453_AddJournalFeature') THEN
    CREATE INDEX "IX_journal_entries_EntryDate" ON journal_entries ("EntryDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230131453_AddJournalFeature') THEN
    CREATE INDEX "IX_journal_entries_HabitStackId" ON journal_entries ("HabitStackId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230131453_AddJournalFeature') THEN
    CREATE INDEX "IX_journal_entries_TaskItemId" ON journal_entries ("TaskItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230131453_AddJournalFeature') THEN
    CREATE INDEX "IX_journal_entries_UserId" ON journal_entries ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230131453_AddJournalFeature') THEN
    CREATE INDEX "IX_journal_images_JournalEntryId" ON journal_images ("JournalEntryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230131453_AddJournalFeature') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251230131453_AddJournalFeature', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230201031_RemoveCategories') THEN
    DROP TABLE goal_categories;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230201031_RemoveCategories') THEN
    DROP TABLE categories;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251230201031_RemoveCategories') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251230201031_RemoveCategories', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102084338_RemoveTaskIsRepeatable') THEN
    ALTER TABLE task_items DROP CONSTRAINT "FK_task_items_repeat_schedules_RepeatScheduleId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102084338_RemoveTaskIsRepeatable') THEN
    DROP TABLE repeat_schedules;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102084338_RemoveTaskIsRepeatable') THEN
    DROP INDEX "IX_task_items_RepeatScheduleId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102084338_RemoveTaskIsRepeatable') THEN
    ALTER TABLE task_items DROP COLUMN "RepeatScheduleId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102084338_RemoveTaskIsRepeatable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260102084338_RemoveTaskIsRepeatable', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102084902_UpdateTaskModel') THEN
    ALTER TABLE task_items DROP COLUMN "IsRepeatable";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102084902_UpdateTaskModel') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260102084902_UpdateTaskModel', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102124636_AddMembershipTier') THEN
    ALTER TABLE users ADD "MembershipTier" character varying(20) NOT NULL DEFAULT 'Free';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102124636_AddMembershipTier') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260102124636_AddMembershipTier', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102174002_AddHasCompletedOnboarding') THEN
    ALTER TABLE users ADD "HasCompletedOnboarding" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102174002_AddHasCompletedOnboarding') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260102174002_AddHasCompletedOnboarding', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102192713_AddUserRole') THEN
    ALTER TABLE users ADD "Role" character varying(20) NOT NULL DEFAULT 'User';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260102192713_AddUserRole') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260102192713_AddUserRole', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    ALTER TABLE journal_entries ADD "AuthorUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE TABLE accountability_buddies (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "UserId" uuid NOT NULL,
        "BuddyUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_accountability_buddies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_accountability_buddies_users_BuddyUserId" FOREIGN KEY ("BuddyUserId") REFERENCES users ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_accountability_buddies_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE TABLE buddy_invite_tokens (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "Token" character varying(64) NOT NULL,
        "InviterUserId" uuid NOT NULL,
        "InvitedEmail" character varying(255) NOT NULL,
        "BuddyUserId" uuid NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "UsedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_buddy_invite_tokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_buddy_invite_tokens_users_BuddyUserId" FOREIGN KEY ("BuddyUserId") REFERENCES users ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_buddy_invite_tokens_users_InviterUserId" FOREIGN KEY ("InviterUserId") REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE INDEX "IX_journal_entries_AuthorUserId" ON journal_entries ("AuthorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE INDEX "IX_accountability_buddies_BuddyUserId" ON accountability_buddies ("BuddyUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE INDEX "IX_accountability_buddies_UserId" ON accountability_buddies ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE UNIQUE INDEX "IX_accountability_buddies_UserId_BuddyUserId" ON accountability_buddies ("UserId", "BuddyUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE INDEX "IX_buddy_invite_tokens_BuddyUserId" ON buddy_invite_tokens ("BuddyUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE INDEX "IX_buddy_invite_tokens_InvitedEmail" ON buddy_invite_tokens ("InvitedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE INDEX "IX_buddy_invite_tokens_InviterUserId" ON buddy_invite_tokens ("InviterUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    CREATE UNIQUE INDEX "IX_buddy_invite_tokens_Token" ON buddy_invite_tokens ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    ALTER TABLE journal_entries ADD CONSTRAINT "FK_journal_entries_users_AuthorUserId" FOREIGN KEY ("AuthorUserId") REFERENCES users ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260103122646_AddAccountabilityBuddyFeature') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260103122646_AddAccountabilityBuddyFeature', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104051547_AddWaitlistAndWhitelist') THEN
    CREATE TABLE waitlist_entries (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "Email" character varying(255) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_waitlist_entries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104051547_AddWaitlistAndWhitelist') THEN
    CREATE TABLE whitelist_entries (
        "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
        "Email" character varying(255) NOT NULL,
        "AddedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "AddedByUserId" uuid,
        "InvitedAt" timestamp with time zone,
        CONSTRAINT "PK_whitelist_entries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_whitelist_entries_users_AddedByUserId" FOREIGN KEY ("AddedByUserId") REFERENCES users ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104051547_AddWaitlistAndWhitelist') THEN
    CREATE UNIQUE INDEX "IX_waitlist_entries_Email" ON waitlist_entries ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104051547_AddWaitlistAndWhitelist') THEN
    CREATE INDEX "IX_whitelist_entries_AddedByUserId" ON whitelist_entries ("AddedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104051547_AddWaitlistAndWhitelist') THEN
    CREATE UNIQUE INDEX "IX_whitelist_entries_Email" ON whitelist_entries ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104051547_AddWaitlistAndWhitelist') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260104051547_AddWaitlistAndWhitelist', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104101824_AddAiUsageLog') THEN
    CREATE TABLE ai_usage_logs (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        model character varying(50) NOT NULL,
        input_tokens integer NOT NULL,
        output_tokens integer NOT NULL,
        audio_duration_seconds integer,
        estimated_cost_usd numeric(10,6) NOT NULL,
        request_type character varying(50) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ai_usage_logs" PRIMARY KEY (id),
        CONSTRAINT "FK_ai_usage_logs_users_user_id" FOREIGN KEY (user_id) REFERENCES users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104101824_AddAiUsageLog') THEN
    CREATE INDEX "IX_ai_usage_logs_created_at" ON ai_usage_logs (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104101824_AddAiUsageLog') THEN
    CREATE INDEX "IX_ai_usage_logs_user_id" ON ai_usage_logs (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260104101824_AddAiUsageLog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260104101824_AddAiUsageLog', '10.0.0');
    END IF;
END $EF$;
COMMIT;

