export type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';
export type RepeatFrequency = 'Daily' | 'Weekly' | 'Monthly';

export interface Task {
	id: string;
	goalId: string;
	parentTaskId: string | null;
	title: string;
	description: string | null;
	status: TaskStatus;
	dueDate: string | null;
	completedAt: string | null;
	isRepeatable: boolean;
	repeatSchedule: RepeatSchedule | null;
	sortOrder: number;
	subtasks: Task[];
	identityId: string | null;
	identityName: string | null;
	identityIcon: string | null;
	identityColor: string | null;
	createdAt: string;
	updatedAt: string;
}

export interface RepeatSchedule {
	frequency: RepeatFrequency;
	intervalValue: number;
	daysOfWeek: number[] | null;
	dayOfMonth: number | null;
	nextOccurrence: string | null;
}

export interface CreateTaskRequest {
	title: string;
	description?: string;
	dueDate?: string;
	isRepeatable?: boolean;
	repeatSchedule?: {
		frequency: RepeatFrequency;
		intervalValue?: number;
		daysOfWeek?: number[];
		dayOfMonth?: number;
		startDate?: string;
		endDate?: string;
	};
	identityId?: string;
}

export interface UpdateTaskRequest {
	title: string;
	description?: string;
	dueDate?: string;
	isRepeatable?: boolean;
	repeatSchedule?: {
		frequency: RepeatFrequency;
		intervalValue?: number;
		daysOfWeek?: number[];
		dayOfMonth?: number;
		startDate?: string;
		endDate?: string;
	};
	identityId?: string;
}
