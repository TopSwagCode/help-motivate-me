<script lang="ts">
	import { t } from 'svelte-i18n';
	import { onMount } from 'svelte';
	import { browser } from '$app/environment';

	// PWA install prompt
	let deferredPrompt: any = null;
	let canInstall = $state(false);
	let isInstalled = $state(false);
	let isIOS = $state(false);
	let isAndroid = $state(false);
	let isMacOS = $state(false);
	let installing = $state(false);

	onMount(() => {
		if (!browser) return;

		// Detect platform
		const userAgent = navigator.userAgent.toLowerCase();
		isIOS = /iphone|ipad|ipod/.test(userAgent) && !(window as any).MSStream;
		isAndroid = /android/.test(userAgent);
		isMacOS = /macintosh|mac os x/.test(userAgent) && !isIOS;

		// Check if already installed (standalone mode)
		isInstalled = window.matchMedia('(display-mode: standalone)').matches 
			|| (window.navigator as any).standalone === true;

		// Listen for beforeinstallprompt event
		window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);

		// Listen for app installed event
		window.addEventListener('appinstalled', () => {
			isInstalled = true;
			canInstall = false;
			deferredPrompt = null;
		});

		return () => {
			window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
		};
	});

	function handleBeforeInstallPrompt(e: Event) {
		e.preventDefault();
		deferredPrompt = e;
		canInstall = true;
	}

	async function installApp() {
		if (!deferredPrompt) return;

		installing = true;
		try {
			deferredPrompt.prompt();
			const { outcome } = await deferredPrompt.userChoice;
			
			if (outcome === 'accepted') {
				isInstalled = true;
				canInstall = false;
			}
			deferredPrompt = null;
		} catch (e) {
			console.error('Install failed:', e);
		} finally {
			installing = false;
		}
	}
</script>

