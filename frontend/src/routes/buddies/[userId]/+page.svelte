<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { getBuddyTodayView } from '$lib/api/buddies';
	import type { BuddyTodayViewResponse } from '$lib/types';

	let todayData = $state<BuddyTodayViewResponse | null>(null);
	let loading = $state(true);
	let error = $state('');
	let currentDate = $state(getLocalDateString());

	function getLocalDateString(): string {
		const now = new Date();
		return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
	}

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadTodayData();
	});

	async function loadTodayData() {
		loading = true;
		error = '';

		try {
			todayData = await getBuddyTodayView($page.params.userId!, currentDate);
		} catch (e) {
			if (e instanceof Error && e.message.includes('403')) {
				error = 'You do not have permission to view this user\'s progress.';
			} else {
				error = e instanceof Error ? e.message : 'Failed to load data';
			}
		} finally {
			loading = false;
		}
	}

	function goToPreviousDay() {
		const date = new Date(currentDate + 'T12:00:00');
		date.setDate(date.getDate() - 1);
		currentDate = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
		loadTodayData();
	}

	function goToNextDay() {
		const date = new Date(currentDate + 'T12:00:00');
		date.setDate(date.getDate() + 1);
		currentDate = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
		loadTodayData();
	}

	function goToToday() {
		currentDate = getLocalDateString();
		loadTodayData();
	}

	function formatDisplayDate(dateStr: string): string {
		const date = new Date(dateStr + 'T12:00:00');
		return date.toLocaleDateString('en-US', {
			weekday: 'long',
			month: 'long',
			day: 'numeric'
		});
	}

	function getStreakEmoji(streak: number): string {
		if (streak >= 30) return '🏆';
		if (streak >= 14) return '🔥';
		if (streak >= 7) return '💪';
		if (streak >= 3) return '⚡';
		return '';
	}

	function isToday(dateStr: string): boolean {
		return dateStr === getLocalDateString();
	}
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		{#if loading}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else if error}
			<div class="card p-8 text-center">
				<p class="text-red-600 mb-4">{error}</p>
				<a href="/buddies" class="btn-primary">Back to Buddies</a>
			</div>
		{:else if todayData}
			<!-- Header -->
			<div class="flex items-center justify-between mb-6">
				<div>
					<a href="/buddies" class="text-sm text-gray-500 hover:text-gray-700 mb-1 inline-block">
						&larr; Back to Buddies
					</a>
					<h1 class="text-2xl font-bold text-gray-900">{todayData.userDisplayName}'s Progress</h1>
				</div>
				<a href="/buddies/{$page.params.userId}/journal" class="btn-secondary text-sm">
					View Journal
				</a>
			</div>

			<!-- Date Navigation -->
			<div class="flex items-center justify-center gap-4 mb-6">
				<button
					onclick={goToPreviousDay}
					class="p-2 rounded-lg text-gray-600 hover:bg-gray-100"
					aria-label="Previous day"
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
					</svg>
				</button>

				<div class="text-center">
					<p class="font-semibold text-gray-900">{formatDisplayDate(currentDate)}</p>
					{#if !isToday(currentDate)}
						<button onclick={goToToday} class="text-xs text-primary-600 hover:underline">
							Jump to today
						</button>
					{/if}
				</div>

				<button
					onclick={goToNextDay}
					class="p-2 rounded-lg text-gray-600 hover:bg-gray-100"
					aria-label="Next day"
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
					</svg>
				</button>
			</div>

			<!-- Identity Feedback -->
			{#if todayData.identityFeedback.length > 0}
				<div class="card p-4 mb-6 bg-gradient-to-r from-primary-50 to-purple-50">
					<div class="flex items-center gap-2 mb-2">
						<span class="text-lg">🎯</span>
						<h2 class="font-semibold text-gray-900">Identity Votes Today</h2>
					</div>
					<div class="space-y-2">
						{#each todayData.identityFeedback as feedback (feedback.id)}
							<p class="text-sm text-gray-700">{feedback.reinforcementMessage}</p>
						{/each}
					</div>
				</div>
			{/if}

			<!-- Habit Stacks -->
			<div class="card p-5 mb-6">
				<div class="flex items-center justify-between mb-4">
					<div class="flex items-center gap-2">
						<span class="text-lg">🔗</span>
						<h2 class="font-semibold text-gray-900">Habit Stacks</h2>
					</div>
				</div>

				{#if todayData.habitStacks.length === 0}
					<p class="text-gray-500 text-sm text-center py-4">No habit stacks</p>
				{:else}
					<div class="space-y-4">
						{#each todayData.habitStacks as stack (stack.id)}
							<div class="border border-gray-200 rounded-lg p-4">
								<div class="flex items-center justify-between mb-3">
									<div>
										<h3 class="font-medium text-gray-900">{stack.name}</h3>
										{#if stack.triggerCue}
											<p class="text-xs text-gray-500">{stack.triggerCue}</p>
										{/if}
									</div>
									<div class="text-sm text-gray-600">
										{stack.completedCount}/{stack.totalCount}
									</div>
								</div>

								<!-- Progress bar -->
								<div class="h-2 bg-gray-100 rounded-full mb-3 overflow-hidden">
									<div
										class="h-full bg-primary-500 rounded-full transition-all"
										style="width: {stack.totalCount > 0 ? (stack.completedCount / stack.totalCount) * 100 : 0}%"
									></div>
								</div>

								<!-- Habit items -->
								<div class="space-y-2">
									{#each stack.items as item (item.id)}
										<div class="flex items-center gap-3 py-1">
											<div
												class="w-5 h-5 rounded-full border-2 flex items-center justify-center {item.isCompletedToday
													? 'bg-primary-500 border-primary-500'
													: 'border-gray-300'}"
											>
												{#if item.isCompletedToday}
													<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
														<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
													</svg>
												{/if}
											</div>
											<span class="text-sm {item.isCompletedToday ? 'text-gray-900' : 'text-gray-500'}">
												{item.habitDescription}
											</span>
											{#if item.currentStreak > 0}
												<span class="text-xs">{getStreakEmoji(item.currentStreak)} {item.currentStreak}</span>
											{/if}
										</div>
									{/each}
								</div>
							</div>
						{/each}
					</div>
				{/if}
			</div>

			<!-- Upcoming Tasks -->
			<div class="card p-5 mb-6">
				<div class="flex items-center gap-2 mb-4">
					<span class="text-lg">📋</span>
					<h2 class="font-semibold text-gray-900">Upcoming Tasks</h2>
				</div>

				{#if todayData.upcomingTasks.length === 0}
					<p class="text-gray-500 text-sm text-center py-4">No upcoming tasks</p>
				{:else}
					<div class="space-y-2">
						{#each todayData.upcomingTasks as task (task.id)}
							<div class="flex items-start gap-3 p-3 bg-gray-50 rounded-lg">
								<div class="w-5 h-5 rounded-full border-2 border-gray-300 mt-0.5"></div>
								<div class="flex-1 min-w-0">
									<p class="font-medium text-gray-900">{task.title}</p>
									<p class="text-xs text-gray-500">{task.goalTitle}</p>
									{#if task.dueDate}
										<p class="text-xs text-gray-400 mt-1">Due: {task.dueDate}</p>
									{/if}
								</div>
							</div>
						{/each}
					</div>
				{/if}
			</div>

			<!-- Completed Tasks -->
			{#if todayData.completedTasks.length > 0}
				<div class="card p-5">
					<div class="flex items-center gap-2 mb-4">
						<span class="text-lg">✅</span>
						<h2 class="font-semibold text-gray-900">Completed Today</h2>
					</div>

					<div class="space-y-2">
						{#each todayData.completedTasks as task (task.id)}
							<div class="flex items-start gap-3 p-3 bg-green-50 rounded-lg">
								<div class="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center mt-0.5">
									<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
									</svg>
								</div>
								<div class="flex-1 min-w-0">
									<p class="font-medium text-gray-900 line-through">{task.title}</p>
									<p class="text-xs text-gray-500">{task.goalTitle}</p>
								</div>
							</div>
						{/each}
					</div>
				</div>
			{/if}
		{/if}
	</main>
</div>
