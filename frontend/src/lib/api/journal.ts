import { apiGet, apiPost, apiPut, apiDelete } from './client';
import type {
	JournalEntry,
	JournalImage,
	CreateJournalEntryRequest,
	UpdateJournalEntryRequest,
	LinkableHabitStack,
	LinkableTask
} from '$lib/types';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5001';

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
	const formData = new FormData();
	formData.append('file', file);

	const response = await fetch(`${API_BASE}/api/journal/${entryId}/images`, {
		method: 'POST',
		credentials: 'include',
		headers: {
			'X-CSRF': '1'
		},
		body: formData
	});

	if (!response.ok) {
		const errorText = await response.text();
		let errorMessage = `HTTP ${response.status}`;
		try {
			const errorJson = JSON.parse(errorText);
			errorMessage = errorJson.message || errorMessage;
		} catch {
			errorMessage = errorText || errorMessage;
		}
		throw new Error(errorMessage);
	}

	return response.json();
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
