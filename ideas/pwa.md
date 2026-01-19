# PWA Implementation Plan for Help Motivate Me

## Overview

This document outlines the complete plan to convert the SvelteKit frontend into a Progressive Web App (PWA) with:
- Push notifications support
- Offline readonly mode
- Proper cache invalidation on deployments

---

## Phase 1: Basic PWA Setup

### 1.1 Install Dependencies

```bash
cd frontend
npm install -D vite-plugin-pwa @vite-pwa/sveltekit workbox-window
```

### 1.2 Configure Vite Plugin

Update `vite.config.ts`:

```typescript
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import { SvelteKitPWA } from '@vite-pwa/sveltekit';

export default defineConfig({
  plugins: [
    sveltekit(),
    SvelteKitPWA({
      srcDir: 'src',
      mode: 'production',
      strategies: 'generateSW',
      registerType: 'prompt', // Important: prompts user to update
      manifest: {
        name: 'Help Motivate Me',
        short_name: 'Motivate Me',
        description: 'Track your goals, build habits, and become who you want to be',
        theme_color: '#4F46E5', // Indigo
        background_color: '#ffffff',
        display: 'standalone',
        scope: '/',
        start_url: '/',
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable'
          }
        ]
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff,woff2}'],
        // Cache strategies defined below
        runtimeCaching: [
          // API calls - Network first, fall back to cache for offline
          {
            urlPattern: /^https?:\/\/.*\/api\/(goals|identities|today|habit-stacks|journal)/,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'api-cache',
              expiration: {
                maxEntries: 100,
                maxAgeSeconds: 60 * 60 * 24 // 24 hours
              },
              cacheableResponse: {
                statuses: [0, 200]
              },
              networkTimeoutSeconds: 3
            }
          },
          // Static assets - Cache first
          {
            urlPattern: /\.(?:png|jpg|jpeg|svg|gif|webp)$/,
            handler: 'CacheFirst',
            options: {
              cacheName: 'image-cache',
              expiration: {
                maxEntries: 50,
                maxAgeSeconds: 60 * 60 * 24 * 30 // 30 days
              }
            }
          },
          // Google Fonts
          {
            urlPattern: /^https:\/\/fonts\.googleapis\.com/,
            handler: 'StaleWhileRevalidate',
            options: {
              cacheName: 'google-fonts-stylesheets'
            }
          },
          {
            urlPattern: /^https:\/\/fonts\.gstatic\.com/,
            handler: 'CacheFirst',
            options: {
              cacheName: 'google-fonts-webfonts',
              expiration: {
                maxEntries: 20,
                maxAgeSeconds: 60 * 60 * 24 * 365 // 1 year
              }
            }
          }
        ]
      },
      devOptions: {
        enabled: true,
        type: 'module'
      }
    })
  ],
  server: {
    port: 5173,
    strictPort: true
  }
});
```

### 1.3 Create PWA Icons

Create the following icons in `frontend/static/`:
- `pwa-192x192.png` - 192x192 px
- `pwa-512x512.png` - 512x512 px
- `apple-touch-icon.png` - 180x180 px (for iOS)

**Tool recommendation:** Use https://realfavicongenerator.net/ to generate all required icons from your existing favicon.

### 1.4 Update app.html

Add meta tags for PWA:

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <link rel="icon" href="%sveltekit.assets%/favicon.png" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    
    <!-- PWA Meta Tags -->
    <meta name="theme-color" content="#4F46E5" />
    <meta name="apple-mobile-web-app-capable" content="yes" />
    <meta name="apple-mobile-web-app-status-bar-style" content="default" />
    <meta name="apple-mobile-web-app-title" content="Motivate Me" />
    <link rel="apple-touch-icon" href="%sveltekit.assets%/apple-touch-icon.png" />
    
    <!-- Existing content -->
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
    %sveltekit.head%
  </head>
  <body data-sveltekit-preload-data="hover">
    <div style="display: contents">%sveltekit.body%</div>
  </body>
</html>
```

---

## Phase 2: Service Worker Registration & Update Handling

### 2.1 Create PWA Store

Create `frontend/src/lib/stores/pwa.ts`:

```typescript
import { writable } from 'svelte/store';

