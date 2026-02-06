export interface TaskStreak {
	taskId: string;
	taskTitle: string;
	currentStreak: number;
	longestStreak: number;
	lastCompletedDate: string | null;
	isOnGracePeriod: boolean;
	daysUntilStreakBreaks: number;
}

export interface StreakSummary {
	totalHabits: number;
	activeStreaks: number;
	longestActiveStreak: number;
	streaks: TaskStreak[];
}

export interface CompletionRate {
	dailyRate: number;
	weeklyRate: number;
	monthlyRate: number;
	totalCompletions: number;
	missedDays: number;
}

export interface HeatmapData {
	date: string;
	count: number;
}