<div class="space-y-6">
	<div>
		<h2 class="text-lg font-semibold text-gray-900 mb-1">{$t('settings.app.title')}</h2>
		<p class="text-sm text-gray-500">{$t('settings.app.description')}</p>
	</div>

	<!-- Install Status -->
	<div class="bg-gray-50 rounded-lg p-4">
		{#if isInstalled}
			<!-- Already installed -->
			<div class="flex items-center gap-3">
				<div class="flex-shrink-0 w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
					<svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
					</svg>
				</div>
				<div>
					<h3 class="font-medium text-gray-900">{$t('settings.app.installed.title')}</h3>
					<p class="text-sm text-gray-500">{$t('settings.app.installed.description')}</p>
				</div>
			</div>
		{:else if canInstall}
			<!-- Can install via prompt -->
			<div class="flex items-center justify-between gap-4">
				<div class="flex items-center gap-3">
					<div class="flex-shrink-0 w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
						<svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
						</svg>
					</div>
					<div>
						<h3 class="font-medium text-gray-900">{$t('settings.app.install.title')}</h3>
						<p class="text-sm text-gray-500">{$t('settings.app.install.description')}</p>
					</div>
				</div>
				<button
					onclick={installApp}
					disabled={installing}
					class="btn btn-primary flex-shrink-0"
				>
					{#if installing}
						<svg class="animate-spin -ml-1 mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24">
							<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
							<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
						</svg>
						{$t('common.loading')}
					{:else}
						{$t('settings.app.install.button')}
					{/if}
				</button>
			</div>
		{:else}
			<!-- Manual instructions needed -->
			<div class="flex items-center gap-3">
				<div class="flex-shrink-0 w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
					<svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
					</svg>
				</div>
				<div>
					<h3 class="font-medium text-gray-900">{$t('settings.app.manual.title')}</h3>
					<p class="text-sm text-gray-500">{$t('settings.app.manual.description')}</p>
				</div>
			</div>
		{/if}
	</div>

	<!-- Installation Instructions -->
	{#if !isInstalled}
		<div class="border border-gray-200 rounded-lg overflow-hidden">
			<div class="bg-gray-50 px-4 py-3 border-b border-gray-200">
				<h3 class="font-medium text-gray-900">{$t('settings.app.instructions.title')}</h3>
			</div>
			
			<div class="p-4 space-y-4">
				<!-- iOS Instructions -->
				<details class="group" open={isIOS}>
					<summary class="flex items-center justify-between cursor-pointer list-none">
						<div class="flex items-center gap-3">
							<div class="w-8 h-8 bg-gray-100 rounded-lg flex items-center justify-center">
								<svg class="w-5 h-5 text-gray-600" viewBox="0 0 24 24" fill="currentColor">
									<path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.81-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11z"/>
								</svg>
							</div>
							<span class="font-medium text-gray-900">{$t('settings.app.instructions.ios.title')}</span>
						</div>
						<svg class="w-5 h-5 text-gray-400 group-open:rotate-180 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
						</svg>
					</summary>
					<ol class="mt-3 ml-11 space-y-2 text-sm text-gray-600 list-decimal list-inside">
						<li>{$t('settings.app.instructions.ios.step1')}</li>
						<li>{$t('settings.app.instructions.ios.step2')}</li>
						<li>{$t('settings.app.instructions.ios.step3')}</li>
					</ol>
				</details>

				<hr class="border-gray-200" />

				<!-- Android Instructions -->
				<details class="group" open={isAndroid}>
					<summary class="flex items-center justify-between cursor-pointer list-none">
						<div class="flex items-center gap-3">
							<div class="w-8 h-8 bg-gray-100 rounded-lg flex items-center justify-center">
								<svg class="w-5 h-5 text-gray-600" viewBox="0 0 24 24" fill="currentColor">
									<path d="M17.6 9.48l1.84-3.18c.16-.31.04-.69-.26-.85-.29-.15-.65-.06-.83.22l-1.88 3.24c-1.38-.59-2.93-.92-4.56-.92s-3.18.33-4.56.92L5.5 5.67c-.18-.29-.54-.37-.83-.22-.31.16-.43.54-.26.85L6.24 9.5C3.59 10.8 1.74 13.16 1.5 16h21c-.24-2.84-2.09-5.2-4.74-6.52zM7 13c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm10 0c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1z"/>
								</svg>
							</div>
							<span class="font-medium text-gray-900">{$t('settings.app.instructions.android.title')}</span>
						</div>
						<svg class="w-5 h-5 text-gray-400 group-open:rotate-180 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
						</svg>
					</summary>
					<ol class="mt-3 ml-11 space-y-2 text-sm text-gray-600 list-decimal list-inside">
						<li>{$t('settings.app.instructions.android.step1')}</li>
						<li>{$t('settings.app.instructions.android.step2')}</li>
						<li>{$t('settings.app.instructions.android.step3')}</li>
					</ol>
				</details>

				<hr class="border-gray-200" />

				<!-- Desktop Instructions -->
				<details class="group" open={!isIOS && !isAndroid}>
					<summary class="flex items-center justify-between cursor-pointer list-none">
						<div class="flex items-center gap-3">
							<div class="w-8 h-8 bg-gray-100 rounded-lg flex items-center justify-center">
								<svg class="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
								</svg>
							</div>
							<span class="font-medium text-gray-900">{$t('settings.app.instructions.desktop.title')}</span>
						</div>
						<svg class="w-5 h-5 text-gray-400 group-open:rotate-180 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
						</svg>
					</summary>
					<ol class="mt-3 ml-11 space-y-2 text-sm text-gray-600 list-decimal list-inside">
						<li>{$t('settings.app.instructions.desktop.step1')}</li>
						<li>{$t('settings.app.instructions.desktop.step2')}</li>
						<li>{$t('settings.app.instructions.desktop.step3')}</li>
					</ol>
				</details>
			</div>
		</div>
	{/if}

	<!-- Benefits Section -->
	<div class="border border-gray-200 rounded-lg overflow-hidden">
		<div class="bg-gray-50 px-4 py-3 border-b border-gray-200">
			<h3 class="font-medium text-gray-900">{$t('settings.app.benefits.title')}</h3>
		</div>
		<div class="p-4">
			<ul class="space-y-3">
				<li class="flex items-start gap-3">
					<div class="flex-shrink-0 w-6 h-6 bg-green-100 rounded-full flex items-center justify-center mt-0.5">
						<svg class="w-3.5 h-3.5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<div>
						<p class="font-medium text-gray-900 text-sm">{$t('settings.app.benefits.offline.title')}</p>
						<p class="text-sm text-gray-500">{$t('settings.app.benefits.offline.description')}</p>
					</div>
				</li>
				<li class="flex items-start gap-3">
					<div class="flex-shrink-0 w-6 h-6 bg-green-100 rounded-full flex items-center justify-center mt-0.5">
						<svg class="w-3.5 h-3.5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<div>
						<p class="font-medium text-gray-900 text-sm">{$t('settings.app.benefits.fast.title')}</p>
						<p class="text-sm text-gray-500">{$t('settings.app.benefits.fast.description')}</p>
					</div>
				</li>
				<li class="flex items-start gap-3">
					<div class="flex-shrink-0 w-6 h-6 bg-green-100 rounded-full flex items-center justify-center mt-0.5">
						<svg class="w-3.5 h-3.5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<div>
						<p class="font-medium text-gray-900 text-sm">{$t('settings.app.benefits.notifications.title')}</p>
						<p class="text-sm text-gray-500">{$t('settings.app.benefits.notifications.description')}</p>
					</div>
				</li>
				<li class="flex items-start gap-3">
					<div class="flex-shrink-0 w-6 h-6 bg-green-100 rounded-full flex items-center justify-center mt-0.5">
						<svg class="w-3.5 h-3.5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
						</svg>
					</div>
					<div>
						<p class="font-medium text-gray-900 text-sm">{$t('settings.app.benefits.homescreen.title')}</p>
						<p class="text-sm text-gray-500">{$t('settings.app.benefits.homescreen.description')}</p>
					</div>
				</li>
			</ul>
		</div>
	</div>
</div>