interface PWAState {
  needsRefresh: boolean;
  offlineReady: boolean;
  isOnline: boolean;
  updateServiceWorker?: () => Promise<void>;
}

export const pwaStore = writable<PWAState>({
  needsRefresh: false,
  offlineReady: false,
  isOnline: typeof navigator !== 'undefined' ? navigator.onLine : true
});
```

### 2.2 Create PWA Registration Component

Create `frontend/src/lib/components/PWAReloadPrompt.svelte`:

```svelte
<script lang="ts">
  import { onMount } from 'svelte';
  import { pwaStore } from '$lib/stores/pwa';
  import { useRegisterSW } from 'virtual:pwa-register/svelte';

  let needRefresh = $state(false);
  let offlineReady = $state(false);

  const {
    needRefresh: needRefreshStore,
    offlineReady: offlineReadyStore,
    updateServiceWorker
  } = useRegisterSW({
    immediate: true,
    onRegisteredSW(swUrl, registration) {
      console.log(`Service Worker registered: ${swUrl}`);
      
      // Check for updates every hour
      if (registration) {
        setInterval(() => {
          registration.update();
        }, 60 * 60 * 1000);
      }
    },
    onRegisterError(error) {
      console.error('Service Worker registration error:', error);
    }
  });

  // Subscribe to stores
  $effect(() => {
    needRefreshStore.subscribe((value) => {
      needRefresh = value;
      pwaStore.update((state) => ({ ...state, needsRefresh: value }));
    });
  });

  $effect(() => {
    offlineReadyStore.subscribe((value) => {
      offlineReady = value;
      pwaStore.update((state) => ({ ...state, offlineReady: value }));
    });
  });

  // Store the update function
  $effect(() => {
    pwaStore.update((state) => ({ ...state, updateServiceWorker }));
  });

  function close() {
    offlineReady = false;
    needRefresh = false;
  }

  async function handleUpdate() {
    await updateServiceWorker(true);
  }
</script>

{#if offlineReady}
  <div class="fixed bottom-4 right-4 z-50 bg-green-600 text-white px-4 py-3 rounded-lg shadow-lg flex items-center gap-3">
    <span>App ready to work offline</span>
    <button onclick={close} class="text-white/80 hover:text-white">
      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
      </svg>
    </button>
  </div>
{/if}

{#if needRefresh}
  <div class="fixed bottom-4 right-4 z-50 bg-indigo-600 text-white px-4 py-3 rounded-lg shadow-lg">
    <div class="flex items-center gap-4">
      <span>New version available!</span>
      <button 
        onclick={handleUpdate}
        class="bg-white text-indigo-600 px-3 py-1 rounded font-medium hover:bg-indigo-50"
      >
        Update
      </button>
      <button onclick={close} class="text-white/80 hover:text-white">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>
  </div>
{/if}
```

### 2.3 Add PWA Component to Layout

Update `frontend/src/routes/+layout.svelte` to include the PWA component:

```svelte
<script>
  // ... existing imports
  import PWAReloadPrompt from '$lib/components/PWAReloadPrompt.svelte';
</script>

<!-- Add near the end of the template -->
<PWAReloadPrompt />
```

---

## Phase 3: Offline Support (Read-Only Mode)

### 3.1 Create Offline Detection Store

Update `frontend/src/lib/stores/pwa.ts` to include online/offline detection:

```typescript
import { writable } from 'svelte/store';
import { browser } from '$app/environment';

interface PWAState {
  needsRefresh: boolean;
  offlineReady: boolean;
  isOnline: boolean;
  updateServiceWorker?: () => Promise<void>;
}

function createPWAStore() {
  const { subscribe, set, update } = writable<PWAState>({
    needsRefresh: false,
    offlineReady: false,
    isOnline: browser ? navigator.onLine : true
  });

  if (browser) {
    window.addEventListener('online', () => {
      update((state) => ({ ...state, isOnline: true }));
    });

    window.addEventListener('offline', () => {
      update((state) => ({ ...state, isOnline: false }));
    });
  }

  return { subscribe, set, update };
}

export const pwaStore = createPWAStore();
```

### 3.2 Create Offline Banner Component

Create `frontend/src/lib/components/OfflineBanner.svelte`:

```svelte
<script lang="ts">
  import { pwaStore } from '$lib/stores/pwa';

  let isOnline = $derived($pwaStore.isOnline);
</script>

{#if !isOnline}
  <div class="fixed top-0 left-0 right-0 z-50 bg-amber-500 text-amber-900 text-center py-2 px-4 text-sm font-medium">
    <span>📴 You're offline - viewing cached data (read-only mode)</span>
  </div>
{/if}
```

### 3.3 Modify API Client for Offline Support

Update `frontend/src/lib/api/client.ts` to handle offline gracefully:

```typescript
import { browser } from '$app/environment';

const API_BASE = import.meta.env.VITE_API_URL !== undefined ? import.meta.env.VITE_API_URL : '';

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public code?: string,
    public data?: Record<string, unknown>
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export class OfflineError extends Error {
  constructor() {
    super('You are offline. This action requires an internet connection.');
    this.name = 'OfflineError';
  }
}

function isOnline(): boolean {
  return !browser || navigator.onLine;
}

// Helper to check if action requires network
function requiresNetwork(method: string): boolean {
  return ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method);
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    if (response.status === 401) {
      window.location.href = '/auth/login';
    }

    const errorText = await response.text();
    let errorMessage = `HTTP ${response.status}`;
    let errorCode: string | undefined;
    let errorData: Record<string, unknown> | undefined;
    try {
      const errorJson = JSON.parse(errorText);
      errorMessage = errorJson.message || errorJson.title || errorMessage;
      errorCode = errorJson.code;
      errorData = errorJson;
    } catch {
      errorMessage = errorText || errorMessage;
    }
    throw new ApiError(response.status, errorMessage, errorCode, errorData);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json();
}

export async function apiGet<T>(endpoint: string): Promise<T> {
  const response = await fetch(`${API_BASE}/api${endpoint}`, {
    method: 'GET',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF': '1'
    }
  });
  return handleResponse<T>(response);
}

export async function apiPost<T>(endpoint: string, data?: unknown): Promise<T> {
  if (!isOnline()) {
    throw new OfflineError();
  }
  
  const response = await fetch(`${API_BASE}/api${endpoint}`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF': '1'
    },
    body: data ? JSON.stringify(data) : undefined
  });
  return handleResponse<T>(response);
}

