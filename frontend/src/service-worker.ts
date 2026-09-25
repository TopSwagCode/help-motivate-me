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
