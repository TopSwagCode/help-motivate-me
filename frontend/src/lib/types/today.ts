import type { DailyCommitment, YesterdayCommitment } from './dailyCommitment';

export interface TodayView {
	date: string;
	habitStacks: TodayHabitStack[];
	upcomingTasks: TodayTask[];
	completedTasks: TodayTask[];
	identityFeedback: IdentityFeedback[];
	identityProgress: IdentityProgress[];
	dailyCommitment: DailyCommitment | null;
	yesterdayCommitment: YesterdayCommitment;
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
	identityColor: string | null;
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
	identityIcon: string | null;
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
	totalVotes: number;
	habitVotes: number;
	stackBonusVotes: number;
	taskVotes: number;
	proofVotes: number;
	reinforcementMessage: string;
}

export interface IdentityProgress {
	id: string;
	name: string;
	color: string | null;
	icon: string | null;
	score: number;
	status: IdentityStatus;
	trend: TrendDirection;
	accountAgeDays: number;
	showNumericScore: boolean;
	todayVotes: number;
}

export type IdentityStatus = 'Dormant' | 'Forming' | 'Emerging' | 'Stabilizing' | 'Strong' | 'Automatic';
export type TrendDirection = 'Up' | 'Down' | 'Neutral';
