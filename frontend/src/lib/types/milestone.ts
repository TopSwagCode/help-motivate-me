export interface MilestoneDefinition {
	id: string;
	code: string;
	titleKey: string;
	descriptionKey: string;
	icon: string;
	triggerEvent: string;
	ruleType: string;
	ruleData: string;
	animationType: string;
	animationData: string | null;
	sortOrder: number;
	isActive: boolean;
}

export interface UserMilestone {
	id: string;
	milestoneDefinitionId: string;
	code: string;
	titleKey: string;
	descriptionKey: string;
	icon: string;
	animationType: string;
	animationData: string | null;
	awardedAt: string;
	hasBeenSeen: boolean;
}

export interface UserStats {
	loginCount: number;
	totalWins: number;
	totalHabitsCompleted: number;
	totalTasksCompleted: number;
	totalIdentityProofs: number;
	totalJournalEntries: number;
	lastLoginAt: string | null;
	lastActivityAt: string | null;
}

export interface MarkSeenRequest {
	milestoneIds: string[];
}

export interface CreateMilestoneRequest {
	code: string;
	titleKey: string;
	descriptionKey: string;
	icon: string;
	triggerEvent: string;
	ruleType: string;
	ruleData: string;
	animationType?: string;
	animationData?: string | null;
	sortOrder?: number;
	isActive?: boolean;
}

export interface UpdateMilestoneRequest {
	code: string;
	titleKey: string;
	descriptionKey: string;
	icon: string;
	triggerEvent: string;
	ruleType: string;
	ruleData: string;
	animationType: string;
	animationData: string | null;
	sortOrder: number;
	isActive: boolean;
}

export interface ToggleMilestoneRequest {
	isActive: boolean;
}
