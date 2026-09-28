import { browser } from '$app/environment';
import { apiDelete, apiGet, apiPost } from '$lib/api/client';
import type {
	PushConfigurationResponse,
	PushNotificationResult,
	PushSubscriptionRequest,
	PushSubscriptionsStatusResponse
} from '$lib/types/api-types';

export type PushState = {
	supported: boolean;
	permission: NotificationPermission;
	enabled: boolean;
	registeredDevices: number;
};

export function isPushSupported(): boolean {
	return browser && 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
}

export async function getPushState(): Promise<PushState> {
	if (!isPushSupported())
		return { supported: false, permission: 'default', enabled: false, registeredDevices: 0 };

	const registration = await navigator.serviceWorker.ready;
	const subscription = await registration.pushManager.getSubscription();
	const status = await apiGet<PushSubscriptionsStatusResponse>('/notifications/push/status');
	return {
		supported: true,
		permission: Notification.permission,
		enabled: subscription !== null,
		registeredDevices: status.subscriptionCount
	};
}

export async function enablePushNotifications(): Promise<void> {
	if (!isPushSupported()) throw new Error('Push notifications are not supported by this browser.');
	if (Notification.permission === 'denied') throw new Error('Notification permission is blocked in browser settings.');

	const permission = await Notification.requestPermission();
	if (permission !== 'granted') throw new Error('Notification permission was not granted.');

	const registration = await navigator.serviceWorker.ready;
	let subscription = await registration.pushManager.getSubscription();
	let created = false;

	if (!subscription) {
		const configuration = await apiGet<PushConfigurationResponse>('/notifications/push/configuration');
		subscription = await registration.pushManager.subscribe({
			userVisibleOnly: true,
			applicationServerKey: urlBase64ToUint8Array(configuration.publicKey)
		});
		created = true;
	}

	try {
		await apiPost<void>('/notifications/push/subscribe', toRequest(subscription));
	} catch (error) {
		if (created) await subscription.unsubscribe();
		throw error;
	}
}

export async function disablePushNotifications(): Promise<void> {
	if (!isPushSupported()) return;

	const registration = await navigator.serviceWorker.ready;
	const subscription = await registration.pushManager.getSubscription();
	if (!subscription) return;

	await apiDelete<void>(`/notifications/push/unsubscribe?endpoint=${encodeURIComponent(subscription.endpoint)}`);
	await subscription.unsubscribe();
}

export async function sendTestPushNotification(): Promise<PushNotificationResult> {
	return apiPost<PushNotificationResult>('/notifications/push/test');
}

function toRequest(subscription: PushSubscription): PushSubscriptionRequest {
	const serialized = subscription.toJSON();
	if (!serialized.endpoint || !serialized.keys?.p256dh || !serialized.keys.auth)
		throw new Error('The browser returned an incomplete push subscription.');

	return {
		endpoint: serialized.endpoint,
		keys: {
			p256dh: serialized.keys.p256dh,
			auth: serialized.keys.auth
		}
	};
}

function urlBase64ToUint8Array(value: string): Uint8Array<ArrayBuffer> {
	const padding = '='.repeat((4 - (value.length % 4)) % 4);
	const base64 = (value + padding).replace(/-/g, '+').replace(/_/g, '/');
	const bytes = atob(base64);
	return Uint8Array.from(bytes, (character) => character.charCodeAt(0));
}