export async function apiPut<T>(endpoint: string, data: unknown): Promise<T> {
  if (!isOnline()) {
    throw new OfflineError();
  }
  
  const response = await fetch(`${API_BASE}/api${endpoint}`, {
    method: 'PUT',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF': '1'
    },
    body: JSON.stringify(data)
  });
  return handleResponse<T>(response);
}

export async function apiPatch<T>(endpoint: string, data?: unknown): Promise<T> {
  if (!isOnline()) {
    throw new OfflineError();
  }
  
  const response = await fetch(`${API_BASE}/api${endpoint}`, {
    method: 'PATCH',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF': '1'
    },
    body: data ? JSON.stringify(data) : undefined
  });
  return handleResponse<T>(response);
}

export async function apiDelete<T>(endpoint: string): Promise<T> {
  if (!isOnline()) {
    throw new OfflineError();
  }
  
  const response = await fetch(`${API_BASE}/api${endpoint}`, {
    method: 'DELETE',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF': '1'
    }
  });
  return handleResponse<T>(response);
}
```

### 3.4 Create Disabled Button Wrapper

Create `frontend/src/lib/components/OfflineDisabled.svelte`:

```svelte
<script lang="ts">
  import { pwaStore } from '$lib/stores/pwa';

  interface Props {
    children: any;
    class?: string;
  }

  let { children, class: className = '' }: Props = $props();
  let isOnline = $derived($pwaStore.isOnline);
</script>

<div class={className} class:opacity-50={!isOnline} class:pointer-events-none={!isOnline}>
  {@render children()}
</div>

