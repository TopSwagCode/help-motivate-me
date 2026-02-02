<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { DailyCommitment, YesterdayCommitment } from '$lib/types/dailyCommitment';

	interface Props {
		commitment: DailyCommitment | null;
		yesterdayCommitment: YesterdayCommitment;
		onStartCommitment: () => void;
		onComplete: (commitmentId: string) => Promise<void>;
		onDismiss: (commitmentId: string) => Promise<void>;
		readonly?: boolean;
	}

	let {
		commitment,
		yesterdayCommitment,
		onStartCommitment,
		onComplete,
		onDismiss,
		readonly = false
	}: Props = $props();

	let isCompleting = $state(false);
	let isDismissing = $state(false);
	let showCelebration = $state(false);

	async function handleComplete() {
		if (!commitment || isCompleting) return;
		isCompleting = true;
		try {
			await onComplete(commitment.id);
			showCelebration = true;
			setTimeout(() => {
				showCelebration = false;
			}, 3000);
		} finally {
			isCompleting = false;
		}
	}

	async function handleDismiss() {
		if (!commitment || isDismissing) return;
		isDismissing = true;
		try {
			await onDismiss(commitment.id);
		} finally {
			isDismissing = false;
		}
	}
</script>

<div class="rounded-xl overflow-hidden shadow-sm border border-gray-100">
	<!-- Yesterday missed warning -->
	{#if yesterdayCommitment.wasMissed && !commitment}
		<div class="bg-amber-50 border-b border-amber-100 px-4 py-3">
			<div class="flex items-start gap-3">
				<span class="text-lg flex-shrink-0">🌅</span>
				<div class="flex-1 min-w-0">
					<p class="text-sm font-medium text-amber-800">
						{$t('dailyCommitment.recoveryTitle')}
					</p>
					<p class="text-xs text-amber-600 mt-0.5">
						{$t('dailyCommitment.recoveryMessage', { values: { identity: yesterdayCommitment.identityName } })}
					</p>
				</div>
			</div>
		</div>
	{/if}

	<!-- Main content area -->
	<div
		class="p-4"
		style="background: linear-gradient(135deg, {commitment?.identityColor || '#d4944c'}08 0%, {commitment?.identityColor || '#d4944c'}15 100%)"
	>
		{#if !commitment}
			<!-- No commitment - Start your day prompt -->
			<div class="text-center py-4">
				<div class="w-14 h-14 mx-auto mb-3 rounded-full bg-warm-paper shadow-sm flex items-center justify-center">
					<span class="text-2xl">🎯</span>
				</div>
				<h3 class="text-lg font-semibold text-cocoa-800 mb-1">
					{$t('dailyCommitment.title')}
				</h3>
				<p class="text-sm text-cocoa-600 mb-4 max-w-xs mx-auto">
					{$t('dailyCommitment.prompt')}
				</p>
				{#if !readonly}
					<button
						onclick={onStartCommitment}
						class="px-5 py-2.5 bg-primary-600 hover:bg-primary-700 text-white font-medium rounded-2xl transition-all hover:scale-105 active:scale-95 shadow-sm"
					>
						{$t('dailyCommitment.startButton')}
					</button>
				{/if}
			</div>

		{:else if commitment.status === 'Committed'}
			<!-- Active commitment - In progress -->
			<div class="flex items-start gap-3">
				<div
					class="w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0"
					style="background-color: {commitment.identityColor || '#d4944c'}20"
				>
					<span class="text-xl">{commitment.identityIcon || '🎯'}</span>
				</div>
				<div class="flex-1 min-w-0">
					<div class="flex items-center gap-2 mb-1">
						<span
							class="text-xs font-medium px-2 py-0.5 rounded-full"
							style="background-color: {commitment.identityColor || '#d4944c'}20; color: {commitment.identityColor || '#d4944c'}"
						>
							{commitment.identityName}
						</span>
					</div>
					<p class="text-sm font-medium text-cocoa-800 mb-3">
						{commitment.actionDescription}
					</p>
					{#if !readonly}
						<div class="flex items-center gap-2">
							<button
								onclick={handleComplete}
								disabled={isCompleting}
								class="flex-1 px-4 py-2 bg-green-500 hover:bg-green-600 text-white font-medium rounded-2xl transition-all hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
							>
								{#if isCompleting}
									<svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
										<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
										<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
									</svg>
								{:else}
									<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
									</svg>
								{/if}
								{$t('dailyCommitment.completeButton')}
							</button>
							<button
								onclick={handleDismiss}
								disabled={isDismissing}
								class="px-3 py-2 text-cocoa-500 hover:text-cocoa-700 hover:bg-primary-50 rounded-2xl transition-colors disabled:opacity-50"
								title={$t('dailyCommitment.dismissButton')}
							>
								<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
								</svg>
							</button>
						</div>
					{/if}
				</div>
			</div>

		{:else if commitment.status === 'Completed'}
			<!-- Completed - Celebration state -->
			<div class="flex items-center gap-3 {showCelebration ? 'animate-pulse' : ''}">
				<div
					class="w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0 bg-green-100"
				>
					<img src="/done.png" alt="Done" class="w-8 h-8 object-contain" />
				</div>
				<div class="flex-1 min-w-0">
					<div class="flex items-center gap-2 mb-1">
						<span class="text-xs font-medium px-2 py-0.5 rounded-full bg-green-100 text-green-700">
							{$t('dailyCommitment.completedLabel')}
						</span>
						<span
							class="text-xs font-medium px-2 py-0.5 rounded-full"
							style="background-color: {commitment.identityColor || '#d4944c'}20; color: {commitment.identityColor || '#d4944c'}"
						>
							{commitment.identityName}
						</span>
					</div>
					<p class="text-sm text-cocoa-600 line-through">
						{commitment.actionDescription}
					</p>
					<p class="text-sm font-medium text-green-600 mt-1">
						{$t('dailyCommitment.reinforcement', { values: { identity: commitment.identityName } })}
					</p>
				</div>
			</div>

		{:else if commitment.status === 'Dismissed'}
			<!-- Dismissed state -->
			<div class="flex items-center gap-3 opacity-60">
				<div
					class="w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0 bg-gray-100"
				>
					<span class="text-xl">{commitment.identityIcon || '🎯'}</span>
				</div>
				<div class="flex-1 min-w-0">
					<div class="flex items-center gap-2 mb-1">
						<span class="text-xs font-medium px-2 py-0.5 rounded-full bg-gray-100 text-cocoa-500">
							{$t('dailyCommitment.dismissedLabel')}
						</span>
					</div>
					<p class="text-sm text-gray-400 line-through">
						{commitment.actionDescription}
					</p>
				</div>
			</div>
		{/if}
	</div>
</div>
