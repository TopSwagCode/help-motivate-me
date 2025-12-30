import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from './client';
import type { Goal, CreateGoalRequest, UpdateGoalRequest } from '$lib/types';

export async function getGoals(categoryId?: string): Promise<Goal[]> {
	const params = categoryId ? `?categoryId=${categoryId}` : '';
	return apiGet<Goal[]>(`/goals${params}`);
}

export async function getGoal(id: string): Promise<Goal> {
	return apiGet<Goal>(`/goals/${id}`);
}

export async function createGoal(data: CreateGoalRequest): Promise<Goal> {
	return apiPost<Goal>('/goals', data);
}

export async function updateGoal(id: string, data: UpdateGoalRequest): Promise<Goal> {
	return apiPut<Goal>(`/goals/${id}`, data);
}

export async function deleteGoal(id: string): Promise<void> {
	return apiDelete<void>(`/goals/${id}`);
}

export async function completeGoal(id: string, date?: string): Promise<Goal> {
	const clientDate = date ?? new Date().toISOString().split('T')[0];
	return apiPatch<Goal>(`/goals/${id}/complete?date=${clientDate}`);
}

export async function reorderGoals(goalIds: string[]): Promise<void> {
	return apiPut<void>('/goals/reorder', goalIds);
}
