export interface JournalEntry {
	id: string;
	title: string;
	description: string | null;
	entryDate: string;
	habitStackId: string | null;
	habitStackName: string | null;
	taskItemId: string | null;
	taskItemTitle: string | null;
	authorUserId: string | null;
	authorDisplayName: string | null;
	images: JournalImage[];
	reactions: JournalReaction[];
	createdAt: string;
	updatedAt: string;
}

export interface JournalImage {
	id: string;
	fileName: string;
	url: string;
	sortOrder: number;
}

export interface JournalReaction {
	id: string;
	emoji: string;
	userId: string;
	userDisplayName: string;
	createdAt: string;
}

export interface JournalReactionSummary {
	emoji: string;
	count: number;
	userIds: string[];
	hasReacted: boolean;
}

export interface CreateJournalEntryRequest {
	title: string;
	description?: string;
	entryDate: string;
	habitStackId?: string;
	taskItemId?: string;
}

export interface UpdateJournalEntryRequest {
	title: string;
	description?: string;
	entryDate: string;
	habitStackId?: string;
	taskItemId?: string;
}

export interface LinkableHabitStack {
	id: string;
	name: string;
}

export interface LinkableTask {
	id: string;
	title: string;
	goalTitle: string;
}
