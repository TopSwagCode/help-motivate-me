export interface Goal {
	id: string;
	title: string;
	description: string | null;
	targetDate: string | null;
	isCompleted: boolean;
	completedAt: string | null;
	sortOrder: number;
	taskCount: number;
	completedTaskCount: number;
	identityId: string | null;
	identityName: string | null;
	identityColor: string | null;
	identityIcon: string | null;
	createdAt: string;
	updatedAt: string;
}

export interface CreateGoalRequest {
	title: string;
	description?: string;
	targetDate?: string;
	identityId?: string;
}

export interface UpdateGoalRequest {
	title: string;
	description?: string;
	targetDate?: string;
	identityId?: string;
}
