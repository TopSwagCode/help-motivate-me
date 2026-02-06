import { apiPost } from './client';
import type { User } from '$lib/types/auth';

export async function completeOnboarding(): Promise<User> {
	return apiPost<User>('/auth/complete-onboarding');
}

export async function resetOnboarding(): Promise<User> {
	return apiPost<User>('/auth/reset-onboarding');
}
