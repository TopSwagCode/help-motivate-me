export interface Identity {
	id: string;
	name: string;
	description: string | null;
	color: string | null;
	icon: string | null;
	totalTasks: number;
	completedTasks: number;
	tasksCompletedLast7Days: number;
	completionRate: number;
	createdAt: string;
}

export interface IdentityStats {
	id: string;
	name: string;
	totalCompletions: number;
	currentStreak: number;
	weeklyCompletions: number;
	reinforcementMessage: string;
}

export interface CreateIdentityRequest {
	name: string;
	description?: string;
	color?: string;
	icon?: string;
}

export interface UpdateIdentityRequest {
	name: string;
	description?: string;
	color?: string;
	icon?: string;
}
