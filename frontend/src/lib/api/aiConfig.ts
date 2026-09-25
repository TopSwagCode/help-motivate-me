import { apiGet } from './client';
import type { AiStatus } from '$lib/types';

export function getAiStatus(): Promise<AiStatus> {
	return apiGet<AiStatus>('/ai/status', { skipAuthRedirect: true });
}