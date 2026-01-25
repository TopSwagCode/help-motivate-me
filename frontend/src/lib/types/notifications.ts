export type TimeSlot = 'Morning' | 'Afternoon' | 'Evening' | 'Custom';

export type DayOfWeek = 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday';

// Bit flags for days (matches backend NotificationDays enum)
export const NotificationDays = {
	None: 0,
	Monday: 1,
	Tuesday: 2,
	Wednesday: 4,
	Thursday: 8,
	Friday: 16,
	Saturday: 32,
	Sunday: 64,
	Weekdays: 1 + 2 + 4 + 8 + 16, // 31
	Weekend: 32 + 64, // 96
	All: 1 + 2 + 4 + 8 + 16 + 32 + 64 // 127
} as const;

export interface NotificationPreferences {
	notificationsEnabled: boolean;
	emailEnabled: boolean;
	smsEnabled: boolean;
	phoneEnabled: boolean;
	habitRemindersEnabled: boolean;
	goalRemindersEnabled: boolean;
	dailyDigestEnabled: boolean;
	streakAlertsEnabled: boolean;
	motivationalQuotesEnabled: boolean;
	weeklyReviewEnabled: boolean;
	buddyUpdatesEnabled: boolean;
	dailyCommitmentEnabled: boolean;
	commitmentDefaultMode: string;
	selectedDays: number;
	preferredTimeSlot: TimeSlot;
	customTimeStart: string | null;
	customTimeEnd: string | null;
	timezoneId: string;
	utcOffsetMinutes: number;
}

export interface UpdateNotificationPreferencesRequest {
	notificationsEnabled?: boolean;
	emailEnabled?: boolean;
	smsEnabled?: boolean;
	habitRemindersEnabled?: boolean;
	goalRemindersEnabled?: boolean;
	dailyDigestEnabled?: boolean;
	streakAlertsEnabled?: boolean;
	motivationalQuotesEnabled?: boolean;
	weeklyReviewEnabled?: boolean;
	buddyUpdatesEnabled?: boolean;
	dailyCommitmentEnabled?: boolean;
	commitmentDefaultMode?: string;
	selectedDays?: number;
	preferredTimeSlot?: string;
	customTimeStart?: string | null;
	customTimeEnd?: string | null;
	timezoneId?: string;
	utcOffsetMinutes?: number;
}
