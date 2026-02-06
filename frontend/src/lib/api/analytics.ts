import { apiGet } from './client';
import type { StreakSummary, CompletionRate, TaskStreak, HeatmapData } from '$lib/types';

export async function getStreakSummary(): Promise<StreakSummary> {
	return apiGet<StreakSummary>('/analytics/streaks');
}

export async function getCompletionRates(): Promise<CompletionRate> {
	return apiGet<CompletionRate>('/analytics/completion-rates');
}

export async function getHeatmapData(days: number = 90): Promise<HeatmapData[]> {
	return apiGet<HeatmapData[]>(`/analytics/heatmap?days=${days}`);
}

export async function getTaskStreak(taskId: string): Promise<TaskStreak> {
	return apiGet<TaskStreak>(`/tasks/${taskId}/streak`);
}
