import { apiGet, apiPost, apiPut, apiDelete, apiUpload } from './client';
import type {
	JournalEntry,
	JournalImage,
	CreateJournalEntryRequest,
	UpdateJournalEntryRequest,
	LinkableHabitStack,
	LinkableTask
} from '$lib/types';

export async function getJournalEntries(): Promise<JournalEntry[]> {
	return apiGet<JournalEntry[]>('/journal');
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
