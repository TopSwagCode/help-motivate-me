import { apiGet, apiPatch, apiPost, apiDelete } from './client';
import type { AdminStats, AdminUser, DailyStats, UpdateRoleRequest } from '$lib/types';
import type { WaitlistEntry, WhitelistEntry, SignupSettingsResponse } from '$lib/types/waitlist';

export async function getAdminStats(): Promise<AdminStats> {
	return apiGet<AdminStats>('/admin/stats');
}

export async function getDailyStats(date?: string): Promise<DailyStats> {
	const endpoint = date ? `/admin/stats/daily?date=${date}` : '/admin/stats/daily';
	return apiGet<DailyStats>(endpoint);
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

// Settings
export async function getAdminSettings(): Promise<SignupSettingsResponse> {
	return apiGet<SignupSettingsResponse>('/admin/settings');
}

// Waitlist management
export async function getWaitlist(params?: {
	search?: string;
	page?: number;
	pageSize?: number;
}): Promise<WaitlistEntry[]> {
	const searchParams = new URLSearchParams();
	if (params?.search) searchParams.set('search', params.search);
	if (params?.page) searchParams.set('page', params.page.toString());
	if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString());

	const queryString = searchParams.toString();
	const endpoint = queryString ? `/admin/waitlist?${queryString}` : '/admin/waitlist';
	return apiGet<WaitlistEntry[]>(endpoint);
}

export async function removeFromWaitlist(id: string): Promise<void> {
	return apiDelete<void>(`/admin/waitlist/${id}`);
}

export async function approveWaitlistEntry(id: string): Promise<WhitelistEntry> {
	return apiPost<WhitelistEntry>(`/admin/waitlist/${id}/approve`);
}

// Whitelist management
export async function getWhitelist(params?: {
	search?: string;
	page?: number;
	pageSize?: number;
}): Promise<WhitelistEntry[]> {
	const searchParams = new URLSearchParams();
	if (params?.search) searchParams.set('search', params.search);
	if (params?.page) searchParams.set('page', params.page.toString());
	if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString());

	const queryString = searchParams.toString();
	const endpoint = queryString ? `/admin/whitelist?${queryString}` : '/admin/whitelist';
	return apiGet<WhitelistEntry[]>(endpoint);
}

export async function addToWhitelist(email: string): Promise<WhitelistEntry> {
	return apiPost<WhitelistEntry>('/admin/whitelist', { email });
}

export async function removeFromWhitelist(id: string): Promise<void> {
	return apiDelete<void>(`/admin/whitelist/${id}`);
}
