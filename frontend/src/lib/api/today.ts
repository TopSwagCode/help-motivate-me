import { apiGet } from './client';
import type { TodayView, DailyDigest } from '$lib/types';

export async function getTodayView(date?: string): Promise<TodayView> {
	const params = date ? `?date=${date}` : '';
	return apiGet<TodayView>(`/today${params}`);
}

export async function getDailyDigest(date?: string): Promise<DailyDigest> {
	const params = date ? `?date=${date}` : '';
	return apiGet<DailyDigest>(`/today/digest${params}`);
}
