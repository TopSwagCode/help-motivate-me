<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { t } from 'svelte-i18n';

	// Redirect to /today if already logged in
	$effect(() => {
		if ($auth.initialized && $auth.user) {
			goto('/today');
		}
	});

	function handleGetStarted() {
		if ($auth.user) {
			goto('/today');
		} else {
			goto('/auth/login');
		}
	}

	let welcomeVideo: HTMLVideoElement;
	let portalVideo: HTMLVideoElement;
	let videoState: 'idle' | 'waiting' | 'portal' | 'ended' = $state('idle');

	function handleVideoClick() {
		if (videoState !== 'idle') return;
		videoState = 'waiting';
		// Remove loop so the current playback ends naturally
		welcomeVideo.loop = false;
	}

	function handleWelcomeEnded() {
		if (videoState !== 'waiting') return;
		videoState = 'portal';
		portalVideo.currentTime = 0;
		portalVideo.play();
	}

	function handlePortalEnded() {
		videoState = 'ended';
	}
</script>

<div class="min-h-screen flex flex-col">
	<!-- Hero Section -->
	<main class="flex-1 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
		<div class="max-w-3xl mx-auto text-center">
			<h1 class="text-4xl sm:text-5xl lg:text-6xl font-bold text-cocoa-800 tracking-tight">
				{$t('landing.hero.title1')}
				<span class="text-primary-600">{$t('landing.hero.title2')}</span>
			</h1>
			<p class="mt-6 text-lg sm:text-xl text-cocoa-600 max-w-2xl mx-auto">
				{$t('landing.hero.subtitle')}
			</p>
			<div class="mt-10 flex flex-col sm:flex-row gap-4 justify-center">
				<button onclick={handleGetStarted} class="btn-primary text-base px-8 py-3">
					{$t('landing.hero.getStarted')}
				</button>
				<a href="/auth/login" class="btn-secondary text-base px-8 py-3">
					{$t('landing.hero.signIn')}
				</a>
			</div>
		</div>
	</main>

	<!-- Meet Milo - Your Gentle Motivator -->
	<section class="py-12 px-4 sm:px-6 lg:px-8 bg-primary-50 border-t border-primary-100">
		<div class="max-w-2xl mx-auto text-center">
			<h2 class="text-2xl font-bold text-cocoa-800 mb-2">{$t('landing.milo.title')}</h2>
			<p class="text-primary-600 font-medium mb-6">{$t('landing.milo.subtitle')}</p>

			<!-- svelte-ignore a11y_click_events_have_key_events -->
			<!-- svelte-ignore a11y_no_static_element_interactions -->
			<div
				class="video-container mb-6 rounded-2xl overflow-hidden mx-auto relative"
				class:animate-ring={videoState === 'waiting' || videoState === 'portal'}
				class:cursor-pointer={videoState === 'idle'}
				style="max-width: 464px; width: 100%; aspect-ratio: 464 / 688;"
				onclick={handleVideoClick}
			>
				{#if videoState === 'waiting' || videoState === 'portal'}
					{#each Array(12) as _, i}
						<span class="sparkle" style="--i:{i};"></span>
					{/each}
				{/if}
				<video
					bind:this={welcomeVideo}
					autoplay
					loop
					muted
					playsinline
					class="w-full h-full object-contain absolute inset-0"
					class:hidden={videoState === 'portal' || videoState === 'ended'}
					onended={handleWelcomeEnded}
				>
					<source src="/welcome.webm" type="video/webm" />
				</video>
				<video
					bind:this={portalVideo}
					muted
					playsinline
					preload="auto"
					controls
					class="w-full h-full object-contain absolute inset-0"
					class:hidden={videoState !== 'portal' && videoState !== 'ended'}
					onended={handlePortalEnded}
				>
					<source src="/portal-intro.webm" type="video/webm" />
				</video>
			</div>

			{#if videoState === 'idle'}
				<p class="text-sm text-primary-500 mb-4 animate-pulse">{$t('landing.milo.clickHint')}</p>
			{/if}

			<p class="text-cocoa-600 leading-relaxed max-w-lg mx-auto">
				{$t('landing.milo.description')}
			</p>
		</div>
	</section>

	<!-- Features Preview -->
	<section class="py-16 px-4 sm:px-6 lg:px-8 bg-warm-paper border-t border-primary-100">
		<div class="max-w-6xl mx-auto">
			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-2xl flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-cocoa-800">{$t('landing.features.identity.title')}</h3>
					<p class="mt-2 text-cocoa-600">{$t('landing.features.identity.description')}</p>
				</div>
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-2xl flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-cocoa-800">{$t('landing.features.goals.title')}</h3>
					<p class="mt-2 text-cocoa-600">{$t('landing.features.goals.description')}</p>
				</div>
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-2xl flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-cocoa-800">{$t('landing.features.tasks.title')}</h3>
					<p class="mt-2 text-cocoa-600">{$t('landing.features.tasks.description')}</p>
				</div>
				<div class="text-center p-6">
					<div class="w-12 h-12 mx-auto mb-4 bg-primary-100 rounded-2xl flex items-center justify-center">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
						</svg>
					</div>
					<h3 class="text-lg font-semibold text-cocoa-800">{$t('landing.features.habits.title')}</h3>
					<p class="mt-2 text-cocoa-600">{$t('landing.features.habits.description')}</p>
				</div>
			</div>
		</div>
	</section>

	<!-- Footer -->
	<footer class="py-8 px-4 text-center text-sm text-cocoa-500 border-t border-primary-100">
		<div class="mb-4 flex flex-wrap justify-center gap-4">
			<a href="/about" class="text-primary-600 hover:text-primary-700 font-medium">{$t('landing.footer.about')}</a>
			<a href="/pricing" class="text-primary-600 hover:text-primary-700 font-medium">{$t('landing.footer.pricing')}</a>
			<a href="/faq" class="text-primary-600 hover:text-primary-700 font-medium">{$t('landing.footer.faq')}</a>
			<a href="/contact" class="text-primary-600 hover:text-primary-700 font-medium">{$t('landing.footer.contact')}</a>
		</div>
		<div class="mb-4 flex flex-wrap justify-center gap-4 text-gray-400">
			<a href="/privacy" class="hover:text-cocoa-600">{$t('landing.footer.privacy')}</a>
			<span class="hidden sm:inline">|</span>
			<a href="/terms" class="hover:text-cocoa-600">{$t('landing.footer.terms')}</a>
		</div>
		<p>{@html $t('landing.footer.copyright', { values: { year: new Date().getFullYear() } })}</p>
	</footer>
</div>

<style>
	.video-container {
		transition: box-shadow 0.4s ease;
	}

	.animate-ring {
		box-shadow:
			0 0 0 4px #e8b87a80,
			0 0 20px 6px #e8b87a4d,
			0 0 40px 12px #f2d4ae26;
		animation: pulse-ring 1.5s ease-in-out infinite;
	}

	@keyframes pulse-ring {
		0%, 100% {
			box-shadow:
				0 0 0 4px #e8b87a80,
				0 0 20px 6px #e8b87a4d,
				0 0 40px 12px #f2d4ae26;
		}
		50% {
			box-shadow:
				0 0 0 8px #e8b87a4d,
				0 0 30px 10px #e8b87a33,
				0 0 60px 20px #f2d4ae1a;
		}
	}

	.sparkle {
		position: absolute;
		width: 8px;
		height: 8px;
		border-radius: 50%;
		background: radial-gradient(circle, #fbbf24, #f59e0b);
		pointer-events: none;
		z-index: 10;
		animation: sparkle-fly 1.8s ease-out forwards;
		animation-delay: calc(var(--i) * 0.1s);
		opacity: 0;
		/* Start from center */
		top: 50%;
		left: 50%;
	}

	@keyframes sparkle-fly {
		0% {
			transform: translate(-50%, -50%) scale(0);
			opacity: 0;
		}
		15% {
			opacity: 1;
			transform: translate(-50%, -50%) scale(1.2);
		}
		100% {
			opacity: 0;
			transform:
				translate(
					calc(-50% + cos(calc(var(--i) * 30deg)) * 180px),
					calc(-50% + sin(calc(var(--i) * 30deg)) * 140px)
				)
				scale(0.3);
		}
	}
</style>
