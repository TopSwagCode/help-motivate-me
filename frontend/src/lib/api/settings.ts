import { apiPatch, apiPost } from './client';
import type { User, UpdateProfileRequest, ChangePasswordRequest, UpdateMembershipRequest } from '$lib/types';

export async function updateProfile(data: UpdateProfileRequest): Promise<User> {
	return apiPatch<User>('/auth/profile', data);
}

export async function changePassword(data: ChangePasswordRequest): Promise<void> {
	await apiPost('/auth/change-password', data);
}

export async function updateMembership(data: UpdateMembershipRequest): Promise<User> {
	return apiPatch<User>('/auth/membership', data);
}
