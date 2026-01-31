<script lang="ts">
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { commandBar } from '$lib/stores/commandBar';
	import type { TodayView, TodayTask, TodayHabitStack, IdentityFeedback, IdentityProgress } from '$lib/types';
	import type { BuddyTodayViewResponse, BuddyTodayTask, BuddyTodayHabitStack } from '$lib/types/buddy';
	import type { IdentityProof } from '$lib/types/identityProof';
	import VoteBreakdownPopover from './VoteBreakdownPopover.svelte';

	// Unified type to accept both TodayView and BuddyTodayViewResponse
	type TodayViewData = TodayView | BuddyTodayViewResponse;
	type TaskData = TodayTask | BuddyTodayTask;
	type HabitStackData = TodayHabitStack | BuddyTodayHabitStack;

	interface Props {
		todayData: TodayViewData;
		wins?: IdentityProof[];
		readonly?: boolean;
		// Event handlers for interactive mode
		onToggleHabitItem?: (itemId: string) => Promise<void>;
		onCompleteAllHabits?: (stackId: string) => Promise<void>;
		onToggleTask?: (taskId: string) => Promise<void>;
		onCompleteAllTasks?: () => Promise<void>;
		onSnoozeTask?: (task: TaskData) => Promise<void>;
		onPostponeTask?: (task: TaskData) => void;
		onEditTask?: (task: TaskData) => void;
		onLogIdentityProof?: () => void;
		onDeleteWin?: (winId: string) => Promise<void>;
		// Animation state (only needed for interactive mode)
		transitioningTaskIds?: string[];
		newlyArrivedTaskIds?: string[];
		snoozingTaskIds?: string[];
		removingAfterSnoozeIds?: string[];
	}

	let {
		todayData,
		wins = [],
		readonly = false,
		onToggleHabitItem,
		onCompleteAllHabits,
		onToggleTask,
		onCompleteAllTasks,
		onSnoozeTask,
		onPostponeTask,
		onEditTask,
		onLogIdentityProof,
		onDeleteWin,
		transitioningTaskIds = [],
		newlyArrivedTaskIds = [],
		snoozingTaskIds = [],
		removingAfterSnoozeIds = []
	}: Props = $props();

	// Type guard to check if data has identityProgress (TodayView)
	function hasIdentityProgress(data: TodayViewData): data is TodayView {
		return 'identityProgress' in data;
	}

	// Collapsible section states
	let sectionsExpanded = $state({
		identityProgress: true,
		habitStacks: true,
		wins: true,
		tasks: true,
		completedTasks: false
	});

	function toggleSection(section: keyof typeof sectionsExpanded) {
		sectionsExpanded[section] = !sectionsExpanded[section];
	}

	// Sort by due date: tasks with due dates come first (nearest first), then tasks without due dates
	function sortByDueDate(a: TaskData, b: TaskData): number {
		if (!a.dueDate && !b.dueDate) return 0;
		if (!a.dueDate) return 1;
		if (!b.dueDate) return -1;
		return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
	}

	const sortedUpcomingTasks = $derived(todayData.upcomingTasks.slice().sort(sortByDueDate));

	function formatRelativeDate(dateStr: string | null): string {
		if (!dateStr) return get(t)('today.dates.noDate');

		const today = new Date();
		today.setHours(0, 0, 0, 0);
		const dueDate = new Date(dateStr);
		dueDate.setHours(0, 0, 0, 0);

		const diffDays = Math.round((dueDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

		if (diffDays === 0) return get(t)('today.dates.today');
		if (diffDays === 1) return get(t)('today.dates.tomorrow');
		if (diffDays === -1) return get(t)('today.dates.yesterday');
		if (diffDays < 0) return get(t)('today.dates.daysAgo', { values: { count: Math.abs(diffDays) } });

		const currentLocale = get(locale) === 'da' ? 'da-DK' : 'en-US';
		if (diffDays < 7) {
			return dueDate.toLocaleDateString(currentLocale, { weekday: 'short', day: 'numeric' });
		}
		return dueDate.toLocaleDateString(currentLocale, { month: 'short', day: 'numeric' });
	}

	function getDueDateColor(dateStr: string | null): string {
		if (!dateStr) return 'text-gray-400';

		const today = new Date();
		today.setHours(0, 0, 0, 0);
		const dueDate = new Date(dateStr);
		dueDate.setHours(0, 0, 0, 0);

		const diffDays = Math.round((dueDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

		if (diffDays < 0) return 'text-red-600 font-medium';
		if (diffDays === 0) return 'text-red-600 font-medium';
		if (diffDays === 1) return 'text-orange-600';
		return 'text-gray-500';
	}

	function getStreakEmoji(streak: number): string {
		if (streak >= 30) return '🏆';
		if (streak >= 14) return '💪';
		if (streak >= 7) return '🔥';
		if (streak >= 3) return '⚡';
		return '';
	}

	async function handleToggleHabitItem(itemId: string) {
		if (readonly || !onToggleHabitItem) return;
		await onToggleHabitItem(itemId);
	}

	async function handleCompleteAllHabits(stackId: string) {
		if (readonly || !onCompleteAllHabits) return;
		await onCompleteAllHabits(stackId);
	}

	async function handleToggleTask(taskId: string) {
		if (readonly || !onToggleTask) return;
		await onToggleTask(taskId);
	}

	async function handleCompleteAllTasks() {
		if (readonly || !onCompleteAllTasks) return;
		await onCompleteAllTasks();
	}

	async function handleSnoozeTask(task: TaskData) {
		if (readonly || !onSnoozeTask) return;
		await onSnoozeTask(task);
	}

	function handlePostponeTask(task: TaskData) {
		if (readonly || !onPostponeTask) return;
		onPostponeTask(task);
	}

	function handleEditTask(task: TaskData) {
		if (readonly || !onEditTask) return;
		onEditTask(task);
	}

	async function handleDeleteWin(winId: string) {
		if (readonly || !onDeleteWin) return;
		await onDeleteWin(winId);
	}

	function getIntensityDots(intensity: string): number {
		switch (intensity) {
			case 'Easy': return 1;
			case 'Moderate': return 2;
			case 'Hard': return 3;
			default: return 1;
		}
	}

	// State for vote breakdown popover
	let activeFeedbackId: string | null = $state(null);

	// Create a lookup map for identity feedback by ID
	const feedbackByIdentityId = $derived(
		new Map(todayData.identityFeedback.map(f => [f.id, f]))
	);

	function handleFeedbackMouseEnter(feedbackId: string) {
		// Only show popover if there are votes for this identity
		if (feedbackByIdentityId.has(feedbackId)) {
			activeFeedbackId = feedbackId;
		}
	}

	function handleFeedbackMouseLeave() {
		activeFeedbackId = null;
	}

	function handleFeedbackClick(feedbackId: string) {
		// Toggle on click/tap for mobile, only if there are votes
		if (feedbackByIdentityId.has(feedbackId)) {
			activeFeedbackId = activeFeedbackId === feedbackId ? null : feedbackId;
		}
	}

	function handleClickOutside(event: MouseEvent) {
		const target = event.target as HTMLElement;
		if (!target.closest('[data-feedback-badge]')) {
			activeFeedbackId = null;
		}
	}
</script>

<svelte:window onclick={handleClickOutside} />

<div class="space-y-6">
	<!-- Identity Progress (merged with Identity Votes) -->
	{#if hasIdentityProgress(todayData) && todayData.identityProgress && todayData.identityProgress.length > 0}
		<section data-tour="identity-progress">
			<div class="flex items-center justify-between mb-2">
				<button
					onclick={() => toggleSection('identityProgress')}
					class="flex items-center gap-2 text-left group"
				>
					<h2 class="text-base font-semibold text-gray-900 flex items-center gap-2">
						<span>📊</span> {$t('today.identityProgress')}
					</h2>
					<svg
						class="w-4 h-4 text-gray-400 transition-transform {sectionsExpanded.identityProgress ? 'rotate-180' : ''}"
						fill="none" stroke="currentColor" viewBox="0 0 24 24"
					>
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
					</svg>
				</button>
			</div>
			{#if sectionsExpanded.identityProgress}
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					{#each todayData.identityProgress as progress (progress.id)}
						{@const feedback = feedbackByIdentityId.get(progress.id)}
						<div
							class="relative rounded-lg p-3 transition-all hover:scale-[1.02] {feedback ? 'cursor-pointer' : 'cursor-default'}"
							style="background-color: {progress.color}15; border: 1px solid {progress.color}30"
							data-feedback-badge
							role={feedback ? 'button' : undefined}
							tabindex={feedback ? 0 : undefined}
							onmouseenter={() => handleFeedbackMouseEnter(progress.id)}
							onmouseleave={handleFeedbackMouseLeave}
							onclick={(e) => { e.stopPropagation(); handleFeedbackClick(progress.id); }}
							onkeydown={(e) => { if (e.key === 'Enter' || e.key === ' ') handleFeedbackClick(progress.id); }}
						>
							<!-- Header: Icon + Score + Trend + Today's Votes -->
							<div class="flex items-center justify-between mb-1.5">
								<span class="text-xl">{progress.icon || '🎯'}</span>
								<div class="flex items-center gap-2">
									<!-- Today's votes badge (if any) -->
									{#if feedback}
										<span
											class="inline-flex items-center gap-1 px-1.5 py-0.5 text-xs font-bold rounded-full"
											style="background-color: {progress.color || '#6366f1'}; color: white"
											title={feedback.reinforcementMessage}
										>
											+{feedback.totalVotes}
										</span>
									{/if}
									<!-- Progress score + trend -->
									<div class="flex items-center gap-0.5">
										<span class="text-sm font-bold" style="color: {progress.color || '#374151'}">{progress.score}</span>
										{#if progress.trend === 'Up'}
											<span class="text-green-500 text-xs">↑</span>
										{:else if progress.trend === 'Down'}
											<span class="text-red-500 text-xs">↓</span>
										{:else}
											<span class="text-gray-400 text-xs">→</span>
										{/if}
									</div>
								</div>
							</div>
							<!-- Name (truncated) -->
							<p class="text-xs font-medium text-gray-700 truncate mb-1.5" title={progress.name}>{progress.name}</p>
							<!-- Mini progress bar -->
							<div class="bg-white/50 rounded-full h-1.5">
								<div
									class="h-1.5 rounded-full transition-all duration-500"
									style="width: {progress.score}%; background-color: {progress.color || '#9CA3AF'}"
								></div>
							</div>
							<!-- Vote breakdown popover -->
							{#if activeFeedbackId === progress.id && feedback}
								<VoteBreakdownPopover {feedback} />
							{/if}
						</div>
					{/each}
				</div>
			{/if}
		</section>
	{/if}

	<!-- Habit Stacks -->
	<section data-tour="habit-stacks">
		<button 
			onclick={() => toggleSection('habitStacks')}
			class="w-full flex items-center justify-between text-left mb-3 group"
		>
			<h2 class="text-base font-semibold text-gray-900 flex items-center gap-2">
				<span>🔗</span> {$t('today.habitStacks')}
				<span class="text-xs font-normal text-gray-500 bg-gray-100 px-1.5 py-0.5 rounded-full">{todayData.habitStacks.length}</span>
			</h2>
			<svg 
				class="w-4 h-4 text-gray-400 transition-transform {sectionsExpanded.habitStacks ? 'rotate-180' : ''}"
				fill="none" stroke="currentColor" viewBox="0 0 24 24"
			>
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
			</svg>
		</button>
		{#if sectionsExpanded.habitStacks}
			{#if todayData.habitStacks.length > 0}
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					{#each todayData.habitStacks as stack (stack.id)}
						<div 
							class="rounded-lg overflow-hidden transition-all group/stack"
							style="background-color: {stack.identityColor || '#6366f1'}08; border: 1px solid {stack.identityColor || '#6366f1'}20"
						>
							<!-- Stack Header -->
							<div 
								class="px-3 py-2"
								style="background-color: {stack.identityColor || '#6366f1'}15"
							>
								<div class="flex items-center gap-2 mb-2">
									{#if stack.identityIcon}
										<span 
											class="text-sm flex-shrink-0 px-1.5 py-0.5 rounded-full"
											style="background-color: {stack.identityColor || '#6366f1'}25"
											title={stack.identityName}
										>
											{stack.identityIcon}
										</span>
									{/if}
									<span class="font-medium text-gray-800 text-sm truncate flex-1">{stack.name}</span>
									{#if stack.completedCount === stack.totalCount}
										<span class="flex items-center gap-1 text-xs font-medium text-green-600 bg-green-100 px-2 py-0.5 rounded-full">
											<svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
												<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
											</svg>
											{$t('today.done')}
										</span>
									{/if}
								</div>
								
								<!-- Progress bar and action area -->
								<div class="flex items-center gap-2">
									<!-- Progress bar -->
									<div class="flex-1 h-2 bg-white/50 rounded-full overflow-hidden">
										<div 
											class="h-full rounded-full transition-all duration-300"
											style="width: {(stack.completedCount / stack.totalCount) * 100}%; background-color: {stack.completedCount === stack.totalCount ? '#22c55e' : (stack.identityColor || '#6366f1')}"
										></div>
									</div>
									
									<!-- Progress count -->
									<span class="text-xs font-semibold tabular-nums" style="color: {stack.identityColor || '#6366f1'}">
										{stack.completedCount}/{stack.totalCount}
									</span>
									
									<!-- Complete all button -->
									{#if !readonly && stack.completedCount < stack.totalCount}
										<button
											onclick={() => handleCompleteAllHabits(stack.id)}
											class="flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-full transition-all hover:scale-105 active:scale-95"
											style="background-color: {stack.identityColor || '#6366f1'}; color: white"
											title={$t('today.completeAll')}
										>
											<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
											</svg>
											{$t('today.all')}
										</button>
									{/if}
								</div>
							</div>

							<!-- Vertical Habits List -->
							<div class="p-1.5 space-y-0.5">
								{#each stack.items as item, index (item.id)}
									{#if readonly}
										<div
											class="w-full flex items-center gap-2 px-2 py-2 rounded-lg text-left
												{item.isCompletedToday 
													? 'bg-green-500/15' 
													: 'bg-white/40'}"
										>
											<!-- Checkbox area -->
											<div class="flex flex-col items-center w-5 flex-shrink-0">
												{#if index > 0}
													<div 
														class="w-0.5 h-1.5 -mt-2 mb-0.5 rounded-full"
														style="background-color: {stack.items[index - 1]?.isCompletedToday ? '#22c55e' : (stack.identityColor || '#d1d5db')}40"
													></div>
												{/if}
												{#if item.isCompletedToday}
													<div class="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center shadow-sm">
														<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
															<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
														</svg>
													</div>
												{:else}
													<div 
														class="w-5 h-5 rounded-full border-2 bg-white"
														style="border-color: {stack.identityColor || '#6366f1'}"
													></div>
												{/if}
												{#if index < stack.items.length - 1}
													<div 
														class="w-0.5 h-1.5 mt-0.5 -mb-2 rounded-full"
														style="background-color: {item.isCompletedToday ? '#22c55e' : (stack.identityColor || '#d1d5db')}40"
													></div>
												{/if}
											</div>
											
											<!-- Habit text -->
											<span class="flex-1 text-xs {item.isCompletedToday ? 'text-green-700 line-through' : 'text-gray-700'}">
												{item.habitDescription}
											</span>
											
											<!-- Streak or pending indicator -->
											{#if item.currentStreak > 0}
												<span class="text-[10px] text-orange-500 flex-shrink-0">🔥{item.currentStreak}</span>
											{:else if !item.isCompletedToday}
												<span class="text-[10px] text-gray-400 flex-shrink-0">{$t('today.pending')}</span>
											{/if}
										</div>
									{:else}
										<button
											onclick={() => handleToggleHabitItem(item.id)}
											class="w-full flex items-center gap-2 px-2 py-2 rounded-lg transition-all text-left group/item
												{item.isCompletedToday 
													? 'bg-green-500/15' 
													: 'bg-white/40 hover:bg-white hover:shadow-sm cursor-pointer active:scale-[0.98]'}"
										>
											<!-- Checkbox area -->
											<div class="flex flex-col items-center w-5 flex-shrink-0">
												{#if index > 0}
													<div 
														class="w-0.5 h-1.5 -mt-2 mb-0.5 rounded-full transition-colors"
														style="background-color: {stack.items[index - 1]?.isCompletedToday ? '#22c55e' : (stack.identityColor || '#d1d5db')}40"
													></div>
												{/if}
												{#if item.isCompletedToday}
													<div class="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center shadow-sm">
														<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
															<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
														</svg>
													</div>
												{:else}
													<div 
														class="w-5 h-5 rounded-full border-2 flex items-center justify-center transition-all group-hover/item:border-3 group-hover/item:scale-110"
														style="border-color: {stack.identityColor || '#6366f1'}; background-color: white"
													>
														<div 
															class="w-2 h-2 rounded-full opacity-0 group-hover/item:opacity-100 transition-opacity"
															style="background-color: {stack.identityColor || '#6366f1'}40"
														></div>
													</div>
												{/if}
												{#if index < stack.items.length - 1}
													<div 
														class="w-0.5 h-1.5 mt-0.5 -mb-2 rounded-full transition-colors"
														style="background-color: {item.isCompletedToday ? '#22c55e' : (stack.identityColor || '#d1d5db')}40"
													></div>
												{/if}
											</div>
											
											<!-- Habit text -->
											<span class="flex-1 text-xs {item.isCompletedToday ? 'text-green-700 line-through' : 'text-gray-700 group-hover/item:text-gray-900'}">
												{item.habitDescription}
											</span>
											
											<!-- Streak or tap hint -->
											{#if item.currentStreak > 0}
												<span class="text-[10px] text-orange-500 flex-shrink-0">🔥{item.currentStreak}</span>
											{:else if !item.isCompletedToday}
												<span class="text-[10px] text-gray-400 opacity-0 group-hover/item:opacity-100 transition-opacity flex-shrink-0">
													{$t('today.tapToComplete')}
												</span>
											{/if}
										</button>
									{/if}
								{/each}
							</div>
						</div>
					{/each}
				</div>
			{:else}
				<div class="py-6 px-4 rounded-lg bg-gray-50 border border-gray-100">
					<p class="text-gray-500 flex flex-col sm:flex-row items-center justify-center gap-2 text-sm mb-3">{$t('today.noHabitStacks')}</p>
					{#if !readonly}
						<div class="flex flex-col sm:flex-row items-center justify-center gap-2 text-sm">
							<a href="/habit-stacks" class="text-primary-600 hover:text-primary-700 font-medium">
								{$t('today.createHabitStack')} →
							</a>
							<span class="text-gray-400 hidden sm:inline">•</span>
							<span class="text-gray-500 flex items-center gap-1">
								{$t('today.orUseAssistant')}
								<button
									type="button"
									onclick={() => commandBar.open()}
									class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-gradient-to-r from-primary-600 to-primary-700 text-white hover:scale-110 hover:shadow-md transition-all cursor-pointer"
									title="Open AI Assistant"
								>
									<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
									</svg>
								</button>
							</span>
						</div>
					{/if}
				</div>
			{/if}
		{/if}
	</section>

	<!-- Wins Section -->
	<section data-tour="wins-section">
		<div class="flex items-center justify-between mb-3">
			<button
				onclick={() => toggleSection('wins')}
				class="flex items-center gap-2 text-left group"
			>
				<h2 class="text-base font-semibold text-gray-900 flex items-center gap-2">
					<svg class="w-4 h-4 text-amber-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
					</svg>
					{$t('today.wins')}
					<span class="text-xs font-normal text-gray-500 bg-gray-100 px-1.5 py-0.5 rounded-full">{wins.length}</span>
				</h2>
				<svg
					class="w-4 h-4 text-gray-400 transition-transform {sectionsExpanded.wins ? 'rotate-180' : ''}"
					fill="none" stroke="currentColor" viewBox="0 0 24 24"
				>
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
				</svg>
			</button>
		</div>
		{#if sectionsExpanded.wins}
			{#if wins.length > 0}
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					{#each wins as win (win.id)}
						<div
							class="rounded-lg overflow-hidden transition-all group/win"
							style="background-color: {win.identityColor || '#f59e0b'}08; border: 1px solid {win.identityColor || '#f59e0b'}20"
						>
							<!-- Win Header -->
							<div
								class="px-3 py-2 flex items-center justify-between"
								style="background-color: {win.identityColor || '#f59e0b'}15"
							>
								<div class="flex items-center gap-2">
									{#if win.identityIcon}
										<span
											class="text-sm flex-shrink-0 px-1.5 py-0.5 rounded-full"
											style="background-color: {win.identityColor || '#f59e0b'}25"
											title={win.identityName}
										>
											{win.identityIcon}
										</span>
									{/if}
									<span class="font-medium text-gray-800 text-sm truncate">{win.identityName}</span>
								</div>
								<!-- Intensity dots -->
								<div class="flex items-center gap-1">
									<div class="flex gap-0.5">
										{#each Array(getIntensityDots(win.intensity)) as _, i}
											<div
												class="w-1.5 h-1.5 rounded-full"
												style="background-color: {win.identityColor || '#f59e0b'}"
											></div>
										{/each}
									</div>
									<span class="text-xs font-medium ml-1" style="color: {win.identityColor || '#f59e0b'}">
										+{win.voteValue}
									</span>
								</div>
							</div>

							<!-- Win Content -->
							<div class="p-3">
								{#if win.description}
									<p class="text-sm text-gray-700">{win.description}</p>
								{:else}
									<p class="text-sm text-gray-400 italic">{$t('identityProof.intensity.' + win.intensity.toLowerCase())}</p>
								{/if}

								<!-- Delete button (only in interactive mode) -->
								{#if !readonly}
									<div class="flex justify-end mt-2 pt-2 opacity-0 group-hover/win:opacity-100 transition-opacity">
										<button
											onclick={() => handleDeleteWin(win.id)}
											class="p-1.5 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded transition-colors"
											title={$t('common.delete')}
										>
											<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
											</svg>
										</button>
									</div>
								{/if}
							</div>
						</div>
					{/each}
				</div>
			{:else}
				<div class="py-6 px-4 rounded-lg bg-gray-50 border border-gray-100">
					<p class="text-gray-500 flex flex-col sm:flex-row items-center justify-center gap-2 text-sm mb-3">{$t('today.noWins')}</p>
					{#if !readonly && onLogIdentityProof}
						<div class="flex flex-col sm:flex-row items-center justify-center gap-2 text-sm">
							<button
								onclick={onLogIdentityProof}
								class="text-amber-600 hover:text-amber-700 font-medium flex items-center gap-1"
							>
								<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
								</svg>
								{$t('today.logFirstWin')} →
							</button>
							<span class="text-gray-400 hidden sm:inline">•</span>
							<span class="text-gray-500 flex items-center gap-1">
								{$t('today.orUseAssistant')}
								<button
									type="button"
									onclick={() => commandBar.open()}
									class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-gradient-to-r from-primary-600 to-primary-700 text-white hover:scale-110 hover:shadow-md transition-all cursor-pointer"
									title="Open AI Assistant"
								>
									<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
									</svg>
								</button>
							</span>
						</div>
					{/if}
				</div>
			{/if}
		{/if}
	</section>

	<!-- Upcoming Tasks -->
	<section data-tour="tasks-section">
		<div class="flex items-center justify-between mb-3">
			<button 
				onclick={() => toggleSection('tasks')}
				class="flex items-center gap-2 text-left group"
			>
				<h2 class="text-base font-semibold text-gray-900 flex items-center gap-2">
					<span>📋</span> {$t('today.tasks')}
					<span class="text-xs font-normal text-gray-500 bg-gray-100 px-1.5 py-0.5 rounded-full">{todayData.upcomingTasks.length}</span>
				</h2>
				<svg 
					class="w-4 h-4 text-gray-400 transition-transform {sectionsExpanded.tasks ? 'rotate-180' : ''}"
					fill="none" stroke="currentColor" viewBox="0 0 24 24"
				>
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
				</svg>
			</button>
			{#if !readonly && todayData.upcomingTasks.length > 0 && sectionsExpanded.tasks}
				<button
					onclick={handleCompleteAllTasks}
					class="px-2 py-1 text-xs font-medium text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-md transition-colors"
				>
					{$t('today.completeAll')}
				</button>
			{/if}
		</div>
		{#if sectionsExpanded.tasks}
			{#if sortedUpcomingTasks.length > 0}
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					{#each sortedUpcomingTasks as task (task.id)}
						<div
							class="relative rounded-lg overflow-hidden transition-all duration-300
								{!readonly && transitioningTaskIds.includes(task.id) ? 'opacity-50 scale-95' : ''}
								{!readonly && newlyArrivedTaskIds.includes(task.id) ? 'animate-slide-in-highlight' : ''}
								{!readonly && snoozingTaskIds.includes(task.id) ? 'bg-amber-50' : ''}
								{!readonly && removingAfterSnoozeIds.includes(task.id) ? 'animate-snooze-remove' : ''}"
							style="background-color: {task.identityColor || '#6366f1'}08; border: 1px solid {task.identityColor || '#6366f1'}20"
						>
							<!-- Snooze animation overlay -->
							{#if !readonly && snoozingTaskIds.includes(task.id)}
								<div class="absolute inset-0 flex items-center justify-center bg-amber-100/90 rounded-lg z-10">
									<span class="text-amber-700 font-bold flex items-center gap-1">
										💤 +7 days
									</span>
								</div>
							{/if}
							<!-- Removal message overlay -->
							{#if !readonly && removingAfterSnoozeIds.includes(task.id)}
								<div class="absolute inset-0 flex items-center justify-center bg-gray-100/90 rounded-lg z-10">
									<span class="text-gray-600 font-medium text-sm">
										📅 {$t('today.movedOutsideView')}
									</span>
								</div>
							{/if}

							<!-- Task Header with identity color -->
							<div
								class="px-3 py-1.5 flex items-center justify-between"
								style="background-color: {task.identityColor || '#6366f1'}15"
							>
								<div class="flex items-center gap-1.5">
									{#if task.identityIcon || task.identityName}
										<span
											class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-xs font-medium"
											style="background-color: {task.identityColor || '#6366f1'}25; color: {task.identityColor || '#6366f1'}"
											title={task.identityName}
										>
											{#if task.identityIcon}
												<span class="text-xs">{task.identityIcon}</span>
											{/if}
											{task.identityName || ''}
										</span>
									{:else}
										<span class="text-xs text-gray-400">{$t('today.noIdentity') || 'No identity'}</span>
									{/if}
								</div>
								<span class="text-xs font-medium {getDueDateColor(task.dueDate)} flex-shrink-0">
									{formatRelativeDate(task.dueDate)}
								</span>
							</div>

							<!-- Task Content -->
							<div class="p-3">
								<!-- Task Title Row: Checkbox + Title -->
								<div class="flex items-start gap-2">
									{#if readonly}
										<div class="flex-shrink-0 mt-0.5">
											<div class="w-5 h-5 rounded-full border-2 border-gray-300"></div>
										</div>
									{:else}
										<button
											onclick={() => handleToggleTask(task.id)}
											class="flex-shrink-0 mt-0.5 group"
											title="Mark as completed"
										>
											<div
												class="w-5 h-5 rounded-full border-2 transition-all duration-200 flex items-center justify-center
													{transitioningTaskIds.includes(task.id)
														? 'bg-green-500 border-green-500'
														: 'group-hover:bg-green-50'}"
												style={!transitioningTaskIds.includes(task.id) ? `border-color: ${task.identityColor || '#6366f1'}60` : ''}
											>
												{#if transitioningTaskIds.includes(task.id)}
													<svg class="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 24 24">
														<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
													</svg>
												{/if}
											</div>
										</button>
									{/if}
									<div class="flex-1 min-w-0">
										<p class="text-sm font-medium text-gray-900 leading-tight {!readonly && transitioningTaskIds.includes(task.id) ? 'line-through text-gray-400' : ''}">
											{task.title}
										</p>
									</div>
								</div>

								<!-- Task Meta Row -->
								<div class="flex items-center gap-2 mt-1.5 pl-7">
									<span class="text-xs text-gray-400 truncate">{task.goalTitle}</span>
								</div>

								<!-- Action Buttons (only in interactive mode) -->
								{#if !readonly}
									<div class="flex items-center justify-end gap-1 mt-2 pt-2">
										<button
											onclick={() => handleSnoozeTask(task)}
											class="p-1.5 text-gray-400 hover:text-amber-500 hover:bg-amber-50 rounded transition-colors"
											title="Snooze 1 week"
											disabled={snoozingTaskIds.includes(task.id)}
										>
											<span class="text-sm">💤</span>
										</button>
										<button
											onclick={() => handlePostponeTask(task)}
											class="p-1.5 text-gray-400 hover:text-primary-600 hover:bg-primary-50 rounded transition-colors"
											title="Postpone task"
										>
											<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
											</svg>
										</button>
										<button
											onclick={() => handleEditTask(task)}
											class="p-1.5 text-gray-400 hover:text-primary-600 hover:bg-primary-50 rounded transition-colors"
											title="Edit task"
										>
											<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
											</svg>
										</button>
									</div>
								{/if}
							</div>
						</div>
					{/each}
				</div>
			{:else}
				<div class="py-6 px-4 rounded-lg bg-gray-50 border border-gray-100">
					<p class="text-gray-500 flex flex-col sm:flex-row items-center justify-center gap-2 text-sm mb-3">{$t('today.noPendingTasks')}</p>
					{#if !readonly}
						<div class="flex flex-col sm:flex-row items-center justify-center gap-2 text-sm">
							<a href="/goals" class="text-primary-600 hover:text-primary-700 font-medium">
								{$t('today.goToGoals')} →
							</a>
							<span class="text-gray-400 hidden sm:inline">•</span>
							<span class="text-gray-500 flex items-center gap-1">
								{$t('today.orUseAssistant')}
								<button
									type="button"
									onclick={() => commandBar.open()}
									class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-gradient-to-r from-primary-600 to-primary-700 text-white hover:scale-110 hover:shadow-md transition-all cursor-pointer"
									title="Open AI Assistant"
								>
									<svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
									</svg>
								</button>
							</span>
						</div>
					{/if}
				</div>
			{/if}
		{/if}
	</section>

	<!-- Completed Tasks -->
	<section>
		<button 
			onclick={() => toggleSection('completedTasks')}
			class="w-full flex items-center justify-between text-left mb-3 group"
		>
			<h2 class="text-base font-semibold text-gray-900 flex items-center gap-2">
				<span>✅</span> {$t('today.completed')}
				<span class="text-xs font-normal text-gray-500 bg-gray-100 px-1.5 py-0.5 rounded-full">{todayData.completedTasks.length}</span>
			</h2>
			<svg 
				class="w-4 h-4 text-gray-400 transition-transform {sectionsExpanded.completedTasks ? 'rotate-180' : ''}"
				fill="none" stroke="currentColor" viewBox="0 0 24 24"
			>
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
			</svg>
		</button>
		{#if sectionsExpanded.completedTasks}
			{#if todayData.completedTasks.length > 0}
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
					{#each todayData.completedTasks as task (task.id)}
						<div
							class="relative rounded-lg overflow-hidden transition-all duration-300
								{!readonly && transitioningTaskIds.includes(task.id) ? 'opacity-50 scale-95' : ''}
								{!readonly && newlyArrivedTaskIds.includes(task.id) ? 'animate-slide-in-highlight-green' : ''}"
							style="background: linear-gradient(135deg, {task.identityColor || '#6366f1'}10, #f0fdf4); border: 1px solid {task.identityColor || '#22c55e'}30"
						>
							<!-- Completed Task Header -->
							<div
								class="px-2.5 py-1 flex items-center justify-between"
								style="background: linear-gradient(90deg, {task.identityColor || '#6366f1'}20, #dcfce7)"
							>
								{#if task.identityIcon || task.identityName}
									<span
										class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-xs font-medium opacity-80"
										style="background-color: {task.identityColor || '#6366f1'}25; color: {task.identityColor || '#6366f1'}"
										title={task.identityName}
									>
										{#if task.identityIcon}
											<span class="text-xs">{task.identityIcon}</span>
										{/if}
										{task.identityName || ''}
									</span>
								{:else}
									<span></span>
								{/if}
								<span class="text-xs text-green-600 font-medium flex items-center gap-0.5">
									<svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
										<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
									</svg>
								</span>
							</div>

							<!-- Task Content -->
							<div class="px-2.5 py-2 flex items-center gap-2">
								{#if readonly}
									<div class="flex-shrink-0">
										<div class="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center">
											<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
											</svg>
										</div>
									</div>
								{:else}
									<button
										onclick={() => handleToggleTask(task.id)}
										class="flex-shrink-0 group"
										title="Mark as incomplete"
									>
										<div class="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center group-hover:bg-green-600 transition-colors">
											<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
											</svg>
										</div>
									</button>
								{/if}
								<p class="flex-1 text-sm text-gray-500 line-through truncate">
									{task.title}
								</p>
							</div>
						</div>
					{/each}
				</div>
			{:else}
				<div class="text-center text-gray-400 py-6">
					{$t('today.noCompletedTasks')}
				</div>
			{/if}
		{/if}
	</section>
</div>
