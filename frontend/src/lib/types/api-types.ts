/**
 * Type aliases mapping friendly frontend names to OpenAPI-generated types.
 * Re-exports from generated/api.d.ts with names matching existing frontend conventions.
 *
 * Generated types come from: npm run generate:types
 * Source spec: openapi/v1.json (fetched from backend via npm run generate:spec)
 */

// ===== Auth =====
export type { UserResponse as User } from './generated/api';
export type { LoginRequest } from './generated/api';
export type { RegisterRequest } from './generated/api';
export type { UpdateProfileRequest } from './generated/api';
export type { ChangePasswordRequest } from './generated/api';
export type { UpdateMembershipRequest } from './generated/api';
export type { UpdateLanguageRequest } from './generated/api';
export type { BuddyLoginResponse } from './generated/api';

// Auth enums (frontend-only string literals, not generated)
export type MembershipTier = 'Free' | 'Plus' | 'Pro';
export type UserRole = 'User' | 'Admin';
export type Language = 'English' | 'Danish';

// ===== Goals =====
export type { GoalResponse as Goal } from './generated/api';
export type { CreateGoalRequest } from './generated/api';
export type { UpdateGoalRequest } from './generated/api';

// ===== Tasks =====
export type { TaskResponse as Task } from './generated/api';
export type { CreateTaskRequest } from './generated/api';
export type { UpdateTaskRequest } from './generated/api';
export type { PostponeTaskRequest } from './generated/api';
export type { CompleteMultipleTasksRequest } from './generated/api';
export type { CompleteMultipleTasksResponse } from './generated/api';

// Task enums (frontend-only string literals)
export type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';
export type RepeatFrequency = 'Daily' | 'Weekly' | 'Monthly';

// ===== Identities =====
export type { IdentityResponse as Identity } from './generated/api';
export type { IdentityStatsResponse as IdentityStats } from './generated/api';
export type { CreateIdentityRequest } from './generated/api';
export type { UpdateIdentityRequest } from './generated/api';

// ===== Identity Proofs =====
export type { IdentityProofResponse as IdentityProof } from './generated/api';
export type { CreateIdentityProofRequest } from './generated/api';

// ProofIntensity re-exported from generated (string enum)
export type { ProofIntensity } from './generated/api';

// ===== Habit Stacks =====
export type { HabitStackResponse as HabitStack } from './generated/api';
export type { HabitStackItemResponse as HabitStackItem } from './generated/api';
export type { CreateHabitStackRequest } from './generated/api';
export type { UpdateHabitStackRequest } from './generated/api';
export type { HabitStackItemRequest } from './generated/api';
export type { AddStackItemRequest } from './generated/api';
export type { ReorderStackItemsRequest } from './generated/api';
export type { ReorderHabitStacksRequest } from './generated/api';
export type { HabitStackItemCompletionResponse } from './generated/api';
export type { CompleteAllResponse } from './generated/api';

// ===== Today View =====
export type { TodayViewResponse as TodayView } from './generated/api';
export type { TodayTaskResponse as TodayTask } from './generated/api';
export type { TodayHabitStackResponse as TodayHabitStack } from './generated/api';
export type { TodayHabitStackItemResponse as TodayHabitStackItem } from './generated/api';
export type { TodayIdentityFeedbackResponse as IdentityFeedback } from './generated/api';
export type { IdentityProgressResponse as IdentityProgress } from './generated/api';

// Today enums (frontend-only string literals)
export type IdentityStatus = 'Dormant' | 'Forming' | 'Emerging' | 'Stabilizing' | 'Strong' | 'Automatic';
export type TrendDirection = 'Up' | 'Down' | 'Neutral';

// ===== Analytics =====
export type { TaskStreakResponse as TaskStreak } from './generated/api';
export type { StreakSummaryResponse as StreakSummary } from './generated/api';
export type { CompletionRateResponse as CompletionRate } from './generated/api';
export type { HeatmapDataResponse as HeatmapData } from './generated/api';

// ===== Journal =====
export type { JournalEntryResponse as JournalEntry } from './generated/api';
export type { JournalImageResponse as JournalImage } from './generated/api';
export type { JournalReactionResponse as JournalReaction } from './generated/api';
export type { CreateJournalEntryRequest } from './generated/api';
export type { UpdateJournalEntryRequest } from './generated/api';
export type { LinkableHabitStackResponse as LinkableHabitStack } from './generated/api';
export type { LinkableTaskResponse as LinkableTask } from './generated/api';

// JournalReactionSummary is frontend-only (computed from JournalReaction[])
export interface JournalReactionSummary {
	emoji: string;
	count: number;
	users: Array<{ id: string; displayName: string }>;
	hasReacted: boolean;
}

