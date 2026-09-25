import { apiPatch } from './client';
import type { User, UpdateProfileRequest } from '$lib/types';

export async function updateProfile(data: UpdateProfileRequest): Promise<User> {
	return apiPatch<User>('/auth/profile', data);
}
