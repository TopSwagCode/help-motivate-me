/// <reference lib="webworker" />
declare const self: ServiceWorkerGlobalScope;

import { precacheAndRoute, cleanupOutdatedCaches } from 'workbox-precaching';
import { clientsClaim } from 'workbox-core';

// Clean up old caches from previous versions
cleanupOutdatedCaches();

// Precache app shell - this is replaced by vite-pwa at build time
precacheAndRoute(self.__WB_MANIFEST);

// Handle the "skip waiting" message from the app when user clicks "Update"
self.addEventListener('message', (event) => {
	if (event.data && event.data.type === 'SKIP_WAITING') {
		self.skipWaiting();
	}
});

// Claim clients immediately after activation
self.addEventListener('activate', (event) => {
	event.waitUntil(clientsClaim());
});

self.addEventListener('push', (event) => {
	let payload: { title?: string; body?: string; url?: string; icon?: string; badge?: string } = {};
	try {
		payload = event.data?.json() ?? {};
	} catch {
		payload = { body: event.data?.text() };
	}

	const target = new URL(payload.url ?? '/today', self.location.origin);
	const url = target.origin === self.location.origin ? `${target.pathname}${target.search}${target.hash}` : '/today';
	event.waitUntil(
		self.registration.showNotification(payload.title ?? 'Help Motivate Me', {
			body: payload.body,
			icon: payload.icon ?? '/android-chrome-192x192.png',
			badge: payload.badge ?? '/android-chrome-192x192.png',
			data: { url }
		})
	);
});

self.addEventListener('notificationclick', (event) => {
	event.notification.close();
	const url = typeof event.notification.data?.url === 'string' ? event.notification.data.url : '/today';
	event.waitUntil(
		self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(async (windowClients) => {
			const existing = windowClients.find((client) => new URL(client.url).origin === self.location.origin);
			if (existing) {
				await existing.navigate(url);
				return existing.focus();
			}
			return self.clients.openWindow(url);
		})
	);
});
