using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMotivateMe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    MembershipTier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Free"),
                    HasCompletedOnboarding = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "User")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "waitlist_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waitlist_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "accountability_buddies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuddyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accountability_buddies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accountability_buddies_users_BuddyUserId",
                        column: x => x.BuddyUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accountability_buddies_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_usage_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    audio_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    estimated_cost_usd = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    request_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_usage_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "buddy_invite_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InviterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    BuddyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buddy_invite_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_buddy_invite_tokens_users_BuddyUserId",
                        column: x => x.BuddyUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_buddy_invite_tokens_users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_login_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_login_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_login_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goals_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_identities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_external_logins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_external_logins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_external_logins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "whitelist_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whitelist_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_whitelist_entries_users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "habit_stacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IdentityId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerCue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habit_stacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_habit_stacks_identities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "identities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_habit_stacks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompletedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IdentityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsTinyHabit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FullVersionTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_items_goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_items_identities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "identities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_task_items_task_items_FullVersionTaskId",
                        column: x => x.FullVersionTaskId,
                        principalTable: "task_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_task_items_task_items_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "task_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "habit_stack_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    HabitStackId = table.Column<Guid>(type: "uuid", nullable: false),
                    CueDescription = table.Column<string>(type: "text", nullable: false),
                    HabitDescription = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastCompletedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habit_stack_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_habit_stack_items_habit_stacks_HabitStackId",
                        column: x => x.HabitStackId,
                        principalTable: "habit_stacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HabitStackId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.Id);
                    table.CheckConstraint("CK_JournalEntry_SingleLink", "\"HabitStackId\" IS NULL OR \"TaskItemId\" IS NULL");
                    table.ForeignKey(
                        name: "FK_journal_entries_habit_stacks_HabitStackId",
                        column: x => x.HabitStackId,
                        principalTable: "habit_stacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_journal_entries_task_items_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "task_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_journal_entries_users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_journal_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "habit_stack_item_completions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    HabitStackItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habit_stack_item_completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_habit_stack_item_completions_habit_stack_items_HabitStackIt~",
                        column: x => x.HabitStackItemId,
                        principalTable: "habit_stack_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "journal_images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    S3Key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_journal_images_journal_entries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "journal_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accountability_buddies_BuddyUserId",
                table: "accountability_buddies",
                column: "BuddyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_accountability_buddies_UserId",
                table: "accountability_buddies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_accountability_buddies_UserId_BuddyUserId",
                table: "accountability_buddies",
                columns: new[] { "UserId", "BuddyUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_logs_created_at",
                table: "ai_usage_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_ai_usage_logs_user_id",
                table: "ai_usage_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_buddy_invite_tokens_BuddyUserId",
                table: "buddy_invite_tokens",
                column: "BuddyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_buddy_invite_tokens_InvitedEmail",
                table: "buddy_invite_tokens",
                column: "InvitedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_buddy_invite_tokens_InviterUserId",
                table: "buddy_invite_tokens",
                column: "InviterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_buddy_invite_tokens_Token",
                table: "buddy_invite_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_login_tokens_Email",
                table: "email_login_tokens",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_email_login_tokens_Token",
                table: "email_login_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_login_tokens_UserId",
                table: "email_login_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_goals_UserId",
                table: "goals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_habit_stack_item_completions_HabitStackItemId",
                table: "habit_stack_item_completions",
                column: "HabitStackItemId");

            migrationBuilder.CreateIndex(
                name: "IX_habit_stack_item_completions_HabitStackItemId_CompletedDate",
                table: "habit_stack_item_completions",
                columns: new[] { "HabitStackItemId", "CompletedDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_habit_stack_items_HabitStackId",
                table: "habit_stack_items",
                column: "HabitStackId");

            migrationBuilder.CreateIndex(
                name: "IX_habit_stacks_IdentityId",
                table: "habit_stacks",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_habit_stacks_UserId",
                table: "habit_stacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_identities_UserId",
                table: "identities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_AuthorUserId",
                table: "journal_entries",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_EntryDate",
                table: "journal_entries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_HabitStackId",
                table: "journal_entries",
                column: "HabitStackId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_TaskItemId",
                table: "journal_entries",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_UserId",
                table: "journal_entries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_images_JournalEntryId",
                table: "journal_images",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_task_items_FullVersionTaskId",
                table: "task_items",
                column: "FullVersionTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_task_items_GoalId",
                table: "task_items",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_task_items_IdentityId",
                table: "task_items",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_task_items_ParentTaskId",
                table: "task_items",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_user_external_logins_Provider_ProviderKey",
                table: "user_external_logins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_external_logins_UserId",
                table: "user_external_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_waitlist_entries_Email",
                table: "waitlist_entries",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whitelist_entries_AddedByUserId",
                table: "whitelist_entries",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_whitelist_entries_Email",
                table: "whitelist_entries",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accountability_buddies");

            migrationBuilder.DropTable(
                name: "ai_usage_logs");

            migrationBuilder.DropTable(
                name: "buddy_invite_tokens");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "email_login_tokens");

            migrationBuilder.DropTable(
                name: "habit_stack_item_completions");

            migrationBuilder.DropTable(
                name: "journal_images");

            migrationBuilder.DropTable(
                name: "user_external_logins");

            migrationBuilder.DropTable(
                name: "waitlist_entries");

            migrationBuilder.DropTable(
                name: "whitelist_entries");

            migrationBuilder.DropTable(
                name: "habit_stack_items");

            migrationBuilder.DropTable(
                name: "journal_entries");

            migrationBuilder.DropTable(
                name: "habit_stacks");

            migrationBuilder.DropTable(
                name: "task_items");

            migrationBuilder.DropTable(
                name: "goals");

            migrationBuilder.DropTable(
                name: "identities");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
