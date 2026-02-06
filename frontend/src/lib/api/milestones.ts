import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from './client';
import type { 
	MilestoneDefinition, 
	UserMilestone, 
	UserStats, 
	MarkSeenRequest,
	CreateMilestoneRequest,
	UpdateMilestoneRequest,
	ToggleMilestoneRequest
} from '$lib/types/milestone';

export async function getMyMilestones(): Promise<UserMilestone[]> {
	return apiGet<UserMilestone[]>('/milestones');
}

export async function getUnseenMilestones(): Promise<UserMilestone[]> {
	return apiGet<UserMilestone[]>('/milestones/unseen');
}

export async function markMilestonesSeen(milestoneIds: string[]): Promise<void> {
	return apiPost<void>('/milestones/mark-seen', { milestoneIds } as MarkSeenRequest);
}

export async function getMyStats(): Promise<UserStats> {
	return apiGet<UserStats>('/milestones/stats');
}

export async function getMilestoneDefinitions(): Promise<MilestoneDefinition[]> {
	return apiGet<MilestoneDefinition[]>('/milestones/definitions');
}

export async function createMilestoneDefinition(request: CreateMilestoneRequest): Promise<MilestoneDefinition> {
	return apiPost<MilestoneDefinition>('/milestones/definitions', request);
}

export async function updateMilestoneDefinition(id: string, request: UpdateMilestoneRequest): Promise<MilestoneDefinition> {
	return apiPut<MilestoneDefinition>(`/milestones/definitions/${id}`, request);
}

export async function toggleMilestoneDefinition(id: string, isActive: boolean): Promise<void> {
	return apiPatch<void>(`/milestones/definitions/${id}/toggle`, { isActive } as ToggleMilestoneRequest);
}

export async function deleteMilestoneDefinition(id: string): Promise<void> {
	return apiDelete<void>(`/milestones/definitions/${id}`);
}
