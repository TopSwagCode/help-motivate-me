import { apiGet, apiPatch, apiPost, apiDelete } from './client';
import type { AdminStats, AdminUser, DailyStats, UpdateRoleRequest, UserActivity } from '$lib/types';
import type { AiUsageStats, AiUsageLog, PaginatedResponse, WaitlistEntry, WhitelistEntry, SignupSettingsResponse } from '$lib/types';

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

export async function getUserActivity(userId: string): Promise<UserActivity> {
	return apiGet<UserActivity>(`/admin/users/${userId}/activity`);
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

// Push Notification management
export interface PushNotificationResult {
	totalSubscriptions: number;
	successCount: number;
	failureCount: number;
	errors: string[];
}

export interface UserPushStatus {
	userId: string;
	email: string;
	hasPushEnabled: boolean;
	subscriptionCount: number;
	lastPushSentAt: string | null;
}

export interface PushStats {
	totalSubscriptions: number;
	usersWithPush: number;
	totalUsers: number;
	percentageWithPush: number;
}

export async function getPushStats(): Promise<PushStats> {
	return apiGet<PushStats>('/notifications/push/admin/stats');
}

export async function getUsersWithPushStatus(params?: {
	hasPush?: boolean;
	search?: string;
}): Promise<UserPushStatus[]> {
	const searchParams = new URLSearchParams();
	if (params?.hasPush !== undefined) searchParams.set('hasPush', params.hasPush.toString());
	if (params?.search) searchParams.set('search', params.search);

	const queryString = searchParams.toString();
	const endpoint = queryString
		? `/notifications/push/admin/users?${queryString}`
		: '/notifications/push/admin/users';
	return apiGet<UserPushStatus[]>(endpoint);
}

export async function sendPushToUser(
	userId: string,
	title: string,
	body: string,
	url?: string
): Promise<PushNotificationResult> {
	return apiPost<PushNotificationResult>('/notifications/push/admin/send-to-user', {
		userId,
		title,
		body,
		url
	});
}

export async function sendPushToAll(
	title: string,
	body: string,
	url?: string
): Promise<PushNotificationResult> {
	return apiPost<PushNotificationResult>('/notifications/push/admin/send-to-all', {
		title,
		body,
		url
	});
}

// Analytics Events management
export interface EventTypeCount {
	eventType: string;
	count: number;
}

export interface DailyEventCount {
	date: string;
	count: number;
}

export interface SessionSummary {
	sessionId: string;
	userId: string;
	email: string;
	firstEvent: string;
	lastEvent: string;
	eventCount: number;
	durationMinutes: number;
}

export interface AnalyticsOverviewResponse {
	totalEvents: number;
	uniqueUsers: number;
	uniqueSessions: number;
	avgEventsPerSession: number;
	topEventTypes: EventTypeCount[];
	dailyEventCounts: DailyEventCount[];
	recentSessions: SessionSummary[];
}

export async function getAnalyticsOverview(days: number = 30): Promise<AnalyticsOverviewResponse> {
	return apiGet<AnalyticsOverviewResponse>(`/admin/analytics/overview?days=${days}`);
}

// AI Usage management
export async function getAiUsageStats(): Promise<AiUsageStats> {
	return apiGet<AiUsageStats>('/admin/ai-usage/stats');
}

export async function getAiUsageLogs(params?: {
	page?: number;
	pageSize?: number;
}): Promise<PaginatedResponse<AiUsageLog>> {
	const searchParams = new URLSearchParams();
	if (params?.page) searchParams.set('page', params.page.toString());
	if (params?.pageSize) searchParams.set('pageSize', params.pageSize.toString());

	const queryString = searchParams.toString();
	const endpoint = queryString ? `/admin/ai-usage?${queryString}` : '/admin/ai-usage';
	return apiGet<PaginatedResponse<AiUsageLog>>(endpoint);
}
