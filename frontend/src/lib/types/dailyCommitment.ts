export type DailyCommitmentStatus = 'Committed' | 'Completed' | 'Dismissed' | 'Missed';

export interface DailyCommitment {
	id: string;
	commitmentDate: string;
	identityId: string;
	identityName: string;
	identityColor: string | null;
	identityIcon: string | null;
	actionDescription: string;
	linkedHabitStackItemId: string | null;
	linkedTaskId: string | null;
	status: DailyCommitmentStatus;
	completedAt: string | null;
	createdAt: string;
}

export interface IdentityOption {
	id: string;
	name: string;
	color: string | null;
	icon: string | null;
	score: number;
	isRecommended: boolean;
}

export interface CommitmentOptions {
	identities: IdentityOption[];
	recommendedIdentityId: string | null;
	defaultMode: string;
}

export interface ActionSuggestion {
	description: string;
	type: 'habit' | 'task';
	habitStackItemId: string | null;
	taskId: string | null;
}

export interface ActionSuggestionsResponse {
	suggestions: ActionSuggestion[];
}

export interface YesterdayCommitment {
	wasMissed: boolean;
	identityName: string | null;
	actionDescription: string | null;
}

export interface CreateDailyCommitmentRequest {
	identityId: string;
	actionDescription: string;
	linkedHabitStackItemId?: string | null;
	linkedTaskId?: string | null;
}