// ===== Daily Commitments =====
export type { DailyCommitmentResponse as DailyCommitment } from './generated/api';
export type { CreateDailyCommitmentRequest } from './generated/api';
export type { CommitmentOptionsResponse as CommitmentOptions } from './generated/api';
export type { IdentityOptionResponse as IdentityOption } from './generated/api';
export type { ActionSuggestion } from './generated/api';
export type { ActionSuggestionsResponse } from './generated/api';
export type { YesterdayCommitmentResponse as YesterdayCommitment } from './generated/api';

// DailyCommitmentStatus enum (generated as string enum)
export type { DailyCommitmentStatus } from './generated/api';

// ===== Notifications =====
export type { NotificationPreferencesResponse as NotificationPreferences } from './generated/api';
export type { UpdateNotificationPreferencesRequest } from './generated/api';

// Notification enums (frontend-only)
export type TimeSlot = 'Morning' | 'Afternoon' | 'Evening' | 'Night' | 'Custom';

export enum DayOfWeek {
	Sunday = 0,
	Monday = 1,
	Tuesday = 2,
	Wednesday = 3,
	Thursday = 4,
	Friday = 5,
	Saturday = 6
}

export const NotificationDays = {
	None: 0,
	Sunday: 1,
	Monday: 2,
	Tuesday: 4,
	Wednesday: 8,
	Thursday: 16,
	Friday: 32,
	Saturday: 64,
	Weekdays: 2 + 4 + 8 + 16 + 32,
	Weekends: 1 + 64,
	Weekend: 1 + 64,
	EveryDay: 1 + 2 + 4 + 8 + 16 + 32 + 64,
	All: 1 + 2 + 4 + 8 + 16 + 32 + 64
} as const;

// ===== Buddies =====
export type { BuddyResponse } from './generated/api';
export type { BuddyForResponse } from './generated/api';
export type { BuddyRelationshipsResponse } from './generated/api';
export type { BuddyTodayViewResponse } from './generated/api';
export type { BuddyJournalEntryResponse as BuddyJournalEntry } from './generated/api';
export type { BuddyJournalImageResponse as BuddyJournalImage } from './generated/api';
export type { BuddyJournalReactionResponse as BuddyJournalReaction } from './generated/api';
export type { CreateBuddyJournalEntryRequest } from './generated/api';

// Buddy-specific today types alias to existing generated today types
export type { TodayHabitStackResponse as BuddyTodayHabitStack } from './generated/api';
export type { TodayHabitStackItemResponse as BuddyTodayHabitStackItem } from './generated/api';
export type { TodayTaskResponse as BuddyTodayTask } from './generated/api';
export type { TodayIdentityFeedbackResponse as BuddyIdentityFeedback } from './generated/api';

// ===== Milestones =====
export type { MilestoneDefinitionResponse as MilestoneDefinition } from './generated/api';
export type { UserMilestoneResponse as UserMilestone } from './generated/api';
export type { UserStatsResponse as UserStats } from './generated/api';
export type { MarkSeenRequest } from './generated/api';
export type { CreateMilestoneRequest } from './generated/api';
export type { UpdateMilestoneRequest } from './generated/api';
export type { ToggleMilestoneRequest } from './generated/api';

// ===== Admin =====
export type { AdminStatsResponse as AdminStats } from './generated/api';
export type { MembershipStats } from './generated/api';
export type { TaskTotals } from './generated/api';
export type { DailyStatsResponse as DailyStats } from './generated/api';
export type { AdminUserResponse as AdminUser } from './generated/api';
export type { UpdateRoleRequest } from './generated/api';
export type { UserActivityPeriod } from './generated/api';
export type { UserActivityResponse as UserActivity } from './generated/api';
export type { AiUsageStatsResponse as AiUsageStats } from './generated/api';
export type { AiUsageLogResponse as AiUsageLog } from './generated/api';
export type { PaginatedResponseOfAiUsageLogResponse } from './generated/api';
export type { AnalyticsOverviewResponse } from './generated/api';
export type { EventTypeCount } from './generated/api';
export type { DailyEventCount } from './generated/api';
export type { SessionSummary } from './generated/api';
export type { SignupSettingsResponse } from './generated/api';
export type { UserPushStatus } from './generated/api';
export type { PushNotificationResult } from './generated/api';
export type { PushNotificationStatsResponse as PushStats } from './generated/api';

// Generic paginated response (frontend convenience type)
export interface PaginatedResponse<T> {
	items: T[];
	totalCount: number;
	page: number;
	pageSize: number;
	totalPages: number;
}

// ===== Waitlist =====
export type { WaitlistEntryResponse as WaitlistEntry } from './generated/api';
export type { WhitelistEntryResponse as WhitelistEntry } from './generated/api';
export type { WhitelistCheckResponse } from './generated/api';

// WaitlistSignupResponse - controller returns IActionResult, not in generated schema
export interface WaitlistSignupResponse {
	message: string;
	canSignup?: boolean;
}
