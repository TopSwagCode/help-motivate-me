import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import { SvelteKitPWA } from '@vite-pwa/sveltekit';

export default defineConfig({
	plugins: [
		sveltekit(),
		SvelteKitPWA({
			srcDir: 'src',
			filename: 'service-worker.ts',
			mode: 'production',
			strategies: 'injectManifest',
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
						src: 'android-chrome-192x192.png',
						sizes: '192x192',
						type: 'image/png'
					},
					{
						src: 'android-chrome-512x512.png',
						sizes: '512x512',
						type: 'image/png'
					},
					{
						src: 'android-chrome-512x512.png',
						sizes: '512x512',
						type: 'image/png',
						purpose: 'maskable'
					}
				]
			},
			injectManifest: {
				globPatterns: ['client/**/*.{js,css,ico,png,svg,webp,woff,woff2}'],
				globIgnores: ['**/sw*', '**/*.html']
			},
			kit: {
				includeVersionFile: true
			},
			devOptions: {
				enabled: false
			}
		})
	],
	server: {
		port: 5173,
		strictPort: true
	}
});
