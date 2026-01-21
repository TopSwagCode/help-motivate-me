import { apiGet, apiPost, apiPut, apiDelete, apiUpload } from './client';
import type {
	JournalEntry,
	JournalImage,
	JournalReaction,
	CreateJournalEntryRequest,
	UpdateJournalEntryRequest,
	LinkableHabitStack,
	LinkableTask
} from '$lib/types';

export type JournalFilter = 'all' | 'own' | 'buddies';

export async function getJournalEntries(filter: JournalFilter = 'all'): Promise<JournalEntry[]> {
	const params = filter !== 'all' ? `?filter=${filter}` : '';
	return apiGet<JournalEntry[]>(`/journal${params}`);
}

export async function getJournalEntry(id: string): Promise<JournalEntry> {
	return apiGet<JournalEntry>(`/journal/${id}`);
}

export async function createJournalEntry(data: CreateJournalEntryRequest): Promise<JournalEntry> {
	return apiPost<JournalEntry>('/journal', data);
}

export async function updateJournalEntry(
	id: string,
	data: UpdateJournalEntryRequest
): Promise<JournalEntry> {
	return apiPut<JournalEntry>(`/journal/${id}`, data);
}

export async function deleteJournalEntry(id: string): Promise<void> {
	return apiDelete<void>(`/journal/${id}`);
}

export async function uploadJournalImage(entryId: string, file: File): Promise<JournalImage> {
	return apiUpload<JournalImage>(`/journal/${entryId}/images`, file);
}

export async function deleteJournalImage(entryId: string, imageId: string): Promise<void> {
	return apiDelete<void>(`/journal/${entryId}/images/${imageId}`);
}

export async function getLinkableHabitStacks(): Promise<LinkableHabitStack[]> {
	return apiGet<LinkableHabitStack[]>('/journal/linkable/habit-stacks');
}

export async function getLinkableTasks(): Promise<LinkableTask[]> {
	return apiGet<LinkableTask[]>('/journal/linkable/tasks');
}

// Reaction APIs
export async function addJournalReaction(entryId: string, emoji: string): Promise<JournalReaction> {
	return apiPost<JournalReaction>(`/journal/${entryId}/reactions`, { emoji });
}

export async function removeJournalReaction(entryId: string, reactionId: string): Promise<void> {
	return apiDelete<void>(`/journal/${entryId}/reactions/${reactionId}`);
}
