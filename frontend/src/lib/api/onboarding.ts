import { apiPost } from './client';
import type { User } from '$lib/types/auth';

export async function completeOnboarding(): Promise<User> {
	return apiPost<User>('/auth/complete-onboarding');
}
