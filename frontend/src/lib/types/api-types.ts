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
export type { UpdateProfileRequest } from './generated/api';
export type { UpdateLanguageRequest } from './generated/api';
export type { AiStatusResponse as AiStatus } from './generated/api';

// Auth enums (frontend-only string literals, not generated)
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
export type { HabitStackDays } from './generated/api';

// ===== Today View =====
export type { TodayViewResponse as TodayView } from './generated/api';
export type { TodayTaskResponse as TodayTask } from './generated/api';
export type { TodayHabitStackResponse as TodayHabitStack } from './generated/api';
export type { TodayHabitStackItemResponse as TodayHabitStackItem } from './generated/api';
export type { TodayIdentityFeedbackResponse as IdentityFeedback } from './generated/api';
export type { IdentityProgressResponse as IdentityProgress } from './generated/api';

export type { DailyDigestResponse as DailyDigest } from './generated/api';
export type { DailyDigestIdentityResponse as DigestIdentity } from './generated/api';

// Today enums (frontend-only string literals)
export type IdentityStatus =
	| 'Dormant'
	| 'Forming'
	| 'Emerging'
	| 'Stabilizing'
	| 'Strong'
	| 'Automatic';
export type TrendDirection = 'Up' | 'Down' | 'Neutral';

// ===== Analytics =====
export type { CompletionRateResponse as CompletionRate } from './generated/api';
export type { HeatmapDataResponse as HeatmapData } from './generated/api';

// ===== Journal =====
export type { JournalEntryResponse as JournalEntry } from './generated/api';
export type { JournalImageResponse as JournalImage } from './generated/api';
export type { CreateJournalEntryRequest } from './generated/api';
export type { UpdateJournalEntryRequest } from './generated/api';
export type { LinkableHabitStackResponse as LinkableHabitStack } from './generated/api';
export type { LinkableTaskResponse as LinkableTask } from './generated/api';

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

// ===== Milestones =====
export type { MilestoneDefinitionResponse as MilestoneDefinition } from './generated/api';
export type { UserMilestoneResponse as UserMilestone } from './generated/api';
export type { UserStatsResponse as UserStats } from './generated/api';
export type { MarkSeenRequest } from './generated/api';
export type { CreateMilestoneRequest } from './generated/api';
export type { UpdateMilestoneRequest } from './generated/api';
export type { ToggleMilestoneRequest } from './generated/api';
