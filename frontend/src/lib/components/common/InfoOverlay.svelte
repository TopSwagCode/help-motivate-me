<script lang="ts">
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';

	interface Props {
		title: string;
		description: string;
		showFaqLink?: boolean;
	}

	let { title, description, showFaqLink = true }: Props = $props();

	let showOverlay = $state(false);

	function openOverlay() {
		showOverlay = true;
	}

	function closeOverlay() {
		showOverlay = false;
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			closeOverlay();
		}
	}

	function handleBackdropClick(e: MouseEvent) {
		if (e.target === e.currentTarget) {
			closeOverlay();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

<div class="flex items-center gap-2">
	<h1 class="text-xl sm:text-2xl font-bold text-gray-900">{title}</h1>
	<button
		onclick={openOverlay}
		class="p-1.5 text-gray-400 hover:text-primary-600 hover:bg-primary-50 rounded-full transition-colors touch-manipulation"
		title={get(t)('common.info.whatIsThis')}
		aria-label={get(t)('common.info.whatIsThis')}
	>
		<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="2"
				d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
			/>
		</svg>
	</button>
</div>

{#if showOverlay}
	<!-- Backdrop -->
	<div
		class="fixed inset-0 bg-black/50 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 animate-fade-in"
		onclick={handleBackdropClick}
		role="button"
		tabindex="-1"
	>
		<!-- Modal -->
		<div
			class="bg-white rounded-t-2xl sm:rounded-2xl shadow-2xl w-full sm:max-w-2xl max-h-[90vh] overflow-y-auto animate-slide-up"
		>
			<!-- Header -->
			<div class="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between rounded-t-2xl sm:rounded-t-2xl">
				<h2 class="text-xl font-bold text-gray-900">{title}</h2>
				<button
					onclick={closeOverlay}
					class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors touch-manipulation"
					aria-label={get(t)('common.close')}
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
			<div class="px-6 py-6">
				<div class="prose prose-sm sm:prose max-w-none">
					{@html description}
				</div>

				{#if showFaqLink}
					<div class="mt-6 pt-6 border-t border-gray-200">
						<p class="text-sm text-gray-600 mb-3">
							{$t('common.info.moreQuestions')}
						</p>
						<a
							href="/faq"
							class="inline-flex items-center gap-2 text-primary-600 hover:text-primary-700 font-medium text-sm"
						>
							<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
								/>
							</svg>
							{$t('common.info.visitFaq')}
						</a>
					</div>
				{/if}
			</div>

			<!-- Footer -->
			<div class="sticky bottom-0 bg-gray-50 px-6 py-4 border-t border-gray-200 rounded-b-2xl sm:rounded-b-2xl">
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
			transform: translateY(100%);
		}
		to {
			transform: translateY(0);
		}
	}

	.animate-fade-in {
		animation: fade-in 0.2s ease-out;
	}

	.animate-slide-up {
		animation: slide-up 0.3s ease-out;
	}

	@media (min-width: 640px) {
		@keyframes slide-up {
			from {
				transform: translateY(2rem);
				opacity: 0;
			}
			to {
				transform: translateY(0);
				opacity: 1;
			}
		}
	}
</style>
