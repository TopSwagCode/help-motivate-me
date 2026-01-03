import { apiGet, apiPost, apiDelete } from './client';
import type {
	BuddyRelationshipsResponse,
	BuddyResponse,
	BuddyTodayViewResponse,
	BuddyJournalEntry,
	CreateBuddyJournalEntryRequest,
	BuddyLoginResponse
} from '$lib/types';

export async function getBuddyRelationships(): Promise<BuddyRelationshipsResponse> {
	return apiGet<BuddyRelationshipsResponse>('/buddies');
}

export async function inviteBuddy(email: string): Promise<BuddyResponse> {
	return apiPost<BuddyResponse>('/buddies/invite', { email });
}

export async function removeBuddy(id: string): Promise<void> {
	return apiDelete<void>(`/buddies/${id}`);
}

export async function leaveBuddy(userId: string): Promise<void> {
	return apiDelete<void>(`/buddies/leave/${userId}`);
}

export async function getBuddyTodayView(userId: string, date?: string): Promise<BuddyTodayViewResponse> {
	const params = date ? `?date=${date}` : '';
	return apiGet<BuddyTodayViewResponse>(`/buddies/${userId}/today${params}`);
}

export async function getBuddyJournal(userId: string): Promise<BuddyJournalEntry[]> {
	return apiGet<BuddyJournalEntry[]>(`/buddies/${userId}/journal`);
}

export async function createBuddyJournalEntry(
	userId: string,
	data: CreateBuddyJournalEntryRequest
): Promise<BuddyJournalEntry> {
	return apiPost<BuddyJournalEntry>(`/buddies/${userId}/journal`, data);
}

export async function loginWithBuddyToken(token: string): Promise<BuddyLoginResponse> {
	return apiPost<BuddyLoginResponse>('/auth/login-with-buddy-token', { token });
}
