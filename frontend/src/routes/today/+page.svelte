<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { getTodayView } from '$lib/api/today';
	import { completeStackItem, completeAllStackItems } from '$lib/api/habitStacks';
	import { completeTask, postponeTask, updateTask, completeMultipleTasks } from '$lib/api/tasks';
	import { getIdentities } from '$lib/api/identities';
	import type { TodayView, TodayTask, Identity } from '$lib/types';

	let todayData = $state<TodayView | null>(null);
	let identities = $state<Identity[]>([]);
	let loading = $state(true);
	let error = $state('');

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

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadToday();
	});

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
		const today = new Date();
		today.setHours(0, 0, 0, 0);
		const targetDate = new Date(dateStr);
		targetDate.setHours(0, 0, 0, 0);

		const diffDays = Math.round((targetDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

		if (diffDays === 0) {
			return `Today - ${date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })}`;
		} else if (diffDays === -1) {
			return `Yesterday - ${date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })}`;
		} else if (diffDays === 1) {
			return `Tomorrow - ${date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })}`;
		}
		return date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
	}

	function formatRelativeDate(dateStr: string | null): string {
		if (!dateStr) return 'No date';

		const today = new Date();
		today.setHours(0, 0, 0, 0);
		const dueDate = new Date(dateStr);
		dueDate.setHours(0, 0, 0, 0);

		const diffDays = Math.round((dueDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

		if (diffDays === 0) return 'Today';
		if (diffDays === 1) return 'Tomorrow';
		if (diffDays === -1) return 'Yesterday';
		if (diffDays < 0) return `${Math.abs(diffDays)} days ago`;
		if (diffDays < 7) {
			return dueDate.toLocaleDateString('en-US', { weekday: 'short', day: 'numeric' });
		}
		return dueDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
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
		<div class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8">
			<div class="flex items-center justify-center py-3 gap-4">
				<button
					onclick={goToPreviousDay}
					class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg"
					title="Previous day"
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
					</svg>
				</button>

				<button
					onclick={() => (showDatePicker = !showDatePicker)}
					class="text-lg font-semibold text-gray-900 hover:text-primary-600 px-3 py-1 rounded-lg hover:bg-gray-100"
				>
					{formatDisplayDate(currentDate)}
				</button>

				<button
					onclick={goToNextDay}
					class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg"
					title="Next day"
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
					</svg>
				</button>

				{#if !isToday()}
					<button
						onclick={goToToday}
						class="px-3 py-1 text-sm text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-lg"
					>
						Jump to Today
					</button>
				{/if}
			</div>

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

	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
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
			<!-- Identity Feedback -->
			{#if todayData.identityFeedback.length > 0}
				<section class="mb-8">
					<h2 class="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
						<span>🎯</span> Identity Votes
					</h2>
					<div class="grid gap-3 sm:grid-cols-2">
						{#each todayData.identityFeedback as feedback (feedback.id)}
							<div
								class="card p-4 border-l-4"
								style="border-left-color: {feedback.color || '#6366f1'}"
							>
								<p class="font-medium text-gray-900">{feedback.name}</p>
								<p class="text-sm text-gray-600 mt-1">{feedback.reinforcementMessage}</p>
							</div>
						{/each}
					</div>
				</section>
			{/if}

			<!-- Habit Stacks -->
			{#if todayData.habitStacks.length > 0}
				<section class="mb-8">
					<h2 class="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
						<span>🔗</span> Habit Stacks
					</h2>
					<div class="space-y-4">
						{#each todayData.habitStacks as stack (stack.id)}
							<div class="card p-4">
								<div class="flex items-center justify-between mb-3">
									<div>
										<h3 class="font-medium text-gray-900">{stack.name}</h3>
										{#if stack.triggerCue}
											<p class="text-sm text-gray-500 mt-0.5">{stack.triggerCue}</p>
										{/if}
									</div>
									<div class="flex items-center gap-2">
										{#if stack.identityColor}
											<span
												class="w-3 h-3 rounded-full"
												style="background-color: {stack.identityColor}"
												title={stack.identityName}
											></span>
										{/if}
										<span class="text-sm font-medium text-gray-600">
											{stack.completedCount}/{stack.totalCount}
										</span>
										{#if stack.completedCount < stack.totalCount}
											<button
												onclick={() => completeAllHabits(stack.id)}
												class="px-2 py-1 text-xs font-medium text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-md transition-colors"
												title="Complete all habits in this stack"
											>
												Complete All
											</button>
										{/if}
									</div>
								</div>

								<!-- Progress bar -->
								<div class="bg-gray-200 rounded-full h-1.5 mb-4">
									<div
										class="bg-primary-600 h-1.5 rounded-full transition-all duration-300"
										style="width: {stack.totalCount > 0 ? (stack.completedCount / stack.totalCount) * 100 : 0}%"
									></div>
								</div>

								<!-- Stack items -->
								<div class="space-y-2">
									{#each stack.items as item (item.id)}
										<button
											onclick={() => toggleHabitItem(item.id)}
											class="w-full flex items-start gap-3 p-2 rounded-lg transition-colors {item.isCompletedToday ? 'bg-green-50' : 'hover:bg-gray-50'} cursor-pointer"
										>
											<div class="flex-shrink-0 mt-0.5">
												{#if item.isCompletedToday}
													<div class="w-5 h-5 rounded-full bg-green-500 flex items-center justify-center">
														<svg class="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
															<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
														</svg>
													</div>
												{:else}
													<div class="w-5 h-5 rounded-full border-2 border-gray-300"></div>
												{/if}
											</div>
											<div class="flex-1 text-left">
												<p class="{item.isCompletedToday ? 'text-gray-500 line-through' : 'text-gray-900'}">
													{item.habitDescription}
												</p>
											</div>
											{#if item.currentStreak > 0}
												<span class="text-sm text-orange-500" title="{item.currentStreak} day streak">
													{getStreakEmoji(item.currentStreak)} {item.currentStreak}
												</span>
											{/if}
										</button>
									{/each}
								</div>
							</div>
						{/each}
					</div>
				</section>
			{/if}

			<!-- Upcoming Tasks - Always visible -->
			<section class="mb-8">
				<div class="flex items-center justify-between mb-4">
					<h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
						<span>📋</span> Tasks ({todayData.upcomingTasks.length})
					</h2>
					{#if todayData.upcomingTasks.length > 0}
						<button
							onclick={completeAllTasks}
							class="px-3 py-1.5 text-sm font-medium text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-md transition-colors"
							title="Complete all pending tasks"
						>
							Complete All
						</button>
					{/if}
				</div>
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
											📅 Moved outside 7-day view
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
							No pending tasks for this day
						</div>
					{/if}
				</div>
			</section>

			<!-- Completed Tasks - Always visible -->
			<section class="mb-8">
				<h2 class="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
					<span>✅</span> Completed ({todayData.completedTasks.length})
				</h2>
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
							No completed tasks yet
						</div>
					{/if}
				</div>
			</section>

			<!-- Empty State - Only show if no habit stacks -->
			{#if todayData.habitStacks.length === 0 && todayData.upcomingTasks.length === 0 && todayData.completedTasks.length === 0}
				<div class="card p-12 text-center">
					<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
						<span class="text-3xl">🌟</span>
					</div>
					<h3 class="text-lg font-medium text-gray-900 mb-2">All clear!</h3>
					<p class="text-gray-500 mb-6">No habit stacks or tasks for this day. Create some habit stacks to build your routine.</p>
					<a href="/habit-stacks" class="btn-primary">Create Habit Stack</a>
				</div>
			{/if}
		{/if}
	</main>

	<!-- Postpone Popup -->
	{#if showPostponePopup && postponingTask}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-sm w-full p-6">
				<h3 class="text-lg font-semibold text-gray-900 mb-4">Postpone Task</h3>
				<p class="text-gray-600 mb-4 truncate">{postponingTask.title}</p>

				<div class="mb-6">
					<label for="newDueDate" class="block text-sm font-medium text-gray-700 mb-2">New due date</label>
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
						Cancel
					</button>
					<button
						onclick={handlePostpone}
						class="btn-primary"
					>
						Save
					</button>
				</div>
			</div>
		</div>
	{/if}

	<!-- Edit Task Popup -->
	{#if showEditPopup && editingTask}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-md w-full p-6">
				<h3 class="text-lg font-semibold text-gray-900 mb-4">Edit Task</h3>

				<div class="space-y-4">
					<div>
						<label for="editTitle" class="block text-sm font-medium text-gray-700 mb-1">Title</label>
						<input
							type="text"
							id="editTitle"
							bind:value={editTitle}
							class="input"
							placeholder="Task title"
						/>
					</div>

					<div>
						<label for="editDescription" class="block text-sm font-medium text-gray-700 mb-1">
							Description <span class="text-gray-400 font-normal">(optional)</span>
						</label>
						<textarea
							id="editDescription"
							bind:value={editDescription}
							rows="3"
							class="input resize-none"
							placeholder="Add a description..."
						></textarea>
					</div>

					<div>
						<label for="editDueDate" class="block text-sm font-medium text-gray-700 mb-1">
							Due Date <span class="text-gray-400 font-normal">(optional)</span>
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
								Identity <span class="text-gray-400 font-normal">(optional)</span>
							</label>
							<select
								id="editIdentity"
								bind:value={editIdentityId}
								class="input w-full"
							>
								<option value="">No identity</option>
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
						Cancel
					</button>
					<button
						onclick={handleSaveEdit}
						class="btn-primary"
						disabled={editSaving || !editTitle.trim()}
					>
						{editSaving ? 'Saving...' : 'Save'}
					</button>
				</div>
			</div>
		</div>
	{/if}
</div>
