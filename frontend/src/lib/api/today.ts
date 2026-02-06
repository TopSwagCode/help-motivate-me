import { apiGet } from './client';
import type { TodayView } from '$lib/types';

export async function getTodayView(date?: string): Promise<TodayView> {
	const params = date ? `?date=${date}` : '';
	return apiGet<TodayView>(`/today${params}`);
}
