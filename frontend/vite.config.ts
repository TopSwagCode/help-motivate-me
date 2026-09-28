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
			manifest: false,
			injectManifest: {
				globPatterns: ['client/**/*.{js,css,ico,png,svg,webp,webm,webmanifest,woff,woff2}'],
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
