import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from './client';
import type { Task, CreateTaskRequest, UpdateTaskRequest } from '$lib/types';
import { getLocalDateString } from '$lib/utils/date';

export async function getTasks(goalId: string): Promise<Task[]> {
	return apiGet<Task[]>(`/goals/${goalId}/tasks`);
}

export async function getTask(id: string): Promise<Task> {
	return apiGet<Task>(`/tasks/${id}`);
}

export async function createTask(goalId: string, data: CreateTaskRequest): Promise<Task> {
	return apiPost<Task>(`/goals/${goalId}/tasks`, data);
}

export async function createSubtask(taskId: string, data: CreateTaskRequest): Promise<Task> {
	return apiPost<Task>(`/tasks/${taskId}/subtasks`, data);
}

export async function updateTask(id: string, data: UpdateTaskRequest): Promise<Task> {
	return apiPut<Task>(`/tasks/${id}`, data);
}

export async function deleteTask(id: string): Promise<void> {
	return apiDelete<void>(`/tasks/${id}`);
}

export async function completeTask(id: string, date?: string): Promise<Task> {
	const clientDate = date ?? getLocalDateString();
	return apiPatch<Task>(`/tasks/${id}/complete?date=${clientDate}`);
}

export async function reorderTasks(taskIds: string[]): Promise<void> {
	return apiPut<void>('/tasks/reorder', taskIds);
}

export async function postponeTask(id: string, newDueDate: string): Promise<Task> {
	return apiPatch<Task>(`/tasks/${id}/postpone`, { newDueDate });
}

export interface CompleteMultipleTasksResponse {
	completedCount: number;
	totalCount: number;
}

export async function completeMultipleTasks(taskIds: string[], date?: string): Promise<CompleteMultipleTasksResponse> {
	const clientDate = date ?? getLocalDateString();
	return apiPost<CompleteMultipleTasksResponse>('/tasks/complete-multiple', { taskIds, date: clientDate });
}
