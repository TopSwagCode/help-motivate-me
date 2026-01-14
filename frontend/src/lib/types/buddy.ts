export interface BuddyResponse {
	id: string;
	buddyUserId: string;
	buddyEmail: string;
	buddyDisplayName: string;
	createdAt: string;
}

export interface BuddyForResponse {
	id: string;
	userId: string;
	userEmail: string;
	userDisplayName: string;
	createdAt: string;
}

export interface BuddyRelationshipsResponse {
	myBuddies: BuddyResponse[];
	buddyingFor: BuddyForResponse[];
}

export interface BuddyTodayViewResponse {
	userId: string;
	userDisplayName: string;
	date: string;
	habitStacks: BuddyTodayHabitStack[];
	upcomingTasks: BuddyTodayTask[];
	completedTasks: BuddyTodayTask[];
	identityFeedback: BuddyIdentityFeedback[];
}

export interface BuddyTodayHabitStack {
	id: string;
	name: string;
	triggerCue: string | null;
	identityId: string | null;
	identityName: string | null;
	identityColor: string | null;
	items: BuddyTodayHabitStackItem[];
	completedCount: number;
	totalCount: number;
}

export interface BuddyTodayHabitStackItem {
	id: string;
	habitDescription: string;
	isCompletedToday: boolean;
	currentStreak: number;
}

export interface BuddyTodayTask {
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

export interface BuddyIdentityFeedback {
	id: string;
	name: string;
	color: string | null;
	icon: string | null;
	completionsToday: number;
	reinforcementMessage: string;
}

export interface BuddyJournalEntry {
	id: string;
	title: string;
	description: string | null;
	entryDate: string;
	authorUserId: string | null;
	authorDisplayName: string | null;
	images: BuddyJournalImage[];
	createdAt: string;
}

export interface BuddyJournalImage {
	id: string;
	fileName: string;
	url: string;
	sortOrder: number;
}

export interface CreateBuddyJournalEntryRequest {
	title: string;
	description?: string;
	entryDate?: string;
}

export interface BuddyLoginResponse {
	user: import('./index').User;
	inviterUserId: string;
	inviterDisplayName: string;
}
