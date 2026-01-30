<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t } from 'svelte-i18n';
	import { auth } from '$lib/stores/auth';
	import { getGoal, completeGoal, deleteGoal, updateGoal } from '$lib/api/goals';
	import { getTasks, createTask, completeTask, deleteTask, postponeTask, updateTask } from '$lib/api/tasks';
	import { getIdentities } from '$lib/api/identities';
	import type { Goal, Task, Identity } from '$lib/types';

	const goalId = $derived($page.params.id!);

	let goal = $state<Goal | null>(null);
	let tasks = $state<Task[]>([]);
	let identities = $state<Identity[]>([]);
	let loading = $state(true);
	let error = $state('');

	// New task form
	let newTaskTitle = $state('');
	let newTaskDescription = $state('');
	let newTaskDueDate = $state('');
	let newTaskIdentityId = $state('');
	let addingTask = $state(false);

	// Edit target date
	let editingTargetDate = $state(false);
	let newTargetDate = $state('');

	// Edit goal identity
	let editingGoalIdentity = $state(false);
	let newGoalIdentityId = $state('');

	// Edit task popup
	let showEditPopup = $state(false);
	let editingTask = $state<Task | null>(null);
	let editTitle = $state('');
	let editDescription = $state('');
	let editDueDate = $state('');
	let editIdentityId = $state('');

	// Postpone task (legacy - keeping for backward compat)
	let showPostponePopup = $state(false);
	let postponingTask = $state<Task | null>(null);
	let newDueDate = $state('');

	// Track tasks transitioning for animation (use arrays for better Svelte 5 reactivity)
	let transitioningTaskIds = $state<string[]>([]);
	// Track newly arrived tasks for entrance animation
	let newlyArrivedTaskIds = $state<string[]>([]);
	// Track tasks being snoozed for animation
	let snoozingTaskIds = $state<string[]>([]);

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadData();
	});

	async function loadData() {
		const id = goalId;
		try {
			[goal, tasks, identities] = await Promise.all([getGoal(id), getTasks(id), getIdentities()]);
			// Set default identity for new tasks to the goal's identity
			if (goal?.identityId) {
				newTaskIdentityId = goal.identityId;
			}
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load goal';
		} finally {
			loading = false;
		}
	}

	async function handleAddTask(e: Event) {
		e.preventDefault();
		if (!newTaskTitle.trim()) return;

		addingTask = true;
		const id = goalId;
		try {
			const task = await createTask(id, {
				title: newTaskTitle,
				description: newTaskDescription.trim() || undefined,
				dueDate: newTaskDueDate || undefined,
				identityId: newTaskIdentityId || undefined
			});
			// Mark as newly arrived for entrance animation
			newlyArrivedTaskIds = [...newlyArrivedTaskIds, task.id];
			tasks = [task, ...tasks];
			newTaskTitle = '';
			newTaskDescription = '';
			newTaskDueDate = '';
			// Reset to goal's identity (not empty)
			newTaskIdentityId = goal?.identityId ?? '';
			// Remove from newly arrived after animation completes
			setTimeout(() => {
				newlyArrivedTaskIds = newlyArrivedTaskIds.filter((id) => id !== task.id);
			}, 400);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to add task';
		} finally {
			addingTask = false;
		}
	}

	async function handleCompleteTask(taskId: string) {
		const id = goalId;
		try {
			// Add to transitioning array for animation
			transitioningTaskIds = [...transitioningTaskIds, taskId];

			const updatedTask = await completeTask(taskId);

			// Wait for exit animation
			await new Promise((resolve) => setTimeout(resolve, 300));

			// Mark as newly arrived for entrance animation in destination list
			newlyArrivedTaskIds = [...newlyArrivedTaskIds, taskId];

			tasks = tasks.map((t) => (t.id === taskId ? updatedTask : t));

			// Remove from transitioning array
			transitioningTaskIds = transitioningTaskIds.filter((id) => id !== taskId);

			// Remove from newly arrived after entrance animation completes
			setTimeout(() => {
				newlyArrivedTaskIds = newlyArrivedTaskIds.filter((id) => id !== taskId);
			}, 400);

			// Reload goal to update counts
			goal = await getGoal(id);
		} catch (e) {
			transitioningTaskIds = transitioningTaskIds.filter((id) => id !== taskId);
			error = e instanceof Error ? e.message : 'Failed to update task';
		}
	}

	async function handleDeleteTask(taskId: string) {
		if (!confirm($t('goals.deleteTaskConfirm'))) return;

		const id = goalId;
		try {
			await deleteTask(taskId);
			tasks = tasks.filter((t) => t.id !== taskId);
			goal = await getGoal(id);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to delete task';
		}
	}

	async function handleCompleteGoal() {
		if (!goal) return;

		try {
			goal = await completeGoal(goal.id);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update goal';
		}
	}

	async function handleDeleteGoal() {
		if (!confirm($t('goals.deleteGoalConfirm'))) return;

		const id = goalId;
		try {
			await deleteGoal(id);
			goto('/dashboard');
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to delete goal';
		}
	}

	function startEditTargetDate() {
		if (!goal) return;
		newTargetDate = goal.targetDate || '';
		editingTargetDate = true;
	}

	async function saveTargetDate() {
		if (!goal) return;

		try {
			goal = await updateGoal(goal.id, {
				title: goal.title,
				description: goal.description || undefined,
				targetDate: newTargetDate || undefined
			});
			editingTargetDate = false;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update target date';
		}
	}

	function cancelEditTargetDate() {
		editingTargetDate = false;
		newTargetDate = '';
	}

	function startEditGoalIdentity() {
		if (!goal) return;
		newGoalIdentityId = goal.identityId || '';
		editingGoalIdentity = true;
	}

	async function saveGoalIdentity() {
		if (!goal) return;

		try {
			goal = await updateGoal(goal.id, {
				title: goal.title,
				description: goal.description || undefined,
				targetDate: goal.targetDate || undefined,
				identityId: newGoalIdentityId || undefined
			});
			editingGoalIdentity = false;
			// Update new task default identity to match new goal identity
			newTaskIdentityId = goal.identityId ?? '';
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update identity';
		}
	}

	function cancelEditGoalIdentity() {
		editingGoalIdentity = false;
		newGoalIdentityId = '';
	}

	function getLocalDateString(date: Date = new Date()): string {
		const year = date.getFullYear();
		const month = String(date.getMonth() + 1).padStart(2, '0');
		const day = String(date.getDate()).padStart(2, '0');
		return `${year}-${month}-${day}`;
	}

	function openPostponePopup(task: Task) {
		postponingTask = task;
		const tomorrow = new Date();
		tomorrow.setDate(tomorrow.getDate() + 1);
		newDueDate = task.dueDate || getLocalDateString(tomorrow);
		showPostponePopup = true;
	}

	function closePostponePopup() {
		showPostponePopup = false;
		postponingTask = null;
		newDueDate = '';
	}

	async function handlePostpone() {
		if (!postponingTask || !newDueDate) return;

		try {
			const updatedTask = await postponeTask(postponingTask.id, newDueDate);
			tasks = tasks.map((t) => (t.id === postponingTask!.id ? updatedTask : t));
			closePostponePopup();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to postpone task';
		}
	}

	function openEditPopup(task: Task) {
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
	}

	async function handleSaveEdit() {
		if (!editingTask || !editTitle.trim()) return;

		try {
			const updatedTask = await updateTask(editingTask.id, {
				title: editTitle.trim(),
				description: editDescription.trim() || undefined,
				dueDate: editDueDate || undefined,
				identityId: editIdentityId || undefined
			});
			tasks = tasks.map((t) => (t.id === editingTask!.id ? updatedTask : t));
			closeEditPopup();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update task';
		}
	}

	async function handleSnooze(task: Task) {
		const taskId = task.id;

		// Start snooze animation
		snoozingTaskIds = [...snoozingTaskIds, taskId];

		try {
			// Calculate new date: 1 week from current due date, or 1 week from now if no due date
			const baseDate = task.dueDate ? new Date(task.dueDate + 'T12:00:00') : new Date();
			baseDate.setDate(baseDate.getDate() + 7);
			const newDate = getLocalDateString(baseDate);

			const updatedTask = await postponeTask(task.id, newDate);

			// Wait for animation to show
			await new Promise((resolve) => setTimeout(resolve, 800));

			// Remove snooze animation and update task
			snoozingTaskIds = snoozingTaskIds.filter((id) => id !== taskId);
			tasks = tasks.map((t) => (t.id === task.id ? updatedTask : t));
		} catch (e) {
			snoozingTaskIds = snoozingTaskIds.filter((id) => id !== taskId);
			error = e instanceof Error ? e.message : 'Failed to snooze task';
		}
	}

	function formatDueDate(dateStr: string | null): string {
		if (!dateStr) return $t('goals.noDate');
		return new Date(dateStr + 'T12:00:00').toLocaleDateString();
	}

	function getDueDateColor(dateStr: string | null): string {
		if (!dateStr) return 'text-gray-400';

		const today = new Date();
		today.setHours(0, 0, 0, 0);
		const dueDate = new Date(dateStr + 'T12:00:00');
		dueDate.setHours(0, 0, 0, 0);

		const diffDays = Math.round((dueDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

		if (diffDays < 0) return 'text-red-600 font-medium';
		if (diffDays === 0) return 'text-red-600 font-medium';
		if (diffDays === 1) return 'text-orange-600';
		return 'text-gray-500';
	}

	// Sort by due date: tasks with due dates come first (nearest first), then tasks without due dates
	function sortByDueDate(a: Task, b: Task): number {
		if (!a.dueDate && !b.dueDate) return 0;
		if (!a.dueDate) return 1; // Tasks without due date go last
		if (!b.dueDate) return -1;
		return new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
	}

	const pendingTasks = $derived(tasks.filter((t) => t.status !== 'Completed').sort(sortByDueDate));
	const completedTasks = $derived(tasks.filter((t) => t.status === 'Completed'));
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		{#if loading}
			<div class="flex justify-center py-12">
				<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
			</div>
		{:else if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
				{error}
			</div>
		{:else if goal}
			<!-- Goal Info -->
			<div class="card p-6 mb-6">
				<div class="flex items-start justify-between mb-4">
					<h1 class="text-2xl font-bold text-gray-900 {goal.isCompleted ? 'line-through opacity-60' : ''}">
						{goal.title}
					</h1>
					<div class="flex items-center gap-2">
						{#if goal.isCompleted}
							<span class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800">
								{$t('goals.completed')}
							</span>
						{/if}
						<button onclick={handleCompleteGoal} class="btn-secondary text-sm">
							{goal.isCompleted ? $t('goals.reopen') : $t('goals.complete')}
						</button>
						<button onclick={handleDeleteGoal} class="btn-danger text-sm">{$t('goals.delete')}</button>
					</div>
				</div>

				{#if goal.description}
					<p class="text-gray-600 mb-4">{goal.description}</p>
				{/if}

				<!-- Identity display/edit -->
				<div class="flex items-center gap-2 mb-4 text-sm">
					<span class="text-gray-500">{$t('goals.identity')}:</span>
					{#if editingGoalIdentity && !goal.isCompleted}
						<select
							bind:value={newGoalIdentityId}
							class="px-2 py-1 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
						>
							<option value="">{$t('goals.noIdentity')}</option>
							{#each identities as identity (identity.id)}
								<option value={identity.id}>
									{identity.icon ? `${identity.icon} ` : ''}{identity.name}
								</option>
							{/each}
						</select>
						<button
							onclick={saveGoalIdentity}
							class="text-primary-600 hover:text-primary-700 font-medium"
						>
							{$t('common.save')}
						</button>
						<button
							onclick={cancelEditGoalIdentity}
							class="text-gray-500 hover:text-gray-700"
						>
							{$t('common.cancel')}
						</button>
					{:else if !goal.isCompleted}
						<button
							onclick={startEditGoalIdentity}
							class="flex items-center gap-1 hover:text-primary-600 transition-colors"
							title="Click to edit identity"
						>
							{#if goal.identityIcon}
								<span class="text-lg">{goal.identityIcon}</span>
							{/if}
							{#if goal.identityName}
								<span class="font-medium text-gray-700">{goal.identityName}</span>
							{:else}
								<span class="text-gray-400">{$t('goals.noIdentity')}</span>
							{/if}
							<svg class="w-3 h-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
							</svg>
						</button>
					{:else}
						{#if goal.identityIcon}
							<span class="text-lg">{goal.identityIcon}</span>
						{/if}
						{#if goal.identityName}
							<span class="font-medium text-gray-700">{goal.identityName}</span>
						{:else}
							<span class="text-gray-400">{$t('goals.noIdentity')}</span>
						{/if}
					{/if}
				</div>

				<div class="flex items-center gap-4 text-sm text-gray-500">
					{#if editingTargetDate && !goal.isCompleted}
						<div class="flex items-center gap-2">
							<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
							</svg>
							<input
								type="date"
								bind:value={newTargetDate}
								class="px-2 py-1 text-sm border border-gray-300 rounded focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
							/>
							<button
								onclick={saveTargetDate}
								class="text-primary-600 hover:text-primary-700 font-medium"
							>
								{$t('common.save')}
							</button>
							<button
								onclick={cancelEditTargetDate}
								class="text-gray-500 hover:text-gray-700"
							>
								{$t('common.cancel')}
							</button>
						</div>
					{:else if !goal.isCompleted}
						<button
							onclick={startEditTargetDate}
							class="flex items-center gap-1 hover:text-primary-600 transition-colors"
							title="Click to edit target date"
						>
							<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
							</svg>
							{#if goal.targetDate}
								{$t('goals.target')}: {new Date(goal.targetDate + 'T12:00:00').toLocaleDateString()}
							{:else}
								{$t('goals.setTargetDate')}
							{/if}
							<svg class="w-3 h-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
							</svg>
						</button>
					{:else if goal.targetDate}
						<span class="flex items-center gap-1">
							<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
							</svg>
							{$t('goals.target')}: {new Date(goal.targetDate + 'T12:00:00').toLocaleDateString()}
						</span>
					{/if}
					<span>{goal.completedTaskCount}/{goal.taskCount} {$t('goals.tasksCompleted')}</span>
				</div>

				{#if goal.taskCount > 0}
					<div class="mt-4 bg-gray-200 rounded-full h-2">
						<div
							class="bg-primary-600 h-2 rounded-full transition-all duration-300"
							style="width: {(goal.completedTaskCount / goal.taskCount) * 100}%"
						></div>
					</div>
				{/if}
			</div>

			<!-- Add Task Form -->
			{#if !goal.isCompleted}
				<form onsubmit={handleAddTask} class="card p-4 mb-6">
					<!-- Desktop: Title, Identity, Date on first row -->
					<!-- Mobile: Stack vertically in same order -->
					<div class="grid grid-cols-1 sm:grid-cols-[1fr,auto,auto] gap-3">
						<input
							type="text"
							bind:value={newTaskTitle}
							placeholder={$t('goals.addTaskPlaceholder')}
							class="input"
							disabled={addingTask}
						/>
						{#if identities.length > 0}
							<select
								bind:value={newTaskIdentityId}
								class="input sm:w-44"
								disabled={addingTask}
								title={$t('goals.form.linkToIdentity')}
							>
								<option value="">{$t('goals.noIdentity')}</option>
								{#each identities as identity (identity.id)}
									<option value={identity.id}>{identity.icon ? `${identity.icon} ` : ''}{identity.name}</option>
								{/each}
							</select>
						{/if}
						<input
							type="date"
							bind:value={newTaskDueDate}
							class="input sm:w-40"
							disabled={addingTask}
							title={$t('goals.dueDate')}
						/>
					</div>
					
					<!-- Description (larger box) -->
					<div class="mt-3">
						<textarea
							bind:value={newTaskDescription}
							placeholder={$t('goals.form.taskDescriptionPlaceholder')}
							class="input w-full resize-none"
							rows="2"
							disabled={addingTask}
						></textarea>
					</div>
					
					<!-- Submit button - right aligned on desktop, full width on mobile -->
					<div class="mt-3 flex justify-end">
						<button 
							type="submit" 
							disabled={addingTask || !newTaskTitle.trim()} 
							class="btn-primary w-full sm:w-auto"
						>
							{addingTask ? $t('goals.adding') : $t('goals.addTask')}
						</button>
					</div>
				</form>
			{/if}

			<!-- Readonly notice for completed goals -->
			{#if goal.isCompleted}
				<div class="bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 rounded-lg mb-6 flex items-center gap-2">
					<svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
					</svg>
					<span>{$t('goals.readonlyNotice')}</span>
				</div>
			{/if}

			<!-- Tasks List - Always show both sections -->
			<section class="mb-6">
				<h2 class="text-lg font-semibold text-gray-900 mb-3">{$t('goals.pendingTasksSection')} ({pendingTasks.length})</h2>
				<div class="card divide-y divide-gray-200 {goal.isCompleted ? 'opacity-60' : ''}">
					{#if pendingTasks.length > 0}
						{#each pendingTasks as task (task.id)}
							<div
								class="relative p-4 flex items-center gap-3 transition-all duration-300
									{goal.isCompleted ? '' : 'hover:bg-gray-50'}
									{transitioningTaskIds.includes(task.id) ? 'opacity-50 scale-95 bg-green-50' : ''}
									{newlyArrivedTaskIds.includes(task.id) ? 'animate-slide-in-highlight' : ''}
									{snoozingTaskIds.includes(task.id) ? 'bg-amber-50' : ''}"
							>
								<!-- Snooze animation overlay -->
								{#if snoozingTaskIds.includes(task.id)}
									<div class="absolute inset-0 flex items-center justify-center bg-amber-100/80 rounded-lg animate-snooze-pulse z-10">
										<span class="text-amber-700 font-bold text-lg flex items-center gap-2">
											💤 +7 days
										</span>
									</div>
								{/if}
								{#if goal.isCompleted}
									<div class="flex-shrink-0 w-5 h-5 rounded-full border-2 border-gray-300"></div>
								{:else}
									<button
										onclick={() => handleCompleteTask(task.id)}
										class="flex-shrink-0 w-5 h-5 rounded-full border-2 border-gray-300 hover:border-primary-500 transition-all duration-200
											{transitioningTaskIds.includes(task.id) ? 'bg-green-500 border-green-500' : ''}"
										title={$t('goals.markCompleted')}
									>
										{#if transitioningTaskIds.includes(task.id)}
											<svg class="w-3 h-3 text-white m-auto" fill="currentColor" viewBox="0 0 24 24">
												<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
											</svg>
										{/if}
									</button>
								{/if}
								{#if task.identityIcon}
									<span class="flex-shrink-0 text-lg" title={task.identityName || 'Identity'}>{task.identityIcon}</span>
								{/if}
								<div class="flex-1 min-w-0">
									<p class="text-gray-900 {transitioningTaskIds.includes(task.id) ? 'line-through text-gray-500' : ''}">{task.title}</p>
									{#if task.description}
										<p class="text-sm text-gray-500 mt-0.5 line-clamp-2">{task.description}</p>
									{/if}
									<div class="flex items-center gap-3 mt-1">
										{#if task.dueDate}
											<span class="text-sm {getDueDateColor(task.dueDate)}">{formatDueDate(task.dueDate)}</span>
										{/if}
										{#if task.isRepeatable && task.repeatSchedule}
											<span class="inline-flex items-center text-xs text-primary-600">
												<svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
													<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
												</svg>
												{task.repeatSchedule.frequency}
											</span>
										{/if}
									</div>
								</div>
								{#if !goal.isCompleted}
									<button
										onclick={() => handleSnooze(task)}
										class="text-gray-400 hover:text-amber-500 transition-colors p-1"
										title={$t('goals.snooze')}
										disabled={snoozingTaskIds.includes(task.id)}
									>
										<span class="text-lg">💤</span>
									</button>
									<button
										onclick={() => openEditPopup(task)}
										class="text-gray-400 hover:text-primary-600 transition-colors p-1"
										title={$t('goals.editTask')}
									>
										<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
										</svg>
									</button>
									<button
										onclick={() => handleDeleteTask(task.id)}
										class="text-gray-400 hover:text-red-500 transition-colors p-1"
										title={$t('goals.deleteTask')}
									>
										<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
										</svg>
									</button>
								{/if}
							</div>
						{/each}
					{:else}
						<div class="p-6 text-center text-gray-400">
							{#if goal.isCompleted}
								{$t('goals.allTasksCompleted')}
							{:else}
								{$t('goals.noPendingTasks')}
							{/if}
						</div>
					{/if}
				</div>
			</section>

			<section>
				<h2 class="text-lg font-semibold text-gray-900 mb-3">{$t('goals.completedTasksSection')} ({completedTasks.length})</h2>
				<div class="card divide-y divide-gray-200 {goal.isCompleted ? 'opacity-60' : ''}">
					{#if completedTasks.length > 0}
						{#each completedTasks as task (task.id)}
							<div
								class="p-4 flex items-center gap-3 bg-green-50 transition-all duration-300
									{transitioningTaskIds.includes(task.id) ? 'opacity-50 scale-95 bg-gray-50' : ''}
									{newlyArrivedTaskIds.includes(task.id) ? 'animate-slide-in-highlight-green' : ''}"
							>
								{#if goal.isCompleted}
									<div class="flex-shrink-0 w-5 h-5 rounded-full bg-green-500 flex items-center justify-center">
										<svg class="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 24 24">
											<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
										</svg>
									</div>
								{:else}
									<button
										onclick={() => handleCompleteTask(task.id)}
										class="flex-shrink-0 w-5 h-5 rounded-full bg-green-500 flex items-center justify-center hover:bg-green-600 transition-colors"
										title={$t('goals.markIncomplete')}
									>
										<svg class="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 24 24">
											<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
										</svg>
									</button>
								{/if}
								{#if task.identityIcon}
									<span class="flex-shrink-0 text-lg opacity-50" title={task.identityName || 'Identity'}>{task.identityIcon}</span>
								{/if}
								<div class="flex-1 min-w-0">
									<p class="text-gray-500 line-through">{task.title}</p>
									{#if task.description}
										<p class="text-sm text-gray-400 mt-0.5 line-clamp-1 line-through">{task.description}</p>
									{/if}
								</div>
								{#if !goal.isCompleted}
									<button
										onclick={() => handleDeleteTask(task.id)}
										class="text-gray-400 hover:text-red-500 transition-colors p-1"
										title={$t('goals.deleteTask')}
									>
										<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
										</svg>
									</button>
								{/if}
							</div>
						{/each}
					{:else}
						<div class="p-6 text-center text-gray-400">
							{$t('goals.noCompletedTasks')}
						</div>
					{/if}
				</div>
			</section>
		{/if}
	</main>

	<!-- Edit Task Popup -->
	{#if showEditPopup && editingTask}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-md w-full p-6">
				<h3 class="text-lg font-semibold text-gray-900 mb-4">{$t('goals.editTask')}</h3>

				<div class="space-y-4">
					<div>
						<label for="editTitle" class="block text-sm font-medium text-gray-700 mb-1">{$t('goals.form.taskTitle')}</label>
						<input
							type="text"
							id="editTitle"
							bind:value={editTitle}
							class="input w-full"
							placeholder={$t('goals.form.taskTitlePlaceholder')}
						/>
					</div>

					<div>
						<label for="editDescription" class="block text-sm font-medium text-gray-700 mb-1">
							{$t('goals.form.taskDescription')} <span class="text-gray-400 font-normal">({$t('goals.form.optional')})</span>
						</label>
						<textarea
							id="editDescription"
							bind:value={editDescription}
							class="input w-full resize-none"
							rows="3"
							placeholder={$t('goals.form.taskDescriptionPlaceholder')}
						></textarea>
					</div>

					<div>
						<label for="editDueDate" class="block text-sm font-medium text-gray-700 mb-1">
							{$t('goals.dueDate')} <span class="text-gray-400 font-normal">({$t('goals.form.optional')})</span>
						</label>
						<input
							type="date"
							id="editDueDate"
							bind:value={editDueDate}
							class="input w-full"
						/>
					</div>

					{#if identities.length > 0}
						<div>
							<label for="editIdentity" class="block text-sm font-medium text-gray-700 mb-1">
								{$t('goals.form.identity')} <span class="text-gray-400 font-normal">({$t('goals.form.optional')})</span>
							</label>
							<select
								id="editIdentity"
								bind:value={editIdentityId}
								class="input w-full"
							>
								<option value="">{$t('goals.noIdentity')}</option>
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
					>
						{$t('common.cancel')}
					</button>
					<button
						onclick={handleSaveEdit}
						disabled={!editTitle.trim()}
						class="btn-primary"
					>
						{$t('common.save')}
					</button>
				</div>
			</div>
		</div>
	{/if}

	<!-- Postpone Popup -->
	{#if showPostponePopup && postponingTask}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-sm w-full p-6">
				<h3 class="text-lg font-semibold text-gray-900 mb-4">{$t('goals.changeDueDate')}</h3>
				<p class="text-gray-600 mb-4 truncate">{postponingTask.title}</p>

				<div class="mb-6">
					<label for="newDueDate" class="block text-sm font-medium text-gray-700 mb-2">{$t('goals.dueDate')}</label>
					<input
						type="date"
						id="newDueDate"
						bind:value={newDueDate}
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
</div>