{#if !isOnline}
  <span class="sr-only">Disabled while offline</span>
{/if}
```

---

## Phase 4: Push Notifications

### 4.1 Backend Changes Required

Create a new endpoint to save push subscriptions. Add to the backend API:

```csharp
// POST /api/notifications/push/subscribe
// Body: { endpoint, keys: { p256dh, auth } }

// POST /api/notifications/push/unsubscribe
// Body: { endpoint }
```

You'll need:
- Store VAPID keys (generate once, store in config)
- Database table for push subscriptions
- Background service to send push notifications

### 4.2 Generate VAPID Keys

```bash
npx web-push generate-vapid-keys
```

Store public key in frontend env, private key in backend.

### 4.3 Create Push Notification Service

Create `frontend/src/lib/services/pushNotifications.ts`:

```typescript
import { browser } from '$app/environment';
import { apiPost, apiDelete } from '$lib/api/client';

const VAPID_PUBLIC_KEY = import.meta.env.VITE_VAPID_PUBLIC_KEY;

export async function subscribeToPushNotifications(): Promise<boolean> {
  if (!browser || !('serviceWorker' in navigator) || !('PushManager' in window)) {
    console.log('Push notifications not supported');
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

    // Send subscription to backend
    await apiPost('/notifications/push/subscribe', subscription.toJSON());
    
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
      await apiDelete('/notifications/push/unsubscribe');
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

// Helper function
function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding)
    .replace(/-/g, '+')
    .replace(/_/g, '/');
  
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  
  return outputArray;
}
```

### 4.4 Add Push Handler to Service Worker

The `@vite-pwa/sveltekit` plugin allows custom service worker code. Create `frontend/src/sw.ts`:

```typescript
/// <reference lib="webworker" />
import { precacheAndRoute, cleanupOutdatedCaches } from 'workbox-precaching';

declare let self: ServiceWorkerGlobalScope;

// Precache assets
precacheAndRoute(self.__WB_MANIFEST);
cleanupOutdatedCaches();

// Handle push notifications
self.addEventListener('push', (event) => {
  if (!event.data) return;

  const data = event.data.json();
  
  const options: NotificationOptions = {
    body: data.body,
    icon: '/pwa-192x192.png',
    badge: '/pwa-192x192.png',
    vibrate: [100, 50, 100],
    data: {
      url: data.url || '/'
    },
    actions: data.actions || []
  };

  event.waitUntil(
    self.registration.showNotification(data.title || 'Help Motivate Me', options)
  );
});

// Handle notification click
self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  const url = event.notification.data?.url || '/';

  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
      // Focus existing window if available
      for (const client of clientList) {
        if (client.url === url && 'focus' in client) {
          return client.focus();
        }
      }
      // Otherwise open new window
      if (self.clients.openWindow) {
        return self.clients.openWindow(url);
      }
    })
  );
});
```

Update `vite.config.ts` to use custom service worker:

```typescript
SvelteKitPWA({
  // ... existing config
  strategies: 'injectManifest',
  srcDir: 'src',
  filename: 'sw.ts',
  // ...
})
```

---

## Phase 5: Deployment & Cache Invalidation (Pitfalls)

### 5.1 The Problem

When deploying new versions:
- Users may have old CSS/JS cached
- Service worker may serve stale content
- Hard refresh doesn't always work

### 5.2 Solutions Implemented

#### A. Use `registerType: 'prompt'`
Users get notified when a new version is available and can choose to update.

#### B. Version-Based Asset Names (Already handled by Vite)
Vite automatically hashes asset filenames, so new deployments get new URLs.

#### C. Service Worker Update Check
The PWAReloadPrompt component checks for updates every hour:

```typescript
setInterval(() => {
  registration.update();
}, 60 * 60 * 1000);
```

#### D. Skip Waiting on Update
When user clicks "Update", the new service worker immediately takes over:

```typescript
await updateServiceWorker(true); // true = skip waiting
```

#### E. Clear Old Caches
Workbox's `cleanupOutdatedCaches()` removes old precache entries.

### 5.3 Additional Deployment Headers

Configure your web server/CDN to set proper cache headers:

```
# For HTML files - no cache
Cache-Control: no-cache, no-store, must-revalidate

# For hashed assets (JS, CSS with hash in filename)
Cache-Control: public, max-age=31536000, immutable

