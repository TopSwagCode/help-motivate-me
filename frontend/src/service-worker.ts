/// <reference lib="webworker" />
declare const self: ServiceWorkerGlobalScope;

import { precacheAndRoute } from 'workbox-precaching';

// Precache app shell - this is replaced by vite-pwa at build time
precacheAndRoute(self.__WB_MANIFEST);

// Handle push notifications
self.addEventListener('push', (event) => {
	console.log('[Service Worker] Push received:', event);
	
	if (!event.data) {
		console.log('[Service Worker] Push event but no data');
		return;
	}
	
	let data: { title?: string; body?: string; url?: string; icon?: string };
	
	try {
		data = event.data.json();
	} catch (e) {
		data = { title: 'Notification', body: event.data.text() };
	}
	
	const title = data.title || 'Help Motivate Me';
	const options: NotificationOptions = {
		body: data.body || '',
		icon: data.icon || '/android-chrome-192x192.png',
		badge: '/android-chrome-192x192.png',
		vibrate: [100, 50, 100],
		data: {
			url: data.url || '/'
		},
		requireInteraction: false,
		tag: 'help-motivate-me-notification'
	};
	
	event.waitUntil(
		self.registration.showNotification(title, options)
	);
});

// Handle notification click
self.addEventListener('notificationclick', (event) => {
	console.log('[Service Worker] Notification clicked:', event);
	
	event.notification.close();
	
	const urlToOpen = event.notification.data?.url || '/';
	
	event.waitUntil(
		self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
			// Check if there's already a window open
			for (const client of clientList) {
				if (client.url.includes(self.location.origin) && 'focus' in client) {
					client.focus();
					if ('navigate' in client) {
						(client as WindowClient).navigate(urlToOpen);
					}
					return;
				}
			}
			// If no window is open, open a new one
			if (self.clients.openWindow) {
				return self.clients.openWindow(urlToOpen);
			}
		})
	);
});

// Handle notification close
self.addEventListener('notificationclose', (event) => {
	console.log('[Service Worker] Notification closed:', event);
});
