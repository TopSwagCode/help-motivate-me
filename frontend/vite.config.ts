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
			registerType: 'prompt',
			scope: '/',
			base: '/',
			manifest: {
				name: 'Help Motivate Me',
				short_name: 'Motivate Me',
				description: 'Track your goals, build habits, and become who you want to be',
				theme_color: '#4F46E5',
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
				navigationPreload: true,
				navigationFallback: '/',
				navigationFallbackDenylist: [/^\/api/, /^\/auth\/callback/],
				runtimeCaching: [
					{
						urlPattern: /^https?:\/\/.*\/api\/(goals|identities|today|habit-stacks|journal)/,
						handler: 'NetworkFirst',
						options: {
							cacheName: 'api-cache',
							expiration: {
								maxEntries: 100,
								maxAgeSeconds: 60 * 60 * 24
							},
							cacheableResponse: {
								statuses: [0, 200]
							},
							networkTimeoutSeconds: 3
						}
					},
					{
						urlPattern: /\.(?:png|jpg|jpeg|svg|gif|webp)$/,
						handler: 'CacheFirst',
						options: {
							cacheName: 'image-cache',
							expiration: {
								maxEntries: 50,
								maxAgeSeconds: 60 * 60 * 24 * 30
							}
						}
					},
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
								maxAgeSeconds: 60 * 60 * 24 * 365
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
