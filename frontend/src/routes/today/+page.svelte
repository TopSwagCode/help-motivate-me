<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { milestoneStore } from '$lib/stores/milestones';
	import { t, locale } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import { getTodayView } from '$lib/api/today';
	import { completeStackItem, completeAllStackItems } from '$lib/api/habitStacks';
	import { completeTask, postponeTask, updateTask, completeMultipleTasks } from '$lib/api/tasks';
	import { getIdentities } from '$lib/api/identities';
	import { getIdentityProofs, deleteIdentityProof } from '$lib/api/identityProofs';
	import { completeDailyCommitment, dismissDailyCommitment } from '$lib/api/dailyCommitment';
	import WelcomePopup from '$lib/components/onboarding/WelcomePopup.svelte';
	import InfoOverlay from '$lib/components/common/InfoOverlay.svelte';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';
	import { tour } from '$lib/stores/tour';
	import { dailyDigestStore } from '$lib/stores/dailyDigest';
	import TodayViewContent from '$lib/components/today/TodayViewContent.svelte';
	import DailyCommitmentCard from '$lib/components/today/DailyCommitmentCard.svelte';
	import CommitmentFlowModal from '$lib/components/today/CommitmentFlowModal.svelte';
	import IdentityProofModal from '$lib/components/today/IdentityProofModal.svelte';
	import DailyDigestOverlay from '$lib/components/today/DailyDigestOverlay.svelte';
	import type { TodayView, TodayTask, Identity, IdentityProof } from '$lib/types';
	import { getLocalDateString } from '$lib/utils/date';

	let todayData = $state<TodayView | null>(null);
	let identities = $state<Identity[]>([]);
	let wins = $state<IdentityProof[]>([]);
	let loading = $state(true);
	let error = $state('');
	let showWelcomePopup = $state(false);

	// Current date being viewed (use local date, not UTC)
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

	// Daily commitment modal state
	let showCommitmentModal = $state(false);

	// Identity proof modal state
	let showProofModal = $state(false);

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		// Check if user needs onboarding
		if (!$auth.user.hasCompletedOnboarding) {
			goto('/onboarding');
			return;
		}

		// Check if we should show welcome popup (from onboarding completion)
		if ($page.url.searchParams.get('welcome') === 'true') {
			showWelcomePopup = true;
			// Clear the URL param without reloading
			window.history.replaceState({}, '', '/today');
		}

		await loadToday();

		// Show daily digest overlay on first visit of the day
		if (isToday() && dailyDigestStore.shouldAutoShow()) {
			dailyDigestStore.showDigest();
		}
	});

	function closeWelcomePopup() {
		showWelcomePopup = false;
	}

	function startGuidedTour() {
		showWelcomePopup = false;
		tour.startTour();
	}

	async function loadToday() {
		loading = true;
		error = '';
		try {
			const [todayResult, identitiesResult, winsResult] = await Promise.all([
				getTodayView(currentDate),
				identities.length === 0 ? getIdentities() : Promise.resolve(identities),
				getIdentityProofs(currentDate, currentDate)
			]);
			todayData = todayResult;
			identities = identitiesResult;
			wins = winsResult;
		} catch (e) {
			error = e instanceof Error ? e.message : get(t)('errors.generic');
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

			// Check for milestones that may have been unlocked
			milestoneStore.checkForNew();
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

			// Check for milestones that may have been unlocked
			milestoneStore.checkForNew();
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

			// Check for milestones that may have been unlocked
			milestoneStore.checkForNew();
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
				// Check for milestones that may have been unlocked
				milestoneStore.checkForNew();
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
				description: editDescription.trim() || null,
				dueDate: editDueDate || null,
				identityId: editIdentityId || null
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

	function isToday(): boolean {
		return currentDate === getLocalDateString();
	}

	// Daily commitment handlers
	function handleStartCommitment() {
		showCommitmentModal = true;
	}

	function handleCloseCommitmentModal() {
		showCommitmentModal = false;
	}

	async function handleCommitmentCreated() {
		// Reload today data to get the new commitment
		await loadToday();
	}

	async function handleCompleteCommitment(commitmentId: string) {
		if (!todayData) return;
		try {
			const updatedCommitment = await completeDailyCommitment(commitmentId);
			todayData = {
				...todayData,
				dailyCommitment: updatedCommitment
			};
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to complete commitment';
		}
	}

	async function handleDismissCommitment(commitmentId: string) {
		if (!todayData) return;
		try {
			const updatedCommitment = await dismissDailyCommitment(commitmentId);
			todayData = {
				...todayData,
				dailyCommitment: updatedCommitment
			};
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to dismiss commitment';
		}
	}

	// Identity proof handlers
	function handleOpenProofModal() {
		showProofModal = true;
	}

	function handleCloseProofModal() {
		showProofModal = false;
	}

	async function handleProofCreated() {
		// Reload today data to get updated scores
		await loadToday();
	}

	async function handleDeleteWin(winId: string) {
		try {
			await deleteIdentityProof(winId);
			wins = wins.filter(w => w.id !== winId);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to delete win';
		}
	}
</script>

<div class="bg-warm-cream">
	<!-- Date Navigation Sub-header -->
	<div class="bg-warm-paper border-b border-gray-100">
		<div class="max-w-3xl mx-auto px-3 sm:px-6 lg:px-8">
			<!-- Page Title with Info -->
			<div class="flex items-center justify-between pt-4 pb-2">
				<InfoOverlay
					title={$t('today.title')}
					description={$t('today.info.description')}
				/>
				{#if isToday()}
					<button
						onclick={() => dailyDigestStore.showDigest(true)}
						class="p-2 text-cocoa-400 hover:text-primary-600 hover:bg-primary-50 rounded-xl transition-colors"
						title={$t('dailyDigest.reshow')}
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
						</svg>
					</button>
				{/if}
			</div>
			
			<div class="flex items-center justify-center py-3 gap-2 sm:gap-4">
				<button
					onclick={goToPreviousDay}
					class="p-2 text-cocoa-500 hover:text-cocoa-700 hover:bg-primary-50 rounded-2xl flex-shrink-0 touch-manipulation"
					title="Previous day"
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
					</svg>
				</button>

				<button
					onclick={() => (showDatePicker = !showDatePicker)}
					class="text-base sm:text-lg font-semibold text-cocoa-800 hover:text-primary-600 px-2 sm:px-3 py-1 rounded-2xl hover:bg-primary-50 min-w-0 sm:min-w-[320px] text-center flex-1 sm:flex-initial truncate"
				>
					{formatDisplayDate(currentDate)}
				</button>

				<button
					onclick={goToNextDay}
					class="p-2 text-cocoa-500 hover:text-cocoa-700 hover:bg-primary-50 rounded-2xl flex-shrink-0 touch-manipulation"
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
						class="px-4 py-1.5 text-sm text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-2xl font-medium transition-colors touch-manipulation"
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
						class="w-full px-3 py-2 border border-primary-200 rounded-2xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
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
			<div class="card">
				<ErrorState message={error} onRetry={loadToday} size="md" />
			</div>
		{:else if todayData}
			<!-- Daily Commitment Card (only show if viewing today) -->
			{#if isToday()}
				<div class="mb-6" data-tour="daily-commitment">
					<DailyCommitmentCard
						commitment={todayData.dailyCommitment}
						yesterdayCommitment={todayData.yesterdayCommitment}
						onStartCommitment={handleStartCommitment}
						onComplete={handleCompleteCommitment}
						onDismiss={handleDismissCommitment}
					/>
				</div>
			{/if}

			<TodayViewContent
				{todayData}
				{wins}
				readonly={false}
				onToggleHabitItem={toggleHabitItem}
				onCompleteAllHabits={completeAllHabits}
				onToggleTask={toggleTask}
				onCompleteAllTasks={completeAllTasks}
				onSnoozeTask={handleSnooze}
				onPostponeTask={openPostponePopup}
				onEditTask={openEditPopup}
				onLogIdentityProof={handleOpenProofModal}
				onDeleteWin={handleDeleteWin}
				{transitioningTaskIds}
				{newlyArrivedTaskIds}
				{snoozingTaskIds}
				{removingAfterSnoozeIds}
			/>
		{/if}
	</main>

	<!-- Postpone Popup -->
	{#if showPostponePopup && postponingTask}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-warm-paper rounded-xl shadow-xl max-w-sm w-full p-6">
				<h3 class="text-lg font-semibold text-cocoa-800 mb-4">{$t('today.postponeTask')}</h3>
				<p class="text-cocoa-600 mb-4 truncate">{postponingTask.title}</p>

				<div class="mb-6">
					<label for="newDueDate" class="block text-sm font-medium text-cocoa-700 mb-2">{$t('today.newDueDate')}</label>
					<input
						type="date"
						id="newDueDate"
						bind:value={newDueDate}
						min={getLocalDateString()}
						class="w-full px-3 py-2 border border-primary-200 rounded-2xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
					/>
				</div>

				<div class="flex justify-end gap-3">
					<button
						onclick={closePostponePopup}
						class="px-4 py-2 text-cocoa-600 hover:text-gray-800"
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
			<div class="bg-warm-paper rounded-xl shadow-xl max-w-md w-full p-6">
				<h3 class="text-lg font-semibold text-cocoa-800 mb-4">{$t('today.editTask')}</h3>

				<div class="space-y-4">
					<div>
						<label for="editTitle" class="block text-sm font-medium text-cocoa-700 mb-1">{$t('today.taskTitle')}</label>
						<input
							type="text"
							id="editTitle"
							bind:value={editTitle}
							class="input"
							placeholder={$t('today.taskTitlePlaceholder')}
						/>
					</div>

					<div>
						<label for="editDescription" class="block text-sm font-medium text-cocoa-700 mb-1">
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
						<label for="editDueDate" class="block text-sm font-medium text-cocoa-700 mb-1">
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
							<label for="editIdentity" class="block text-sm font-medium text-cocoa-700 mb-1">
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
						class="px-4 py-2 text-cocoa-600 hover:text-gray-800"
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
		<WelcomePopup onclose={closeWelcomePopup} ontour={startGuidedTour} />
	{/if}

	<!-- Daily Commitment Flow Modal -->
	<CommitmentFlowModal
		isOpen={showCommitmentModal}
		onClose={handleCloseCommitmentModal}
		onCommitmentCreated={handleCommitmentCreated}
	/>

	<!-- Identity Proof Modal (for TodayViewContent header button) -->
	<IdentityProofModal
		isOpen={showProofModal}
		onClose={handleCloseProofModal}
		onProofCreated={handleProofCreated}
	/>

	<!-- Daily Digest Overlay -->
	<DailyDigestOverlay />
</div>
