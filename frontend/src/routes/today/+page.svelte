<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { getTodayView } from '$lib/api/today';
	import { completeStackItem, completeAllStackItems } from '$lib/api/habitStacks';
	import { completeTask, postponeTask, updateTask, completeMultipleTasks } from '$lib/api/tasks';
	import { getIdentities } from '$lib/api/identities';
	import WelcomePopup from '$lib/components/onboarding/WelcomePopup.svelte';
	import InfoOverlay from '$lib/components/common/InfoOverlay.svelte';
	import type { TodayView, TodayTask, Identity } from '$lib/types';

	let todayData = $state<TodayView | null>(null);
	let identities = $state<Identity[]>([]);
	let loading = $state(true);
	let error = $state('');
	let showWelcomePopup = $state(false);

	// Current date being viewed (use local date, not UTC)
	function getLocalDateString(date: Date = new Date()): string {
		const year = date.getFullYear();
		const month = String(date.getMonth() + 1).padStart(2, '0');
		const day = String(date.getDate()).padStart(2, '0');
		return `${year}-${month}-${day}`;
	}

	let currentDate = $state(getLocalDateString());

	// Postpone popup state
	let showPostponePopup = $state(false);
	let postponingTask = $state<TodayTask | null>(null);
	let newDueDate = $state('');

	// Date picker popup state
	let showDatePicker = $state(false);

	// Edit task popup state
	let showEditPopup = $state(false);
	let editingTask = $state<TodayTask | null>(null);
	let editTitle = $state('');
	let editDescription = $state('');
	let editDueDate = $state('');
	let editIdentityId = $state('');
	let editSaving = $state(false);

	// Track tasks transitioning for animation (use arrays for better Svelte 5 reactivity)
	let transitioningTaskIds = $state<string[]>([]);
	// Track newly arrived tasks for entrance animation
	let newlyArrivedTaskIds = $state<string[]>([]);
	// Track tasks being snoozed for animation
	let snoozingTaskIds = $state<string[]>([]);
	// Track tasks being removed after snooze (outside 7-day window)
	let removingAfterSnoozeIds = $state<string[]>([]);

	// Collapsible section states (default expanded)
	let sectionsExpanded = $state({
		identityVotes: true,
		identityProgress: true,
		habitStacks: true,
		tasks: true,
		completedTasks: false
	});

	function toggleSection(section: keyof typeof sectionsExpanded) {
		sectionsExpanded[section] = !sectionsExpanded[section];
	}

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		// Check if we should show welcome popup (from onboarding completion)
		if ($page.url.searchParams.get('welcome') === 'true') {
			showWelcomePopup = true;
			// Clear the URL param without reloading
			window.history.replaceState({}, '', '/today');
		}

		await loadToday();
	});

	function closeWelcomePopup() {
		showWelcomePopup = false;
	}

	async function loadToday() {
		loading = true;
		try {
			[todayData, identities] = await Promise.all([
				getTodayView(currentDate),
				identities.length === 0 ? getIdentities() : Promise.resolve(identities)
			]);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load today view';
		} finally {
			loading = false;
		}
	}

	function goToPreviousDay() {
		const date = new Date(currentDate + 'T12:00:00'); // Add time to avoid timezone issues
		date.setDate(date.getDate() - 1);
		currentDate = getLocalDateString(date);
		loadToday();
	}

	function goToNextDay() {
		const date = new Date(currentDate + 'T12:00:00'); // Add time to avoid timezone issues
		date.setDate(date.getDate() + 1);
		currentDate = getLocalDateString(date);
		loadToday();
	}

	function goToToday() {
		currentDate = getLocalDateString();
		loadToday();
	}

	function handleDateChange(e: Event) {
		const input = e.target as HTMLInputElement;
		currentDate = input.value;
		showDatePicker = false;
		loadToday();
	}

	async function toggleHabitItem(itemId: string) {
		if (!todayData) return;

		try {
			await completeStackItem(itemId, currentDate);

			// Update state locally
			todayData = {
				...todayData,
				habitStacks: todayData.habitStacks.map((stack) => {
					const itemIndex = stack.items.findIndex((item) => item.id === itemId);
					if (itemIndex === -1) return stack;

					const item = stack.items[itemIndex];
					const wasCompleted = item.isCompletedToday;
					const updatedItems = stack.items.map((i) =>
						i.id === itemId
							? {
									...i,
									isCompletedToday: !wasCompleted,
									currentStreak: wasCompleted ? Math.max(0, i.currentStreak - 1) : i.currentStreak + 1
								}
							: i
					);

					return {
						...stack,
						items: updatedItems,
						completedCount: wasCompleted ? stack.completedCount - 1 : stack.completedCount + 1
					};
				})
			};
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to toggle habit';
		}
	}

	async function completeAllHabits(stackId: string) {
		if (!todayData) return;

		try {
			await completeAllStackItems(stackId, currentDate);

			// Update state locally - mark all items as completed
			todayData = {
				...todayData,
				habitStacks: todayData.habitStacks.map((stack) => {
					if (stack.id !== stackId) return stack;

					const updatedItems = stack.items.map((item) => ({
						...item,
						isCompletedToday: true,
						currentStreak: item.isCompletedToday ? item.currentStreak : item.currentStreak + 1
					}));

					return {
						...stack,
						items: updatedItems,
						completedCount: stack.totalCount
					};
				})
			};
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to complete all habits';
		}
	}

	async function completeAllTasks() {
		if (!todayData || todayData.upcomingTasks.length === 0) return;

		const taskIds = todayData.upcomingTasks.map((t) => t.id);

		try {
			// Add all tasks to transitioning for animation
			transitioningTaskIds = [...transitioningTaskIds, ...taskIds];

			await completeMultipleTasks(taskIds);

			// Wait for animation
			await new Promise((resolve) => setTimeout(resolve, 300));

			// Remove from transitioning
			transitioningTaskIds = transitioningTaskIds.filter((id) => !taskIds.includes(id));

			// Move all tasks to completed
			todayData = {
				...todayData,
				completedTasks: [...todayData.upcomingTasks, ...todayData.completedTasks],
				upcomingTasks: []
			};
		} catch (e) {
			transitioningTaskIds = transitioningTaskIds.filter((id) => !taskIds.includes(id));
			error = e instanceof Error ? e.message : 'Failed to complete all tasks';
		}
	}

	async function toggleTask(taskId: string) {
		if (!todayData) return;

		// Find the task in either list
		const isCompleted = todayData.completedTasks.some((t) => t.id === taskId);
		const task = isCompleted
			? todayData.completedTasks.find((t) => t.id === taskId)
			: todayData.upcomingTasks.find((t) => t.id === taskId);

		if (!task) return;

		try {
			// Add to transitioning array for exit animation
			transitioningTaskIds = [...transitioningTaskIds, taskId];

			await completeTask(taskId);

			// Wait for exit animation
			await new Promise((resolve) => setTimeout(resolve, 300));

			// Remove from transitioning array before moving
			transitioningTaskIds = transitioningTaskIds.filter((id) => id !== taskId);

			// Mark as newly arrived for entrance animation in destination list
			newlyArrivedTaskIds = [...newlyArrivedTaskIds, taskId];

			// Update state locally - move task between lists
			// Important: filter from BOTH lists first to avoid duplicates
			const filteredUpcoming = todayData.upcomingTasks.filter((t) => t.id !== taskId);
			const filteredCompleted = todayData.completedTasks.filter((t) => t.id !== taskId);

			if (isCompleted) {
				// Move from completed to upcoming
				todayData = {
					...todayData,
					completedTasks: filteredCompleted,
					upcomingTasks: [task, ...filteredUpcoming]
				};
			} else {
				// Move from upcoming to completed
				todayData = {
					...todayData,
					upcomingTasks: filteredUpcoming,
					completedTasks: [task, ...filteredCompleted]
				};
			}

			// Remove from newly arrived after entrance animation completes
			setTimeout(() => {
				newlyArrivedTaskIds = newlyArrivedTaskIds.filter((id) => id !== taskId);
			}, 400);
		} catch (e) {
			transitioningTaskIds = transitioningTaskIds.filter((id) => id !== taskId);
			error = e instanceof Error ? e.message : 'Failed to toggle task';
		}
	}

	function openPostponePopup(task: TodayTask) {
		postponingTask = task;
		// Set default date to tomorrow
		const tomorrow = new Date();
		tomorrow.setDate(tomorrow.getDate() + 1);
		newDueDate = getLocalDateString(tomorrow);
		showPostponePopup = true;
	}

	function closePostponePopup() {
		showPostponePopup = false;
		postponingTask = null;
		newDueDate = '';
	}

	async function handleSnooze(task: TodayTask) {
		if (!todayData) return;

		const taskId = task.id;

		// Start snooze animation
		snoozingTaskIds = [...snoozingTaskIds, taskId];

		try {
			// Calculate new date: 1 week from current due date, or 1 week from now if no due date
			const baseDate = task.dueDate ? new Date(task.dueDate + 'T12:00:00') : new Date();
			baseDate.setDate(baseDate.getDate() + 7);
			const newDate = getLocalDateString(baseDate);

			await postponeTask(taskId, newDate);

			// Calculate if task should be removed (new due date > 7 days from today)
			const today = new Date();
			today.setHours(0, 0, 0, 0);
			const newDateObj = new Date(newDate + 'T12:00:00');
			newDateObj.setHours(0, 0, 0, 0);
			const diffDays = Math.round((newDateObj.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

			// Wait for "+7 days" animation to show
			await new Promise((resolve) => setTimeout(resolve, 800));

			// Remove snooze animation
			snoozingTaskIds = snoozingTaskIds.filter((id) => id !== taskId);

			if (diffDays > 7) {
				// Mark as being removed (outside view)
				removingAfterSnoozeIds = [...removingAfterSnoozeIds, taskId];

				// Wait for removal animation (1500ms to allow reading the message)
				await new Promise((resolve) => setTimeout(resolve, 1500));

				// Remove from list
				todayData = {
					...todayData,
					upcomingTasks: todayData.upcomingTasks.filter((t) => t.id !== taskId),
					completedTasks: todayData.completedTasks.filter((t) => t.id !== taskId)
				};

				// Cleanup
				removingAfterSnoozeIds = removingAfterSnoozeIds.filter((id) => id !== taskId);
			} else {
				// Update the due date locally
				todayData = {
					...todayData,
					upcomingTasks: todayData.upcomingTasks.map((t) =>
						t.id === taskId ? { ...t, dueDate: newDate } : t
					),
					completedTasks: todayData.completedTasks.map((t) =>
						t.id === taskId ? { ...t, dueDate: newDate } : t
					)
				};
			}
		} catch (e) {
			snoozingTaskIds = snoozingTaskIds.filter((id) => id !== taskId);
			error = e instanceof Error ? e.message : 'Failed to snooze task';
		}
	}

	async function handlePostpone() {
		if (!postponingTask || !newDueDate || !todayData) return;

		const taskId = postponingTask.id;

		try {
			await postponeTask(taskId, newDueDate);

			// Calculate days difference to determine if task should be removed from view
			const today = new Date();
			today.setHours(0, 0, 0, 0);
			const newDate = new Date(newDueDate + 'T12:00:00');
			newDate.setHours(0, 0, 0, 0);
			const diffDays = Math.round((newDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

			// If postponed more than 7 days, remove from list entirely
			if (diffDays > 7) {
				todayData = {
					...todayData,
					upcomingTasks: todayData.upcomingTasks.filter((t) => t.id !== taskId),
					completedTasks: todayData.completedTasks.filter((t) => t.id !== taskId)
				};
			} else {
				// Update the due date locally
				todayData = {
					...todayData,
					upcomingTasks: todayData.upcomingTasks.map((t) =>
						t.id === taskId ? { ...t, dueDate: newDueDate } : t
					),
					completedTasks: todayData.completedTasks.map((t) =>
						t.id === taskId ? { ...t, dueDate: newDueDate } : t
					)
				};
			}

			closePostponePopup();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to postpone task';
		}
	}

	function openEditPopup(task: TodayTask) {
		editingTask = task;
		editTitle = task.title;
		editDescription = task.description || '';
		editDueDate = task.dueDate || '';
		editIdentityId = task.identityId || '';
		showEditPopup = true;
	}

	function closeEditPopup() {
		showEditPopup = false;
		editingTask = null;
		editTitle = '';
		editDescription = '';
		editDueDate = '';
		editIdentityId = '';
		editSaving = false;
	}

	async function handleSaveEdit() {
		if (!editingTask || !editTitle.trim() || !todayData) return;

		editSaving = true;
		const taskId = editingTask.id;

		try {
			const updatedTaskResponse = await updateTask(taskId, {
				title: editTitle.trim(),
				description: editDescription.trim() || undefined,
				dueDate: editDueDate || undefined,
				identityId: editIdentityId || undefined
			});

			// Calculate if task should be removed (due date > 7 days out)
			let shouldRemove = false;
			if (editDueDate) {
				const today = new Date();
				today.setHours(0, 0, 0, 0);
				const newDate = new Date(editDueDate + 'T12:00:00');
				newDate.setHours(0, 0, 0, 0);
				const diffDays = Math.round((newDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
				shouldRemove = diffDays > 7;
			}

			if (shouldRemove) {
				todayData = {
					...todayData,
					upcomingTasks: todayData.upcomingTasks.filter((t) => t.id !== taskId),
					completedTasks: todayData.completedTasks.filter((t) => t.id !== taskId)
				};
			} else {
				// Update task locally with new identity info from response
				const updateTaskData = (t: TodayTask) =>
					t.id === taskId
						? {
								...t,
								title: editTitle.trim(),
								description: editDescription.trim() || null,
								dueDate: editDueDate || null,
								identityId: updatedTaskResponse.identityId,
								identityName: updatedTaskResponse.identityName,
								identityIcon: updatedTaskResponse.identityIcon
							}
						: t;

				todayData = {
					...todayData,
					upcomingTasks: todayData.upcomingTasks.map(updateTaskData),
					completedTasks: todayData.completedTasks.map(updateTaskData)
				};
			}

			closeEditPopup();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update task';
			editSaving = false;
		}
	}

	function formatDisplayDate(dateStr: string): string {
		const date = new Date(dateStr);
		const currentLocale = get(locale) === 'da' ? 'da-DK' : 'en-US';
		return date.toLocaleDateString(currentLocale, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
	}

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

	function isToday(): boolean {
		return currentDate === getLocalDateString();
	}

	// Sort by due date: tasks with due dates come first (nearest first), then tasks without due dates
	function sortByDueDate(a: TodayTask, b: TodayTask): number {
		if (!a.dueDate && !b.dueDate) return 0;
		if (!a.dueDate) return 1; // Tasks without due date go last
		if (!b.dueDate) return -1;
		return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
	}

	// Sorted tasks
	const sortedUpcomingTasks = $derived(todayData?.upcomingTasks.slice().sort(sortByDueDate) ?? []);
</script>

<div class="min-h-screen bg-gray-50">
	<!-- Date Navigation Sub-header -->
	<div class="bg-white border-b border-gray-100">
		<div class="max-w-3xl mx-auto px-3 sm:px-6 lg:px-8">
			<!-- Page Title with Info -->
			<div class="flex items-center justify-between pt-4 pb-2">
				<InfoOverlay 
					title={$t('today.title')} 
					description={$t('today.info.description')} 
				/>
			</div>
			
			<div class="flex items-center justify-center py-3 gap-2 sm:gap-4">
				<button
					onclick={goToPreviousDay}
					class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg flex-shrink-0 touch-manipulation"
					title="Previous day"
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
					</svg>
				</button>

				<button
					onclick={() => (showDatePicker = !showDatePicker)}
					class="text-base sm:text-lg font-semibold text-gray-900 hover:text-primary-600 px-2 sm:px-3 py-1 rounded-lg hover:bg-gray-100 min-w-0 sm:min-w-[320px] text-center flex-1 sm:flex-initial truncate"
				>
					{formatDisplayDate(currentDate)}
				</button>

				<button
					onclick={goToNextDay}
					class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg flex-shrink-0 touch-manipulation"
					title="Next day"
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
					</svg>
				</button>
			</div>

			<!-- Jump to Today Button -->
			{#if !isToday()}
				<div class="flex justify-center pb-3">
					<button
						onclick={goToToday}
						class="px-4 py-1.5 text-sm text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-lg font-medium transition-colors touch-manipulation"
					>
						{$t('today.jumpToToday')}
					</button>
				</div>
			{/if}

			<!-- Date Picker Dropdown -->
			{#if showDatePicker}
				<div class="pb-4">
					<input
						type="date"
						value={currentDate}
						onchange={handleDateChange}
						class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
					/>
				</div>
			{/if}
		</div>
	</div>

	<main class="max-w-3xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
		{#if loading}
			<div class="flex justify-center py-12">
				<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
			</div>
		{:else if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
				{error}
				<button onclick={() => (error = '')} class="float-right text-red-500 hover:text-red-700">&times;</button>
			</div>
		{:else if todayData}
			<!-- Identity Feedback (Votes) -->
			{#if todayData.identityFeedback.length > 0}
				<section class="mb-6">
					<button 
						onclick={() => toggleSection('identityVotes')}
						class="w-full flex items-center justify-between text-left mb-2 group"
					>
						<h2 class="text-base font-semibold text-gray-900 flex items-center gap-2">
							<span>🎯</span> {$t('today.identityVotes')}
							<span class="text-xs font-normal text-gray-500 bg-gray-100 px-1.5 py-0.5 rounded-full">{todayData.identityFeedback.length}</span>
						</h2>
						<svg 
							class="w-4 h-4 text-gray-400 transition-transform {sectionsExpanded.identityVotes ? 'rotate-180' : ''}"
							fill="none" stroke="currentColor" viewBox="0 0 24 24"
						>
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
						</svg>
					</button>
					{#if sectionsExpanded.identityVotes}
						<div class="flex flex-wrap gap-1.5">
							{#each todayData.identityFeedback as feedback (feedback.id)}
								<div
									class="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-full text-sm transition-all hover:scale-105 cursor-default"
									style="background-color: {feedback.color || '#6366f1'}15; border: 1px solid {feedback.color || '#6366f1'}30"
									title={feedback.reinforcementMessage}
								>
									{#if feedback.icon}
										<span class="text-sm">{feedback.icon}</span>
									{/if}
									<span class="font-medium text-gray-700">{feedback.name}</span>
									<span class="text-green-500 text-xs">✓</span>
								</div>
							{/each}
						</div>
					{/if}
				</section>
			{/if}

			<!-- Identity Progress -->
			{#if todayData.identityProgress && todayData.identityProgress.length > 0}
				<section class="mb-6">
					<button 
						onclick={() => toggleSection('identityProgress')}
						class="w-full flex items-center justify-between text-left mb-2 group"
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
					{#if sectionsExpanded.identityProgress}
						<div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-2">
							{#each todayData.identityProgress as progress (progress.id)}
								<div 
									class="rounded-lg p-3 transition-all hover:scale-[1.02] cursor-default"
									style="background-color: {progress.color}15; border: 1px solid {progress.color}30"
								>
									<!-- Header: Icon + Score + Trend -->
									<div class="flex items-center justify-between mb-1.5">
										<span class="text-xl">{progress.icon || '🎯'}</span>
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
									<!-- Name (truncated) -->
									<p class="text-xs font-medium text-gray-700 truncate mb-1.5" title={progress.name}>{progress.name}</p>
									<!-- Mini progress bar -->
									<div class="bg-white/50 rounded-full h-1.5">
										<div
											class="h-1.5 rounded-full transition-all duration-500"
											style="width: {progress.score}%; background-color: {progress.color || '#9CA3AF'}"
										></div>
									</div>
								</div>
							{/each}
						</div>
					{/if}
				</section>
			{/if}

			<!-- Habit Stacks -->
			{#if todayData.habitStacks.length > 0}
				<section class="mb-6">
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
						<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
						{#each todayData.habitStacks as stack (stack.id)}
							<div 
								class="rounded-lg overflow-hidden transition-all"
								style="background-color: {stack.identityColor || '#6366f1'}08; border: 1px solid {stack.identityColor || '#6366f1'}20"
							>
								<!-- Stack Header -->
								<div 
									class="flex items-center gap-2 px-3 py-2"
									style="background-color: {stack.identityColor || '#6366f1'}15"
								>
									<span class="font-medium text-gray-800 text-sm truncate flex-1">{stack.name}</span>
									<span class="text-xs text-gray-500">{stack.completedCount}/{stack.totalCount}</span>
									{#if stack.completedCount < stack.totalCount}
										<button
											onclick={() => completeAllHabits(stack.id)}
											class="p-0.5 text-gray-400 hover:text-primary-600 rounded transition-colors"
											title={$t('today.completeAll')}
										>
											<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
											</svg>
										</button>
									{:else}
										<span class="text-green-500 text-sm">✓</span>
									{/if}
								</div>

								<!-- Vertical Habits List -->
								<div class="p-1">
									{#each stack.items as item, index (item.id)}
										<button
											onclick={() => toggleHabitItem(item.id)}
											class="w-full flex items-center gap-2 px-2 py-1.5 rounded transition-all text-left
												{item.isCompletedToday 
													? 'bg-green-500/10' 
													: 'hover:bg-white/50'}"
										>
											<!-- Connector line + checkbox -->
											<div class="flex flex-col items-center w-4 flex-shrink-0">
												{#if index > 0}
													<div 
														class="w-0.5 h-1 -mt-1 mb-0.5 rounded-full"
														style="background-color: {stack.items[index - 1]?.isCompletedToday ? '#22c55e' : (stack.identityColor || '#d1d5db')}40"
													></div>
												{/if}
												{#if item.isCompletedToday}
													<div class="w-4 h-4 rounded-full bg-green-500 flex items-center justify-center">
														<svg class="w-2.5 h-2.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
															<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
														</svg>
													</div>
												{:else}
													<div 
														class="w-4 h-4 rounded-full border-2 flex items-center justify-center text-[8px] font-bold"
														style="border-color: {stack.identityColor || '#d1d5db'}60; color: {stack.identityColor || '#9ca3af'}"
													>
														{index + 1}
													</div>
												{/if}
												{#if index < stack.items.length - 1}
													<div 
														class="w-0.5 h-1 mt-0.5 -mb-1 rounded-full"
														style="background-color: {item.isCompletedToday ? '#22c55e' : (stack.identityColor || '#d1d5db')}40"
													></div>
												{/if}
											</div>
											
											<!-- Habit text -->
											<span class="flex-1 text-xs truncate {item.isCompletedToday ? 'text-green-700 line-through' : 'text-gray-700'}">
												{item.habitDescription}
											</span>
											
											<!-- Streak -->
											{#if item.currentStreak > 0}
												<span class="text-[10px] text-orange-500 flex-shrink-0">🔥{item.currentStreak}</span>
											{/if}
										</button>
									{/each}
								</div>
							</div>
						{/each}
					</div>
					{/if}
				</section>
			{/if}

			<!-- Upcoming Tasks -->
			<section class="mb-6">
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
					{#if todayData.upcomingTasks.length > 0 && sectionsExpanded.tasks}
						<button
							onclick={completeAllTasks}
							class="px-2 py-1 text-xs font-medium text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-md transition-colors"
						>
							{$t('today.completeAll')}
						</button>
					{/if}
				</div>
				{#if sectionsExpanded.tasks}
				<div class="card divide-y divide-gray-100">
					{#if sortedUpcomingTasks.length > 0}
						{#each sortedUpcomingTasks as task (task.id)}
							<div
								class="relative flex items-start gap-4 p-4 transition-all duration-300
									{transitioningTaskIds.includes(task.id) ? 'opacity-50 scale-95 bg-green-50' : ''}
									{newlyArrivedTaskIds.includes(task.id) ? 'animate-slide-in-highlight' : ''}
									{snoozingTaskIds.includes(task.id) ? 'bg-amber-50' : ''}
									{removingAfterSnoozeIds.includes(task.id) ? 'animate-snooze-remove' : ''}"
							>
								<!-- Snooze animation overlay -->
								{#if snoozingTaskIds.includes(task.id)}
									<div class="absolute inset-0 flex items-center justify-center bg-amber-100/80 rounded-lg animate-snooze-pulse z-10">
										<span class="text-amber-700 font-bold text-lg flex items-center gap-2">
											💤 +7 days
										</span>
									</div>
								{/if}
								<!-- Removal message overlay -->
								{#if removingAfterSnoozeIds.includes(task.id)}
									<div class="absolute inset-0 flex items-center justify-center bg-gray-100/90 rounded-lg z-10">
										<span class="text-gray-600 font-medium text-sm flex items-center gap-2">
											📅 {$t('today.movedOutsideView')}
										</span>
									</div>
								{/if}
								<button
									onclick={() => toggleTask(task.id)}
									class="flex-shrink-0 mt-1"
									title="Mark as completed"
								>
									<div
										class="w-6 h-6 rounded-full border-2 border-gray-300 hover:border-primary-500 transition-all duration-200 flex items-center justify-center
											{transitioningTaskIds.includes(task.id) ? 'bg-green-500 border-green-500' : ''}"
									>
										{#if transitioningTaskIds.includes(task.id)}
											<svg class="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 24 24">
												<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
											</svg>
										{/if}
									</div>
								</button>
								{#if task.identityIcon}
									<span class="flex-shrink-0 text-lg mt-1" title={task.identityName || 'Identity'}>{task.identityIcon}</span>
								{/if}
								<div class="flex-1 min-w-0">
									<p class="text-gray-900 font-medium {transitioningTaskIds.includes(task.id) ? 'line-through text-gray-500' : ''}">
										{task.title}
									</p>
									{#if task.description}
										<p class="text-sm text-gray-500 mt-1 line-clamp-2">{task.description}</p>
									{/if}
									<p class="text-xs text-gray-400 mt-1">{task.goalTitle}</p>
								</div>
								<div class="flex items-center gap-2 flex-shrink-0">
									<span class="{getDueDateColor(task.dueDate)} text-sm">
										{formatRelativeDate(task.dueDate)}
									</span>
									<button
										onclick={() => handleSnooze(task)}
										class="text-gray-400 hover:text-amber-500 p-1 relative"
										title="Snooze 1 week"
										disabled={snoozingTaskIds.includes(task.id)}
									>
										<span class="text-lg">💤</span>
									</button>
									<button
										onclick={() => openEditPopup(task)}
										class="text-gray-400 hover:text-primary-600 p-1"
										title="Edit task"
									>
										<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
										</svg>
									</button>
									<button
										onclick={() => openPostponePopup(task)}
										class="text-gray-400 hover:text-primary-600 p-1"
										title="Postpone task"
									>
										<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
										</svg>
									</button>
								</div>
							</div>
						{/each}
					{:else}
						<div class="p-6 text-center text-gray-400">
							{$t('today.noPendingTasks')}
						</div>
					{/if}
				</div>
				{/if}
			</section>

			<!-- Completed Tasks -->
			<section class="mb-6">
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
				<div class="card divide-y divide-gray-100">
					{#if todayData.completedTasks.length > 0}
						{#each todayData.completedTasks as task (task.id)}
							<div
								class="flex items-start gap-4 p-4 bg-green-50 transition-all duration-300
									{transitioningTaskIds.includes(task.id) ? 'opacity-50 scale-95 bg-gray-50' : ''}
									{newlyArrivedTaskIds.includes(task.id) ? 'animate-slide-in-highlight-green' : ''}"
							>
								<button
									onclick={() => toggleTask(task.id)}
									class="flex-shrink-0 mt-1"
									title="Mark as incomplete"
								>
									<div class="w-6 h-6 rounded-full bg-green-500 flex items-center justify-center hover:bg-green-600 transition-colors">
										<svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
										</svg>
									</div>
								</button>
								{#if task.identityIcon}
									<span class="flex-shrink-0 text-lg mt-1 opacity-50" title={task.identityName || 'Identity'}>{task.identityIcon}</span>
								{/if}
								<div class="flex-1 min-w-0">
									<p class="text-gray-500 line-through font-medium">
										{task.title}
									</p>
									{#if task.description}
										<p class="text-sm text-gray-400 mt-1 line-clamp-2 line-through">{task.description}</p>
									{/if}
									<p class="text-xs text-gray-400 mt-1">{task.goalTitle}</p>
								</div>
								<button
									onclick={() => openEditPopup(task)}
									class="text-gray-400 hover:text-primary-600 p-1 flex-shrink-0"
									title="Edit task"
								>
									<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
									</svg>
								</button>
							</div>
						{/each}
					{:else}
						<div class="p-6 text-center text-gray-400">
							{$t('today.noCompletedTasks')}
						</div>
					{/if}
				</div>
				{/if}
			</section>

			<!-- Empty State - Only show if no habit stacks -->
			{#if todayData.habitStacks.length === 0 && todayData.upcomingTasks.length === 0 && todayData.completedTasks.length === 0}
				<div class="card p-12 text-center">
					<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
						<span class="text-3xl">🌟</span>
					</div>
					<h3 class="text-lg font-medium text-gray-900 mb-2">{$t('today.allClear')}</h3>
					<p class="text-gray-500 mb-6">{$t('today.allClearDescription')}</p>
					<a href="/habit-stacks" class="btn-primary">{$t('today.createHabitStack')}</a>
				</div>
			{/if}
		{/if}
	</main>

	<!-- Postpone Popup -->
	{#if showPostponePopup && postponingTask}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-sm w-full p-6">
				<h3 class="text-lg font-semibold text-gray-900 mb-4">{$t('today.postponeTask')}</h3>
				<p class="text-gray-600 mb-4 truncate">{postponingTask.title}</p>

				<div class="mb-6">
					<label for="newDueDate" class="block text-sm font-medium text-gray-700 mb-2">{$t('today.newDueDate')}</label>
					<input
						type="date"
						id="newDueDate"
						bind:value={newDueDate}
						min={getLocalDateString()}
						class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
					/>
				</div>

				<div class="flex justify-end gap-3">
					<button
						onclick={closePostponePopup}
						class="px-4 py-2 text-gray-600 hover:text-gray-800"
					>
						{$t('common.cancel')}
					</button>
					<button
						onclick={handlePostpone}
						class="btn-primary"
					>
						{$t('common.save')}
					</button>
				</div>
			</div>
		</div>
	{/if}

	<!-- Edit Task Popup -->
	{#if showEditPopup && editingTask}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-md w-full p-6">
				<h3 class="text-lg font-semibold text-gray-900 mb-4">{$t('today.editTask')}</h3>

				<div class="space-y-4">
					<div>
						<label for="editTitle" class="block text-sm font-medium text-gray-700 mb-1">{$t('today.taskTitle')}</label>
						<input
							type="text"
							id="editTitle"
							bind:value={editTitle}
							class="input"
							placeholder={$t('today.taskTitlePlaceholder')}
						/>
					</div>

					<div>
						<label for="editDescription" class="block text-sm font-medium text-gray-700 mb-1">
							{$t('today.taskDescription')} <span class="text-gray-400 font-normal">({$t('common.optional')})</span>
						</label>
						<textarea
							id="editDescription"
							bind:value={editDescription}
							rows="3"
							class="input resize-none"
							placeholder={$t('today.taskDescriptionPlaceholder')}
						></textarea>
					</div>

					<div>
						<label for="editDueDate" class="block text-sm font-medium text-gray-700 mb-1">
							{$t('today.dueDate')} <span class="text-gray-400 font-normal">({$t('common.optional')})</span>
						</label>
						<input
							type="date"
							id="editDueDate"
							bind:value={editDueDate}
							class="input"
						/>
					</div>

					{#if identities.length > 0}
						<div>
							<label for="editIdentity" class="block text-sm font-medium text-gray-700 mb-1">
								{$t('today.identity')} <span class="text-gray-400 font-normal">({$t('common.optional')})</span>
							</label>
							<select
								id="editIdentity"
								bind:value={editIdentityId}
								class="input w-full"
							>
								<option value="">{$t('today.noIdentity')}</option>
								{#each identities as identity (identity.id)}
									<option value={identity.id}>{identity.icon ? `${identity.icon} ` : ''}{identity.name}</option>
								{/each}
							</select>
						</div>
					{/if}
				</div>

				<div class="flex justify-end gap-3 mt-6">
					<button
						onclick={closeEditPopup}
						class="px-4 py-2 text-gray-600 hover:text-gray-800"
						disabled={editSaving}
					>
						{$t('common.cancel')}
					</button>
					<button
						onclick={handleSaveEdit}
						class="btn-primary"
						disabled={editSaving || !editTitle.trim()}
					>
						{editSaving ? $t('common.saving') : $t('common.save')}
					</button>
				</div>
			</div>
		</div>
	{/if}

	<!-- Welcome Popup (shown after onboarding completion) -->
	{#if showWelcomePopup}
		<WelcomePopup onclose={closeWelcomePopup} />
	{/if}
</div>
