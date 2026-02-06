<script lang="ts">
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { tour } from '$lib/stores/tour';
	import { auth } from '$lib/stores/auth';
	import { resetOnboarding } from '$lib/api/onboarding';

	interface Props {
		isOpen: boolean;
		onClose: () => void;
	}

	let { isOpen, onClose }: Props = $props();

	// Video element reference for volume control
	let videoElement = $state<HTMLVideoElement | null>(null);

	// Onboarding reset state
	let resetLoading = $state(false);

	// Set video volume when element is available
	$effect(() => {
		if (videoElement) {
			videoElement.volume = 0.1;
		}
	});

	// Track which accordion sections are expanded
	let expandedSections = $state<Set<string>>(new Set());

	// Auto-expand section based on current page
	$effect(() => {
		if (isOpen) {
			const path = $page.url.pathname;
			let sectionToExpand = '';

			if (path.startsWith('/today')) sectionToExpand = 'today';
			else if (path.startsWith('/identities')) sectionToExpand = 'identities';
			else if (path.startsWith('/goals')) sectionToExpand = 'goals';
			else if (path.startsWith('/habit-stacks')) sectionToExpand = 'habitStacks';
			else if (path.startsWith('/journal')) sectionToExpand = 'journal';

			if (sectionToExpand) {
				expandedSections = new Set([sectionToExpand]);
			} else {
				expandedSections = new Set();
			}
		}
	});

	function toggleSection(section: string) {
		const newSections = new Set(expandedSections);
		if (newSections.has(section)) {
			newSections.delete(section);
		} else {
			newSections.add(section);
		}
		expandedSections = newSections;
	}

	async function handleRestartTour() {
		onClose();
		await goto('/today');
		// Short delay to allow the page to render before starting the tour
		setTimeout(() => {
			tour.resetTour();
		}, 300);
	}

	async function handleRestartOnboarding() {
		if (!confirm(get(t)('settings.onboarding.confirmReset'))) {
			return;
		}

		resetLoading = true;

		try {
			const updatedUser = await resetOnboarding();
			auth.updateUser(updatedUser);
			onClose();
			goto('/onboarding');
		} catch (e) {
			alert(e instanceof Error ? e.message : get(t)('settings.onboarding.failedReset'));
			resetLoading = false;
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape') {
			onClose();
		}
	}

	const sections = [
		{ id: 'today', icon: 'today' },
		{ id: 'identities', icon: 'identities' },
		{ id: 'goals', icon: 'goals' },
		{ id: 'habitStacks', icon: 'habitStacks' },
		{ id: 'journal', icon: 'journal' },
		{ id: 'aiAssistant', icon: 'aiAssistant' }
	];
</script>

