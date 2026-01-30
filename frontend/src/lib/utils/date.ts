/**
 * Date utilities for handling user's local date.
 * 
 * These functions ensure we always use the client's local date rather than UTC,
 * which is important for date-sensitive operations like journal entries, 
 * daily commitments, and task completions near midnight.
 */

/**
 * Get the user's local date as a string in YYYY-MM-DD format.
 * This avoids timezone issues that occur with toISOString() which returns UTC date.
 * 
 * @param date - Optional Date object to convert. Defaults to current date/time.
 * @returns Date string in YYYY-MM-DD format based on user's local timezone.
 * 
 * @example
 * // Get today's local date
 * const today = getLocalDateString();
 * 
 * @example
 * // Convert a specific date
 * const tomorrow = new Date();
 * tomorrow.setDate(tomorrow.getDate() + 1);
 * const tomorrowStr = getLocalDateString(tomorrow);
 */
export function getLocalDateString(date: Date = new Date()): string {
	const year = date.getFullYear();
	const month = String(date.getMonth() + 1).padStart(2, '0');
	const day = String(date.getDate()).padStart(2, '0');
	return `${year}-${month}-${day}`;
}

/**
 * Check if a date string represents today's date in user's local timezone.
 * 
 * @param dateStr - Date string in YYYY-MM-DD format to check.
 * @returns True if the date string matches today's local date.
 */
export function isToday(dateStr: string): boolean {
	return dateStr === getLocalDateString();
}
