import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from './client';
import type {
	HabitStack,
	CreateHabitStackRequest,
	UpdateHabitStackRequest,
	HabitStackItemRequest,
	HabitStackItemCompletionResponse
} from '$lib/types';

export async function getHabitStacks(): Promise<HabitStack[]> {
	return apiGet<HabitStack[]>('/habit-stacks');
}

export async function getHabitStack(id: string): Promise<HabitStack> {
	return apiGet<HabitStack>(`/habit-stacks/${id}`);
}

export async function createHabitStack(data: CreateHabitStackRequest): Promise<HabitStack> {
	return apiPost<HabitStack>('/habit-stacks', data);
}

export async function updateHabitStack(
	id: string,
	data: UpdateHabitStackRequest
): Promise<HabitStack> {
	return apiPut<HabitStack>(`/habit-stacks/${id}`, data);
}

export async function deleteHabitStack(id: string): Promise<void> {
	return apiDelete<void>(`/habit-stacks/${id}`);
}

export async function reorderHabitStacks(stackIds: string[]): Promise<void> {
	return apiPut<void>('/habit-stacks/reorder', { stackIds });
}

export async function addStackItem(
	stackId: string,
	data: HabitStackItemRequest
): Promise<HabitStack> {
	return apiPost<HabitStack>(`/habit-stacks/${stackId}/items`, data);
}

export async function reorderStackItems(stackId: string, itemIds: string[]): Promise<HabitStack> {
	return apiPut<HabitStack>(`/habit-stacks/${stackId}/items/reorder`, { itemIds });
}

export async function deleteStackItem(itemId: string): Promise<void> {
	return apiDelete<void>(`/habit-stacks/items/${itemId}`);
}

export async function updateStackItem(
	itemId: string,
	data: { cueDescription: string; habitDescription: string }
): Promise<void> {
	return apiPut<void>(`/habit-stacks/items/${itemId}`, data);
}

export async function completeStackItem(
	itemId: string,
	date?: string
): Promise<HabitStackItemCompletionResponse> {
	const params = date ? `?date=${date}` : '';
	return apiPatch<HabitStackItemCompletionResponse>(`/habit-stacks/items/${itemId}/complete${params}`);
}

export interface CompleteAllResponse {
	stackId: string;
	completedCount: number;
	totalCount: number;
}

export async function completeAllStackItems(
	stackId: string,
	date?: string
): Promise<CompleteAllResponse> {
	const params = date ? `?date=${date}` : '';
	return apiPatch<CompleteAllResponse>(`/habit-stacks/${stackId}/complete-all${params}`);
}
