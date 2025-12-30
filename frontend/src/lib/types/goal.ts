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
	createdAt: string;
	updatedAt: string;
}

export interface CreateGoalRequest {
	title: string;
	description?: string;
	targetDate?: string;
}

export interface UpdateGoalRequest {
	title: string;
	description?: string;
	targetDate?: string;
}
