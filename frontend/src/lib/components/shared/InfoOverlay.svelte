<script lang="ts">
	import { t } from 'svelte-i18n';

	interface Props {
		title: string;
		description: string;
		learnMoreKey?: string; // i18n key for "Learn more" link text
		showFaqLink?: boolean;
	}

	let { title, description, learnMoreKey, showFaqLink = true }: Props = $props();

	let showOverlay = $state(false);

	function openOverlay() {
		showOverlay = true;
	}

	function closeOverlay() {
		showOverlay = false;
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && showOverlay) {
			e.preventDefault();
			closeOverlay();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

<div class="flex items-center gap-2 sm:gap-3">
	<h1 class="text-xl sm:text-2xl font-bold text-cocoa-800">{title}</h1>
	<button
		onclick={openOverlay}
		class="flex items-center gap-1 text-sm text-primary-600 hover:text-primary-700 font-medium transition-colors touch-manipulation"
		aria-label="Learn more about {title}"
	>
		<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="2"
				d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
			/>
		</svg>
		<span class="hidden xs:inline">{learnMoreKey ? $t(learnMoreKey) : 'What is this?'}</span>
	</button>
</div>

<!-- Overlay Modal -->
{#if showOverlay}
	<!-- Backdrop -->
	<button
		type="button"
		class="fixed inset-0 bg-black/50 z-50 animate-fade-in"
		onclick={closeOverlay}
		aria-label="Close information"
	></button>

	<!-- Modal -->
	<div class="fixed inset-0 z-50 flex items-start justify-center p-4 sm:p-6 md:p-8 overflow-y-auto pointer-events-none">
		<div class="pointer-events-auto bg-warm-paper rounded-xl shadow-2xl max-w-2xl w-full my-8 animate-slide-up">
			<!-- Header -->
			<div class="flex items-start justify-between p-4 sm:p-6 border-b border-primary-100">
				<div class="flex items-center gap-3">
					<div class="w-10 h-10 rounded-2xl bg-primary-100 flex items-center justify-center flex-shrink-0">
						<svg class="w-6 h-6 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
							/>
						</svg>
					</div>
					<h2 class="text-lg sm:text-xl font-semibold text-cocoa-800">{title}</h2>
				</div>
				<button
					onclick={closeOverlay}
					class="text-gray-400 hover:text-cocoa-600 p-1 rounded-2xl hover:bg-primary-50 transition-colors touch-manipulation"
					aria-label="Close"
				>
					<svg class="w-5 h-5 sm:w-6 sm:h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M6 18L18 6M6 6l12 12"
						/>
					</svg>
				</button>
			</div>

			<!-- Content -->
			<div class="p-4 sm:p-6">
				<div class="prose prose-sm sm:prose max-w-none text-cocoa-700">
					<!-- Allow HTML in description for formatting -->
					{@html description}
				</div>

				<!-- FAQ Link -->
				{#if showFaqLink}
					<div class="mt-6 pt-6 border-t border-primary-100">
						<p class="text-sm text-cocoa-600 mb-3">
							{$t('common.info.moreQuestions')}
						</p>
						<a
							href="/faq"
							class="inline-flex items-center gap-2 text-sm font-medium text-primary-600 hover:text-primary-700 transition-colors"
						>
							<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
								/>
							</svg>
							<span>{$t('common.info.visitFaq')}</span>
						</a>
					</div>
				{/if}
			</div>

			<!-- Footer -->
			<div class="px-4 sm:px-6 pb-4 sm:pb-6">
				<button
					onclick={closeOverlay}
					class="w-full btn-primary justify-center"
				>
					{$t('common.gotIt')}
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	@keyframes fade-in {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
	}

	@keyframes slide-up {
		from {
			opacity: 0;
			transform: translateY(20px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}

	.animate-fade-in {
		animation: fade-in 200ms ease-out;
	}

	.animate-slide-up {
		animation: slide-up 300ms ease-out;
	}

	/* Prose styles for better text formatting */
	:global(.prose p) {
		margin-bottom: 1em;
	}

	:global(.prose p:last-child) {
		margin-bottom: 0;
	}

	:global(.prose strong) {
		font-weight: 600;
		color: #111827;
	}

	:global(.prose ul, .prose ol) {
		margin-top: 0.5em;
		margin-bottom: 1em;
		padding-left: 1.5em;
	}

	:global(.prose li) {
		margin-bottom: 0.25em;
	}
</style>
