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

<div class="flex items-center gap-2 sm:gap-3">
	<h1 class="text-xl sm:text-2xl font-bold text-gray-900">{title}</h1>
	<button
		onclick={openOverlay}
		class="group relative flex items-center justify-center w-8 h-8 sm:w-9 sm:h-9 text-primary-500 hover:text-primary-600 bg-primary-50 hover:bg-primary-100 rounded-full transition-all duration-200 touch-manipulation hover:scale-110 active:scale-95 shadow-sm hover:shadow-md"
		title={get(t)('common.info.whatIsThis')}
		aria-label={get(t)('common.info.whatIsThis')}
	>
		<svg class="w-5 h-5 sm:w-5.5 sm:h-5.5 transition-transform group-hover:rotate-12" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path
				stroke-linecap="round"
				stroke-linejoin="round"
				stroke-width="2.5"
				d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
			/>
		</svg>
		<!-- Tooltip for desktop -->
		<span class="absolute hidden sm:block -bottom-8 left-1/2 -translate-x-1/2 px-2 py-1 bg-gray-900 text-white text-xs rounded whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
			{get(t)('common.info.whatIsThis')}
		</span>
	</button>
</div>

{#if showOverlay}
	<!-- Backdrop with blur -->
	<div
		class="fixed inset-0 bg-gradient-to-br from-black/60 via-black/50 to-black/60 backdrop-blur-sm z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 animate-fade-in"
		onclick={handleBackdropClick}
		onkeydown={(e) => e.key === 'Escape' && handleBackdropClick(e as unknown as MouseEvent)}
		role="button"
		tabindex="0"
		aria-label={get(t)('common.close')}
	>
		<!-- Modal -->
		<div
			class="bg-white rounded-t-3xl sm:rounded-3xl shadow-2xl w-full sm:max-w-2xl max-h-[92vh] overflow-hidden flex flex-col animate-slide-up"
		>
			<!-- Decorative header bar -->
			<div class="h-1.5 bg-gradient-to-r from-primary-400 via-primary-500 to-primary-600"></div>
			
			<!-- Header -->
			<div class="relative bg-gradient-to-br from-primary-50 via-white to-white px-6 sm:px-8 pt-6 pb-5 border-b border-primary-100">
				<div class="flex items-start justify-between gap-4">
					<div class="flex-1">
						<div class="flex items-center gap-3 mb-2">
							<!-- Icon -->
							<div class="flex items-center justify-center w-10 h-10 sm:w-12 sm:h-12 bg-gradient-to-br from-primary-500 to-primary-600 rounded-2xl shadow-lg shadow-primary-500/30">
								<svg class="w-6 h-6 sm:w-7 sm:h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
									/>
								</svg>
							</div>
							<h2 class="text-xl sm:text-2xl font-bold text-gray-900">{title}</h2>
						</div>
						<p class="text-sm text-gray-600 ml-13 sm:ml-15">
							{$t('common.info.learnMore')}
						</p>
					</div>
					<button
						onclick={closeOverlay}
						class="flex-shrink-0 p-2 text-gray-400 hover:text-gray-600 hover:bg-white/80 rounded-xl transition-all duration-200 touch-manipulation hover:scale-110 active:scale-95"
						aria-label={get(t)('common.close')}
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2.5"
								d="M6 18L18 6M6 6l12 12"
							/>
						</svg>
					</button>
				</div>
			</div>

			<!-- Content -->
			<div class="flex-1 overflow-y-auto px-6 sm:px-8 py-6 sm:py-8">
				<!-- Main description with enhanced styling -->
				<div class="prose prose-sm sm:prose max-w-none prose-headings:text-gray-900 prose-p:text-gray-700 prose-p:leading-relaxed prose-strong:text-primary-700 prose-strong:font-semibold prose-ul:space-y-2 prose-li:text-gray-700">
					{@html description}
				</div>

				{#if showFaqLink}
					<!-- FAQ Link Section with card style -->
					<div class="mt-8 p-5 bg-gradient-to-br from-primary-50 to-blue-50 border border-primary-100 rounded-2xl">
						<div class="flex items-start gap-4">
							<div class="flex-shrink-0 w-10 h-10 bg-white rounded-xl shadow-sm flex items-center justify-center">
								<svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
									/>
								</svg>
							</div>
							<div class="flex-1 min-w-0">
								<p class="text-sm font-medium text-gray-900 mb-2">
									{$t('common.info.moreQuestions')}
								</p>
								<a
									href="/faq"
									class="inline-flex items-center gap-2 text-primary-600 hover:text-primary-700 font-semibold text-sm group transition-colors"
								>
									<span>{$t('common.info.visitFaq')}</span>
									<svg class="w-4 h-4 transition-transform group-hover:translate-x-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2.5"
											d="M9 5l7 7-7 7"
										/>
									</svg>
								</a>
							</div>
						</div>
					</div>
				{/if}
			</div>

			<!-- Footer -->
			<div class="sticky bottom-0 bg-white px-6 sm:px-8 py-4 sm:py-5 border-t border-gray-100 shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.05)]">
				<button
					onclick={closeOverlay}
					class="w-full btn-primary justify-center font-semibold shadow-lg shadow-primary-500/30 hover:shadow-xl hover:shadow-primary-500/40 transition-all duration-200"
				>
					<span>{$t('common.gotIt')}</span>
					<svg class="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2.5"
							d="M5 13l4 4L19 7"
						/>
					</svg>
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
			opacity: 0.8;
		}
		to {
			transform: translateY(0);
			opacity: 1;
		}
	}

	.animate-fade-in {
		animation: fade-in 0.25s ease-out;
	}

	.animate-slide-up {
		animation: slide-up 0.35s cubic-bezier(0.34, 1.56, 0.64, 1);
	}

	@media (min-width: 640px) {
		@keyframes slide-up {
			from {
				transform: translateY(3rem) scale(0.95);
				opacity: 0;
			}
			to {
				transform: translateY(0) scale(1);
				opacity: 1;
			}
		}
		
		.animate-slide-up {
			animation: slide-up 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
		}
	}
	
	/* Enhanced prose styling */
	:global(.prose ul) {
		list-style-type: none;
		padding-left: 0;
	}
	
	:global(.prose ul li) {
		position: relative;
		padding-left: 1.75rem;
		margin-bottom: 0.75rem;
	}
	
	:global(.prose ul li::before) {
		content: '';
		position: absolute;
		left: 0;
		top: 0.5rem;
		width: 0.375rem;
		height: 0.375rem;
		background: linear-gradient(135deg, #3b82f6, #2563eb);
		border-radius: 50%;
		box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
	}
	
	:global(.prose p:first-of-type) {
		font-size: 1.05em;
		color: #374151;
	}
</style>
