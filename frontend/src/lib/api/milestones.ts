import { apiGet, apiPost } from './client';
import type { MilestoneDefinition, UserMilestone, UserStats, MarkSeenRequest } from '$lib/types/milestone';

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
