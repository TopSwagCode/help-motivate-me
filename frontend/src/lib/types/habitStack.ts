export interface HabitStack {
	id: string;
	name: string;
	description: string | null;
	identityId: string | null;
	identityName: string | null;
	identityColor: string | null;
	triggerCue: string | null;
	isActive: boolean;
	items: HabitStackItem[];
	createdAt: string;
}

export interface HabitStackItem {
	id: string;
	cueDescription: string;
	habitDescription: string;
	sortOrder: number;
	currentStreak: number;
	longestStreak: number;
}

export interface CreateHabitStackRequest {
	name: string;
	description?: string;
	identityId?: string;
	triggerCue?: string;
	items?: HabitStackItemRequest[];
}

export interface UpdateHabitStackRequest {
	name: string;
	description?: string;
	identityId?: string;
	triggerCue?: string;
	isActive: boolean;
}

export interface HabitStackItemRequest {
	cueDescription: string;
	habitDescription: string;
}

export interface AddStackItemRequest {
	cueDescription: string;
	habitDescription: string;
}

export interface ReorderStackItemsRequest {
	itemIds: string[];
}

export interface HabitStackItemCompletionResponse {
	itemId: string;
	habitDescription: string;
	currentStreak: number;
	longestStreak: number;
	isCompleted: boolean;
}
