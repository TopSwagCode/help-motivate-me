import { browser } from '$app/environment';
import { apiPost, apiDelete, apiGet } from '$lib/api/client';

const VAPID_PUBLIC_KEY = import.meta.env.VITE_VAPID_PUBLIC_KEY;

// Convert base64 URL to Uint8Array (needed for applicationServerKey)
function urlBase64ToUint8Array(base64String: string): ArrayBuffer {
	const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
	const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
	const rawData = window.atob(base64);
	const outputArray = new Uint8Array(rawData.length);
	for (let i = 0; i < rawData.length; ++i) {
		outputArray[i] = rawData.charCodeAt(i);
	}
	return outputArray.buffer as ArrayBuffer;
}

export async function subscribeToPushNotifications(): Promise<boolean> {
	if (!browser || !('serviceWorker' in navigator) || !('PushManager' in window)) {
		console.log('Push notifications not supported');
		return false;
	}

	if (!VAPID_PUBLIC_KEY) {
		console.error('VAPID public key not configured');
		return false;
	}

	try {
		const registration = await navigator.serviceWorker.ready;

		// Check current subscription
		let subscription = await registration.pushManager.getSubscription();

		if (!subscription) {
			// Create new subscription
			subscription = await registration.pushManager.subscribe({
				userVisibleOnly: true,
				applicationServerKey: urlBase64ToUint8Array(VAPID_PUBLIC_KEY)
			});
		}

		// Convert to JSON and send to backend
		const subscriptionJson = subscription.toJSON();
		await apiPost('/notifications/push/subscribe', {
			endpoint: subscriptionJson.endpoint,
			keys: {
				p256dh: subscriptionJson.keys?.p256dh,
				auth: subscriptionJson.keys?.auth
			}
		});

		return true;
	} catch (error) {
		console.error('Failed to subscribe to push notifications:', error);
		return false;
	}
}

export async function unsubscribeFromPushNotifications(): Promise<boolean> {
	if (!browser || !('serviceWorker' in navigator)) {
		return false;
	}

	try {
		const registration = await navigator.serviceWorker.ready;
		const subscription = await registration.pushManager.getSubscription();

		if (subscription) {
			// Tell backend to remove subscription
			const subscriptionJson = subscription.toJSON();
			await apiDelete(`/notifications/push/unsubscribe?endpoint=${encodeURIComponent(subscriptionJson.endpoint || '')}`);
			await subscription.unsubscribe();
		}

		return true;
	} catch (error) {
		console.error('Failed to unsubscribe from push notifications:', error);
		return false;
	}
}

export async function checkPushPermission(): Promise<NotificationPermission> {
	if (!browser || !('Notification' in window)) {
		return 'denied';
	}
	return Notification.permission;
}

export async function requestPushPermission(): Promise<NotificationPermission> {
	if (!browser || !('Notification' in window)) {
		return 'denied';
	}
	return Notification.requestPermission();
}

export function isPushSupported(): boolean {
	return browser && 'serviceWorker' in navigator && 'PushManager' in window && !!VAPID_PUBLIC_KEY;
}

/**
 * Check if currently subscribed to push notifications
 */
export async function isSubscribedToPush(): Promise<boolean> {
	if (!browser || !('serviceWorker' in navigator) || !('PushManager' in window)) {
		return false;
	}

	try {
		const registration = await navigator.serviceWorker.ready;
		const subscription = await registration.pushManager.getSubscription();
		return !!subscription;
	} catch {
		return false;
	}
}

/**
 * Get push notification status from backend
 */
export async function getPushStatus(): Promise<{ subscribed: boolean; subscriptionCount: number }> {
	try {
		const response = await apiGet<{ subscribed: boolean; subscriptionCount: number }>('/notifications/push/status');
		return response;
	} catch {
		return { subscribed: false, subscriptionCount: 0 };
	}
}
