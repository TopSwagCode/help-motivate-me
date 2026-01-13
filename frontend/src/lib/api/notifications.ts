import { apiGet, apiPatch } from './client';
import type { NotificationPreferences, UpdateNotificationPreferencesRequest } from '$lib/types';

export async function getNotificationPreferences(): Promise<NotificationPreferences> {
	return apiGet<NotificationPreferences>('/auth/notification-preferences');
}

export async function updateNotificationPreferences(
	data: UpdateNotificationPreferencesRequest
): Promise<NotificationPreferences> {
	return apiPatch<NotificationPreferences>('/auth/notification-preferences', data);
}
