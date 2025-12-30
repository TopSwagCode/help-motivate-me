export interface TodayView {
	date: string;
	habitStacks: TodayHabitStack[];
	upcomingTasks: TodayTask[];
	completedTasks: TodayTask[];
	identityFeedback: IdentityFeedback[];
}

export interface TodayTask {
	id: string;
	title: string;
	description: string | null;
	goalId: string;
	goalTitle: string;
	identityId: string | null;
	identityName: string | null;
	identityIcon: string | null;
	dueDate: string | null;
	status: string;
}

export interface TodayHabitStack {
	id: string;
	name: string;
	triggerCue: string | null;
	identityId: string | null;
	identityName: string | null;
	identityColor: string | null;
	items: TodayHabitStackItem[];
	completedCount: number;
	totalCount: number;
}

export interface TodayHabitStackItem {
	id: string;
	habitDescription: string;
	isCompletedToday: boolean;
	currentStreak: number;
}

export interface IdentityFeedback {
	id: string;
	name: string;
	color: string | null;
	icon: string | null;
	completionsToday: number;
	reinforcementMessage: string;
}
