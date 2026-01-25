import { apiGet, apiPost, apiPut } from './client';
import type {
	DailyCommitment,
	CommitmentOptions,
	ActionSuggestionsResponse,
	YesterdayCommitment,
	CreateDailyCommitmentRequest
} from '$lib/types/dailyCommitment';

export async function getDailyCommitment(date?: string): Promise<DailyCommitment | null> {
	const params = date ? `?date=${date}` : '';
	return apiGet<DailyCommitment | null>(`/daily-commitment${params}`);
}

export async function getCommitmentOptions(): Promise<CommitmentOptions> {
	return apiGet<CommitmentOptions>('/daily-commitment/options');
}

export async function getActionSuggestions(identityId: string): Promise<ActionSuggestionsResponse> {
	return apiGet<ActionSuggestionsResponse>(`/daily-commitment/suggestions?identityId=${identityId}`);
}

export async function getYesterdayCommitment(): Promise<YesterdayCommitment> {
	return apiGet<YesterdayCommitment>('/daily-commitment/yesterday');
}

export async function createDailyCommitment(
	request: CreateDailyCommitmentRequest
): Promise<DailyCommitment> {
	return apiPost<DailyCommitment>('/daily-commitment', request);
}

export async function completeDailyCommitment(id: string): Promise<DailyCommitment> {
	return apiPut<DailyCommitment>(`/daily-commitment/${id}/complete`, {});
}

export async function dismissDailyCommitment(id: string): Promise<DailyCommitment> {
	return apiPut<DailyCommitment>(`/daily-commitment/${id}/dismiss`, {});
}
