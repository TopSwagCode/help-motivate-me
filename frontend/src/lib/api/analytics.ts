import { apiGet } from './client';
import type { CompletionRate, HeatmapData } from '$lib/types';

export async function getCompletionRates(): Promise<CompletionRate> {
	return apiGet<CompletionRate>('/analytics/completion-rates');
}

export async function getHeatmapData(days: number = 90): Promise<HeatmapData[]> {
	return apiGet<HeatmapData[]>(`/analytics/heatmap?days=${days}`);
}
