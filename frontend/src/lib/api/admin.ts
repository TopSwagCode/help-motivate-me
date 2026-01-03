import { apiGet, apiPatch } from './client';
import type { AdminStats, AdminUser, UpdateRoleRequest } from '$lib/types';

export async function getAdminStats(): Promise<AdminStats> {
	return apiGet<AdminStats>('/admin/stats');
}

export async function getAdminUsers(params?: {
	search?: string;
	tier?: string;
	isActive?: boolean;
	page?: number;
	pageSize?: number;
}): Promise<AdminUser[]> {
	const searchParams = new URLSearchParams();

	if (params?.search) searchParams.set('search', params.search);
	if (params?.tier) searchParams.set('tier', params.tier);
	if (params?.isActive !== undefined) searchParams.set('isActive', params.isActive.toString());
	if (params?.page) searchParams.set('page', params.page.toString());
	if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString());

	const queryString = searchParams.toString();
	const endpoint = queryString ? `/admin/users?${queryString}` : '/admin/users';

	return apiGet<AdminUser[]>(endpoint);
}

export async function toggleUserActive(userId: string): Promise<AdminUser> {
	return apiPatch<AdminUser>(`/admin/users/${userId}/toggle-active`);
}

export async function updateUserRole(userId: string, data: UpdateRoleRequest): Promise<AdminUser> {
	return apiPatch<AdminUser>(`/admin/users/${userId}/role`, data);
}