# For service worker
Cache-Control: no-cache
Service-Worker-Allowed: /
```

### 5.4 Force Update Strategy (Emergency)

If you need to force all users to update, you can:

1. Change the service worker URL/scope (nuclear option)
2. Use a version check endpoint that forces refresh
3. Add version mismatch detection in your API responses

---

## Phase 6: Testing Checklist

### 6.1 PWA Installation
- [ ] App is installable on Chrome/Edge desktop
- [ ] App is installable on Android Chrome
- [ ] App is installable on iOS Safari (Add to Home Screen)
- [ ] App icon and name appear correctly
- [ ] Splash screen shows on launch

### 6.2 Offline Mode
- [ ] Offline banner appears when network is lost
- [ ] Cached pages load when offline
- [ ] Goals list shows cached data offline
- [ ] Identities show cached data offline
- [ ] Create/Update/Delete buttons are disabled offline
- [ ] Appropriate error messages when attempting actions offline

### 6.3 Update Flow
- [ ] "New version available" prompt appears after deployment
- [ ] Clicking "Update" refreshes and loads new version
- [ ] Old CSS/JS is not served after update
- [ ] Service worker updates successfully

### 6.4 Push Notifications
- [ ] Permission prompt appears
- [ ] Subscription is saved to backend
- [ ] Notifications are received when app is closed
- [ ] Clicking notification opens correct page
- [ ] Unsubscribe works correctly

### 6.5 Chrome DevTools Testing
- [ ] Lighthouse PWA audit passes
- [ ] Application > Service Workers shows registered SW
- [ ] Application > Cache Storage shows cached assets
- [ ] Network tab shows requests served from cache when offline

---

## Implementation Order

1. **Week 1: Basic PWA**
   - [ ] Install dependencies
   - [ ] Configure vite plugin
   - [ ] Create PWA icons
   - [ ] Update app.html
   - [ ] Test installation

2. **Week 2: Update Handling**
   - [ ] Create PWA store
   - [ ] Create PWAReloadPrompt component
   - [ ] Add to layout
   - [ ] Test update flow

3. **Week 3: Offline Support**
   - [ ] Add online/offline detection
   - [ ] Create OfflineBanner component
   - [ ] Modify API client for offline errors
   - [ ] Disable mutation buttons when offline
   - [ ] Test offline mode

4. **Week 4: Push Notifications**
   - [ ] Backend: Create push subscription endpoints
   - [ ] Backend: Generate and store VAPID keys
   - [ ] Frontend: Create push notification service
   - [ ] Frontend: Add notification settings UI
   - [ ] Custom service worker for push handling
   - [ ] Test notifications

---

## Environment Variables

Add to `.env`:

```env
# Frontend
VITE_VAPID_PUBLIC_KEY=your_vapid_public_key

# Backend
VAPID_PRIVATE_KEY=your_vapid_private_key
VAPID_PUBLIC_KEY=your_vapid_public_key
VAPID_SUBJECT=mailto:your@email.com
```

---

## Common Pitfalls & Solutions

| Pitfall | Solution |
|---------|----------|
| Users stuck on old version | Use `registerType: 'prompt'` + update prompt UI |
| CSS not updating | Vite hashes filenames; ensure no manual cache headers override |
| Service worker not updating | Add version check interval + skip waiting on update |
| iOS PWA limitations | Test thoroughly; some features limited on iOS |
| Push not working on iOS | Web Push requires iOS 16.4+ and is still limited |
| Offline data getting stale | Set reasonable `maxAgeSeconds` in cache config |
| Authentication issues offline | Cache auth state locally; show login prompt when reconnected |
| Large cache size | Limit `maxEntries` in cache config |

---

## Future Enhancements (Sync Support)

For future offline write support with sync:

1. **IndexedDB for pending actions**
   - Store mutations when offline
   - Queue them for sync when online

2. **Background Sync API**
   - Use `sync` event in service worker
   - Retry failed requests automatically

3. **Conflict Resolution**
   - Timestamp-based or last-write-wins
   - Or show conflict UI for user resolution

---

## Resources

- [Vite PWA Plugin Docs](https://vite-pwa-org.netlify.app/)
- [SvelteKit PWA Docs](https://vite-pwa-org.netlify.app/frameworks/sveltekit.html)
- [Workbox Documentation](https://developer.chrome.com/docs/workbox/)
- [Web Push Notifications](https://web.dev/push-notifications-overview/)
- [PWA Builder](https://www.pwabuilder.com/) - Testing tool