{#snippet sectionIcon(iconType: string)}
	{#if iconType === 'today'}
		<svg class="w-5 h-5 text-cocoa-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
		</svg>
	{:else if iconType === 'identities'}
		<svg class="w-5 h-5 text-cocoa-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
		</svg>
	{:else if iconType === 'goals'}
		<svg class="w-5 h-5 text-cocoa-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
		</svg>
	{:else if iconType === 'habitStacks'}
		<svg class="w-5 h-5 text-cocoa-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
		</svg>
	{:else if iconType === 'journal'}
		<svg class="w-5 h-5 text-cocoa-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
		</svg>
	{:else if iconType === 'aiAssistant'}
		<svg class="w-5 h-5 text-cocoa-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
		</svg>
	{/if}
{/snippet}

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<!-- Backdrop -->
	<!-- svelte-ignore a11y_interactive_supports_focus -->
	<div
		class="fixed inset-0 bg-black/50 z-50 flex items-end sm:items-center justify-center"
		onclick={onClose}
		role="dialog"
		aria-modal="true"
		aria-labelledby="help-popup-title"
	>
		<!-- Modal (bottom sheet on mobile, centered on desktop) -->
		<div
			class="bg-warm-paper rounded-t-2xl sm:rounded-xl shadow-xl w-full sm:max-w-lg max-h-[85vh] sm:max-h-[70vh] overflow-hidden flex flex-col"
			onclick={(e) => e.stopPropagation()}
		>
			<!-- Header -->
			<div class="px-4 py-3 border-b border-gray-100 flex items-center justify-between flex-shrink-0">
				<div>
					<h2 id="help-popup-title" class="text-base font-semibold text-cocoa-800">
						{$t('help.title')}
					</h2>
					<p class="text-xs text-cocoa-500">{$t('help.subtitle')}</p>
				</div>
				<button
					onclick={onClose}
					class="p-1 text-gray-400 hover:text-cocoa-600 rounded-2xl hover:bg-primary-50 transition-colors"
					aria-label={$t('common.close')}
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					</svg>
				</button>
			</div>

			<!-- Scrollable Content -->
			<div class="flex-1 overflow-y-auto p-4">
				<!-- Guided Tour CTA -->
				<div class="mb-4 p-4 bg-white rounded-xl border border-primary-200">
					<!-- Text and Video side by side -->
					<div class="flex items-start gap-4">
						<div class="flex items-start gap-3 flex-1">
							<div class="w-10 h-10 rounded-full bg-primary-500 flex items-center justify-center flex-shrink-0">
								<svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
								</svg>
							</div>
							<div class="flex-1">
								<h3 class="font-semibold text-cocoa-800 text-sm">{$t('help.tour.title')}</h3>
								<p class="text-xs text-cocoa-600 mt-0.5">{$t('help.tour.description')}</p>
							</div>
						</div>
						<!-- Tour Preview Video (cropped container) -->
						<div class="flex-shrink-0 w-24 sm:w-32 overflow-hidden rounded-lg video-crop">
							<video
								bind:this={videoElement}
								class="w-full h-auto object-contain"
								style="margin-top: calc(-1 * var(--crop-top)); margin-bottom: calc(-1 * var(--crop-bottom));"
								autoplay
								loop
								playsinline
							>
								<source src="/manual.webm" type="video/webm" />
							</video>
						</div>
					</div>
					<!-- Button spanning full width below -->
					<button
						onclick={handleRestartTour}
						class="mt-3 w-full px-4 py-2 bg-primary-500 hover:bg-primary-600 text-white text-sm font-medium rounded-full transition-colors"
					>
						{$t('help.tour.button')}
					</button>
				</div>

				<!-- Restart Onboarding CTA -->
				<div class="mb-4 p-4 bg-white rounded-xl border border-primary-100">
					<div class="flex items-start gap-3">
						<div class="w-10 h-10 rounded-full bg-cocoa-100 flex items-center justify-center flex-shrink-0">
							<svg class="w-5 h-5 text-cocoa-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
							</svg>
						</div>
						<div class="flex-1">
							<h3 class="font-semibold text-cocoa-800 text-sm">{$t('settings.onboarding.title')}</h3>
							<p class="text-xs text-cocoa-600 mt-0.5">{$t('settings.onboarding.description')}</p>
							<button
								onclick={handleRestartOnboarding}
								disabled={resetLoading}
								class="mt-2 px-4 py-1.5 bg-cocoa-100 hover:bg-cocoa-200 text-cocoa-700 text-xs font-medium rounded-full transition-colors disabled:opacity-50"
							>
								{resetLoading ? $t('settings.onboarding.restarting') : $t('settings.onboarding.restart')}
							</button>
						</div>
					</div>
				</div>

				<!-- Accordion Sections -->
				<div class="space-y-2">
					{#each sections as section (section.id)}
						<div class="border border-primary-100 rounded-xl overflow-hidden">
							<button
								onclick={() => toggleSection(section.id)}
								class="w-full px-4 py-3 flex items-center justify-between bg-warm-paper hover:bg-primary-50 transition-colors"
								aria-expanded={expandedSections.has(section.id)}
							>
								<span class="flex items-center gap-2">
									{@render sectionIcon(section.icon)}
									<span class="font-medium text-cocoa-800 text-sm">{$t(`help.sections.${section.id}.title`)}</span>
								</span>
								<svg
									class="w-4 h-4 text-cocoa-400 transition-transform duration-200 {expandedSections.has(section.id) ? 'rotate-180' : ''}"
									fill="none"
									stroke="currentColor"
									viewBox="0 0 24 24"
								>
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
								</svg>
							</button>

							{#if expandedSections.has(section.id)}
								<div class="px-4 pb-3 pt-1 border-t border-primary-50 bg-white/50">
									<ul class="space-y-2">
										{#each $t(`help.sections.${section.id}.tips`) as tip}
											<li class="flex items-start gap-2 text-sm text-cocoa-600">
												<span class="text-primary-500 mt-0.5">•</span>
												<span>{tip}</span>
											</li>
										{/each}
									</ul>
								</div>
							{/if}
						</div>
					{/each}
				</div>
			</div>

			<!-- Footer -->
			<div class="px-4 py-3 border-t border-gray-100 bg-warm-cream/50 flex-shrink-0">
				<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
					<div class="text-xs text-cocoa-500">
						{$t('help.faq.title')}
					</div>
					<div class="flex gap-3">
						<a
							href="/faq"
							onclick={onClose}
							class="text-sm text-primary-600 hover:text-primary-700 font-medium flex items-center gap-1"
						>
							{$t('help.faq.link')}
							<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
							</svg>
						</a>
						<a
							href="/contact"
							onclick={onClose}
							class="text-sm text-primary-600 hover:text-primary-700 font-medium flex items-center gap-1"
						>
							{$t('help.contact.link')}
							<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
							</svg>
						</a>
					</div>
				</div>
			</div>
		</div>
	</div>
{/if}

<style>
	.video-crop {
		--crop-top: 20px;
		--crop-bottom: 20px;
	}

	@media (min-width: 640px) {
		.video-crop {
			--crop-top: 40px;
			--crop-bottom: 40px;
		}
	}
</style>
