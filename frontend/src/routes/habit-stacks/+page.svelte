<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import {
		getHabitStacks,
		createHabitStack,
		updateHabitStack,
		deleteHabitStack,
		addStackItem,
		deleteStackItem,
		reorderHabitStacks
	} from '$lib/api/habitStacks';
	import type {
		HabitStack,
		HabitStackItemRequest
	} from '$lib/types';

	let stacks = $state<HabitStack[]>([]);
	let loading = $state(true);
	let error = $state('');

	// Create popup state
	let showCreatePopup = $state(false);
	let createName = $state('');
	let createItems = $state<HabitStackItemRequest[]>([{ cueDescription: '', habitDescription: '' }]);
	let createLoading = $state(false);
	let createError = $state('');

	// Edit popup state
	let showEditPopup = $state(false);
	let editingStack = $state<HabitStack | null>(null);
	let editName = $state('');
	let editIsActive = $state(true);
	let editLoading = $state(false);
	let editError = $state('');

	// Add item state
	let newItemCue = $state('');
	let newItemHabit = $state('');
	let addingItem = $state(false);

	// Reorder popup state
	let showReorderPopup = $state(false);
	let reorderList = $state<{ id: string; name: string }[]>([]);
	let reordering = $state(false);
	let draggedIndex = $state<number | null>(null);

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		if (!$auth.user) {
			goto('/auth/login');
			return;
		}

		await loadStacks();
	});

	async function loadStacks() {
		try {
			stacks = await getHabitStacks();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load habit stacks';
		} finally {
			loading = false;
		}
	}

	// Create popup functions
	function openCreatePopup() {
		createName = '';
		createItems = [{ cueDescription: '', habitDescription: '' }];
		createError = '';
		showCreatePopup = true;
	}

	function closeCreatePopup() {
		showCreatePopup = false;
		createName = '';
		createItems = [{ cueDescription: '', habitDescription: '' }];
		createError = '';
	}

	function addCreateItem() {
		const lastItem = createItems[createItems.length - 1];
		createItems = [
			...createItems,
			{
				cueDescription: lastItem?.habitDescription || '',
				habitDescription: ''
			}
		];
	}

	function removeCreateItem(index: number) {
		if (createItems.length > 1) {
			createItems = createItems.filter((_, i) => i !== index);
		}
	}

	function updateCreateItem(
		index: number,
		field: 'cueDescription' | 'habitDescription',
		value: string
	) {
		createItems = createItems.map((item, i) => (i === index ? { ...item, [field]: value } : item));
	}

	async function handleCreate() {
		if (!createName.trim()) {
			createError = 'Stack name is required';
			return;
		}

		const validItems = createItems.filter(
			(item) => item.cueDescription.trim() && item.habitDescription.trim()
		);
		if (validItems.length === 0) {
			createError = 'At least one complete habit item is required';
			return;
		}

		createLoading = true;
		createError = '';

		try {
			const stack = await createHabitStack({ name: createName.trim(), items: validItems });
			stacks = [stack, ...stacks];
			closeCreatePopup();
		} catch (e) {
			createError = e instanceof Error ? e.message : 'Failed to create habit stack';
		} finally {
			createLoading = false;
		}
	}

	// Edit popup functions
	function openEditPopup(stack: HabitStack) {
		editingStack = stack;
		editName = stack.name;
		editIsActive = stack.isActive;
		editError = '';
		newItemCue = '';
		newItemHabit = '';
		showEditPopup = true;
	}

	function closeEditPopup() {
		showEditPopup = false;
		editingStack = null;
		editName = '';
		editIsActive = true;
		editError = '';
		newItemCue = '';
		newItemHabit = '';
	}

	async function handleSaveEdit() {
		if (!editingStack || !editName.trim()) {
			editError = 'Stack name is required';
			return;
		}

		editLoading = true;
		editError = '';

		try {
			const updated = await updateHabitStack(editingStack.id, {
				name: editName.trim(),
				isActive: editIsActive
			});
			stacks = stacks.map((s) => (s.id === updated.id ? updated : s));
			editingStack = updated;
		} catch (e) {
			editError = e instanceof Error ? e.message : 'Failed to update habit stack';
		} finally {
			editLoading = false;
		}
	}

	async function handleAddItem() {
		if (!editingStack || !newItemCue.trim() || !newItemHabit.trim()) return;

		addingItem = true;
		try {
			const updated = await addStackItem(editingStack.id, {
				cueDescription: newItemCue.trim(),
				habitDescription: newItemHabit.trim()
			});
			stacks = stacks.map((s) => (s.id === updated.id ? updated : s));
			editingStack = updated;
			newItemCue = '';
			newItemHabit = '';
		} catch (e) {
			editError = e instanceof Error ? e.message : 'Failed to add item';
		} finally {
			addingItem = false;
		}
	}

	async function handleDeleteItem(itemId: string) {
		if (!editingStack) return;
		if (!confirm('Delete this habit from the stack?')) return;

		try {
			await deleteStackItem(itemId);
			const updatedItems = editingStack.items.filter((i) => i.id !== itemId);
			const updated = { ...editingStack, items: updatedItems };
			stacks = stacks.map((s) => (s.id === updated.id ? updated : s));
			editingStack = updated;
		} catch (e) {
			editError = e instanceof Error ? e.message : 'Failed to delete item';
		}
	}

	async function handleDelete(id: string) {
		if (!confirm('Are you sure you want to delete this habit stack?')) return;

		try {
			await deleteHabitStack(id);
			stacks = stacks.filter((s) => s.id !== id);
			if (editingStack?.id === id) {
				closeEditPopup();
			}
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to delete habit stack';
		}
	}

	function getStreakEmoji(streak: number): string {
		if (streak >= 30) return '🏆';
		if (streak >= 14) return '💪';
		if (streak >= 7) return '🔥';
		if (streak >= 3) return '⚡';
		return '';
	}

	// Reorder popup functions
	function openReorderPopup() {
		reorderList = stacks.map((s) => ({ id: s.id, name: s.name }));
		showReorderPopup = true;
	}

	function closeReorderPopup() {
		showReorderPopup = false;
		reorderList = [];
		draggedIndex = null;
	}

	function moveItemUp(index: number) {
		if (index === 0) return;
		const newList = [...reorderList];
		[newList[index - 1], newList[index]] = [newList[index], newList[index - 1]];
		reorderList = newList;
	}

	function moveItemDown(index: number) {
		if (index === reorderList.length - 1) return;
		const newList = [...reorderList];
		[newList[index], newList[index + 1]] = [newList[index + 1], newList[index]];
		reorderList = newList;
	}

	function handleDragStart(index: number) {
		draggedIndex = index;
	}

	function handleDragOver(e: DragEvent, index: number) {
		e.preventDefault();
		if (draggedIndex === null || draggedIndex === index) return;

		const newList = [...reorderList];
		const draggedItem = newList[draggedIndex];
		newList.splice(draggedIndex, 1);
		newList.splice(index, 0, draggedItem);
		reorderList = newList;
		draggedIndex = index;
	}

	function handleDragEnd() {
		draggedIndex = null;
	}

	async function saveReorder() {
		reordering = true;
		try {
			await reorderHabitStacks(reorderList.map((s) => s.id));
			// Update local stacks order to match
			const orderedStacks = reorderList
				.map((item) => stacks.find((s) => s.id === item.id))
				.filter((s): s is HabitStack => s !== undefined);
			stacks = orderedStacks;
			closeReorderPopup();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to reorder stacks';
		} finally {
			reordering = false;
		}
	}
</script>

<div class="min-h-screen bg-gray-50">
	<main class="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
		<!-- Page Header -->
		<div class="flex items-center justify-between mb-6">
			<h1 class="text-2xl font-bold text-gray-900">Habit Stacks</h1>
			<div class="flex items-center gap-2">
				{#if stacks.length > 1}
					<button
						onclick={openReorderPopup}
						class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
						title="Reorder stacks"
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
						</svg>
					</button>
				{/if}
				<button onclick={openCreatePopup} class="btn-primary text-sm">New Stack</button>
			</div>
		</div>

		{#if loading}
			<div class="flex justify-center py-12">
				<div
					class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"
				></div>
			</div>
		{:else if error}
			<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
				{error}
				<button onclick={() => (error = '')} class="float-right text-red-500 hover:text-red-700"
					>&times;</button
				>
			</div>
		{:else}
			<!-- Info Card -->
			<div class="card p-6 mb-6 bg-gradient-to-r from-purple-50 to-pink-50">
			<h2 class="font-semibold text-gray-900 mb-2">Habit Stacking</h2>
			<p class="text-sm text-gray-600 mb-4">
				Habit stacking works by attaching a new habit to something you already do.
				By anchoring small improvements to existing routines, you reduce friction
				and make consistency easier. Over time, these linked actions form powerful
				daily rhythms that run almost automatically.
			</p>

			<div class="mt-4 border-t border-pink-100 pt-4">
				<h3 class="text-sm font-medium text-gray-800 mb-1">Build Your Own Habit Stacks</h3>
				<p class="text-sm text-gray-600">
				Start with habits that already happen every day, then decide what positive
				action you want to follow them with. Simple rules like “After I finish X,
				I will do Y” turn ordinary moments into reliable cues for better behavior.
				</p>
			</div>
			</div>

			{#if stacks.length > 0}
				<div class="space-y-4">
					{#each stacks as stack (stack.id)}
						<button
							type="button"
							onclick={() => openEditPopup(stack)}
							class="card-hover p-5 text-left w-full"
						>
								<div class="flex items-center justify-between mb-3">
									<h3 class="font-semibold text-gray-900 text-lg">{stack.name}</h3>
									<div class="flex items-center gap-2">
										{#if !stack.isActive}
											<span class="text-xs px-2 py-0.5 rounded-full bg-gray-100 text-gray-600"
												>Inactive</span
											>
										{/if}
										<span class="text-sm text-gray-500"
											>{stack.items.length} habit{stack.items.length !== 1 ? 's' : ''}</span
										>
									</div>
								</div>

							{#if stack.items.length > 0}
								<div class="relative ml-2">
									{#each stack.items as item, i (item.id)}
										<div class="relative pl-6 pb-2 last:pb-0">
											<!-- Vertical connecting line -->
											{#if i < stack.items.length - 1}
												<div class="absolute left-[7px] top-4 bottom-0 w-0.5 bg-gray-200"></div>
											{/if}
											<!-- Step circle -->
											<div
												class="absolute left-0 top-1.5 w-4 h-4 rounded-full border-2 border-primary-500 bg-white flex items-center justify-center z-10"
											>
												<span class="text-[10px] font-medium text-primary-600">{i + 1}</span>
											</div>
											<div class="bg-gray-50 rounded-lg p-3">
												<p class="text-sm text-gray-500">
													<span class="font-medium text-gray-700">After I</span>
													{item.cueDescription}
												</p>
												<p class="text-gray-900 mt-0.5">
													<span class="font-medium text-primary-600">I will</span>
													{item.habitDescription}
												</p>
												{#if item.currentStreak > 0}
													<span
														class="inline-flex items-center gap-1 text-xs text-orange-600 mt-1"
													>
														{getStreakEmoji(item.currentStreak)} {item.currentStreak} day streak
													</span>
												{/if}
											</div>
										</div>
									{/each}
								</div>
							{:else}
								<p class="text-sm text-gray-500 italic">No habits in this stack yet</p>
							{/if}
						</button>
					{/each}
				</div>
			{:else}
				<div class="card p-12 text-center">
					<div
						class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center"
					>
						<span class="text-3xl">🔗</span>
					</div>
					<h3 class="text-lg font-medium text-gray-900 mb-2">No habit stacks yet</h3>
					<p class="text-gray-500 mb-6">Create your first habit stack to chain habits together.</p>
					<button onclick={openCreatePopup} class="btn-primary">Create Habit Stack</button>
				</div>
			{/if}
		{/if}
	</main>

	<!-- Create Popup -->
	{#if showCreatePopup}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-lg w-full max-h-[90vh] overflow-y-auto">
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-gray-900">Create Habit Stack</h2>
						<button onclick={closeCreatePopup} class="text-gray-400 hover:text-gray-600">
							<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M6 18L18 6M6 6l12 12"
								/>
							</svg>
						</button>
					</div>

					{#if createError}
						<div
							class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4"
						>
							{createError}
						</div>
					{/if}

					<div class="space-y-6">
						<div>
							<label for="createName" class="block text-sm font-medium text-gray-700 mb-1"
								>Stack Name</label
							>
							<input
								type="text"
								id="createName"
								bind:value={createName}
								placeholder="Morning Routine"
								class="input"
							/>
						</div>

						<div>
							<label class="block text-sm font-medium text-gray-700 mb-3">Habit Chain</label>
							<div class="space-y-4">
								{#each createItems as item, i (i)}
									<div
										class="relative pl-6 pb-4 {i < createItems.length - 1
											? 'border-l-2 border-gray-200 ml-3'
											: 'ml-3'}"
									>
										<div
											class="absolute left-0 top-0 w-6 h-6 -ml-3 rounded-full bg-primary-100 border-2 border-primary-500 flex items-center justify-center"
										>
											<span class="text-xs font-medium text-primary-700">{i + 1}</span>
										</div>
										<div class="bg-gray-50 rounded-lg p-4">
											<div class="space-y-3">
												<div>
													<label class="block text-xs font-medium text-gray-500 mb-1"
														>After I...</label
													>
													<input
														type="text"
														value={item.cueDescription}
														oninput={(e) =>
															updateCreateItem(
																i,
																'cueDescription',
																(e.target as HTMLInputElement).value
															)}
														placeholder={i === 0 ? 'wake up' : 'previous habit'}
														class="input text-sm"
													/>
												</div>
												<div>
													<label class="block text-xs font-medium text-gray-500 mb-1"
														>I will...</label
													>
													<input
														type="text"
														value={item.habitDescription}
														oninput={(e) =>
															updateCreateItem(
																i,
																'habitDescription',
																(e.target as HTMLInputElement).value
															)}
														placeholder="drink a glass of water"
														class="input text-sm"
													/>
												</div>
											</div>
											{#if createItems.length > 1}
												<button
													type="button"
													onclick={() => removeCreateItem(i)}
													class="mt-2 text-xs text-red-500 hover:text-red-700"
												>
													Remove
												</button>
											{/if}
										</div>
									</div>
								{/each}
							</div>

							<button
								type="button"
								onclick={addCreateItem}
								class="mt-4 ml-3 flex items-center text-sm text-primary-600 hover:text-primary-700"
							>
								<svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M12 4v16m8-8H4"
									/>
								</svg>
								Add another habit
							</button>
						</div>
					</div>

					<div class="flex justify-end gap-3 mt-6 pt-4 border-t border-gray-200">
						<button
							type="button"
							onclick={closeCreatePopup}
							class="btn-secondary"
							disabled={createLoading}
						>
							Cancel
						</button>
						<button
							type="button"
							onclick={handleCreate}
							class="btn-primary"
							disabled={createLoading}
						>
							{createLoading ? 'Creating...' : 'Create Stack'}
						</button>
					</div>
				</div>
			</div>
		</div>
	{/if}

	<!-- Edit Popup -->
	{#if showEditPopup && editingStack}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-lg w-full max-h-[90vh] overflow-y-auto">
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-gray-900">Edit Habit Stack</h2>
						<button onclick={closeEditPopup} class="text-gray-400 hover:text-gray-600">
							<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M6 18L18 6M6 6l12 12"
								/>
							</svg>
						</button>
					</div>

					{#if editError}
						<div
							class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4"
						>
							{editError}
						</div>
					{/if}

					<div class="space-y-6">
						<!-- Stack Settings -->
						<div class="space-y-4">
							<div>
								<label for="editName" class="block text-sm font-medium text-gray-700 mb-1"
									>Stack Name</label
								>
								<input type="text" id="editName" bind:value={editName} class="input" />
							</div>

							<div class="flex items-center justify-between">
								<label for="editActive" class="text-sm font-medium text-gray-700">Active</label>
								<button
									type="button"
									id="editActive"
									onclick={() => (editIsActive = !editIsActive)}
									class="relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 {editIsActive
										? 'bg-primary-600'
										: 'bg-gray-200'}"
								>
									<span
										class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out {editIsActive
											? 'translate-x-5'
											: 'translate-x-0'}"
									></span>
								</button>
							</div>

							<button
								type="button"
								onclick={handleSaveEdit}
								class="btn-primary w-full"
								disabled={editLoading}
							>
								{editLoading ? 'Saving...' : 'Save Changes'}
							</button>
						</div>

						<hr class="border-gray-200" />

						<!-- Existing Items -->
						<div>
							<h3 class="text-sm font-medium text-gray-700 mb-3">
								Habits in Stack ({editingStack.items.length})
							</h3>
							{#if editingStack.items.length > 0}
								<div class="relative ml-2">
									{#each editingStack.items as item, i (item.id)}
										<div class="relative pl-6 pb-3 last:pb-0">
											<!-- Vertical connecting line -->
											{#if i < editingStack.items.length - 1}
												<div class="absolute left-[7px] top-4 bottom-0 w-0.5 bg-primary-200"></div>
											{/if}
											<!-- Step circle -->
											<div
												class="absolute left-0 top-2 w-4 h-4 rounded-full bg-primary-100 border-2 border-primary-500 flex items-center justify-center z-10"
											>
												<span class="text-[10px] font-medium text-primary-700">{i + 1}</span>
											</div>
											<div class="bg-gray-50 rounded-lg p-3 group relative">
												<p class="text-sm text-gray-600">
													<span class="font-medium text-gray-800">After I</span>
													{item.cueDescription}
												</p>
												<p class="text-gray-900 mt-1">
													<span class="font-medium text-primary-600">I will</span>
													{item.habitDescription}
												</p>
												{#if item.currentStreak > 0}
													<p class="text-xs text-orange-600 mt-2 flex items-center gap-1">
														{getStreakEmoji(item.currentStreak)} {item.currentStreak} day streak
													</p>
												{/if}
												<button
													type="button"
													onclick={() => handleDeleteItem(item.id)}
													class="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity p-1 text-gray-400 hover:text-red-500"
													title="Delete item"
												>
													<svg
														class="w-4 h-4"
														fill="none"
														stroke="currentColor"
														viewBox="0 0 24 24"
													>
														<path
															stroke-linecap="round"
															stroke-linejoin="round"
															stroke-width="2"
															d="M6 18L18 6M6 6l12 12"
														/>
													</svg>
												</button>
											</div>
										</div>
									{/each}
								</div>
							{:else}
								<p class="text-sm text-gray-500 text-center py-4">No habits in this stack yet</p>
							{/if}
						</div>

						<hr class="border-gray-200" />

						<!-- Add New Item -->
						<div>
							<h3 class="text-sm font-medium text-gray-700 mb-3">Add New Habit</h3>
							<div class="bg-gray-50 rounded-lg p-4 space-y-3">
								<div>
									<label class="block text-xs font-medium text-gray-500 mb-1">After I...</label>
									<input
										type="text"
										bind:value={newItemCue}
										placeholder={editingStack.items.length > 0
											? editingStack.items[editingStack.items.length - 1].habitDescription
											: 'previous habit'}
										class="input text-sm"
									/>
								</div>
								<div>
									<label class="block text-xs font-medium text-gray-500 mb-1">I will...</label>
									<input
										type="text"
										bind:value={newItemHabit}
										placeholder="new habit"
										class="input text-sm"
									/>
								</div>
								<button
									type="button"
									onclick={handleAddItem}
									class="btn-primary w-full text-sm"
									disabled={addingItem || !newItemCue.trim() || !newItemHabit.trim()}
								>
									{addingItem ? 'Adding...' : 'Add Habit'}
								</button>
							</div>
						</div>

						<hr class="border-gray-200" />

						<!-- Delete Stack -->
						<div>
							<button
								type="button"
								onclick={() => handleDelete(editingStack!.id)}
								class="w-full py-2 text-red-600 hover:text-red-700 text-sm font-medium"
							>
								Delete Habit Stack
							</button>
						</div>
					</div>
				</div>
			</div>
		</div>
	{/if}

	<!-- Reorder Popup -->
	{#if showReorderPopup}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div class="bg-white rounded-xl shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto">
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-gray-900">Reorder Stacks</h2>
						<button
							onclick={closeReorderPopup}
							class="text-gray-400 hover:text-gray-600"
							title="Close"
						>
							<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M6 18L18 6M6 6l12 12"
								/>
							</svg>
						</button>
					</div>

					<p class="text-sm text-gray-500 mb-4">Drag items or use arrows to reorder your habit stacks.</p>

					<div class="space-y-2">
						{#each reorderList as item, index (item.id)}
							<div
								draggable="true"
								ondragstart={() => handleDragStart(index)}
								ondragover={(e) => handleDragOver(e, index)}
								ondragend={handleDragEnd}
								class="flex items-center gap-3 p-3 bg-gray-50 rounded-lg border border-gray-200 cursor-move hover:bg-gray-100 transition-colors {draggedIndex === index ? 'opacity-50 border-primary-500' : ''}"
							>
								<!-- Drag handle -->
								<div class="text-gray-400">
									<svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
										<path d="M8 6a2 2 0 1 1-4 0 2 2 0 0 1 4 0zM8 12a2 2 0 1 1-4 0 2 2 0 0 1 4 0zM8 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0zM14 6a2 2 0 1 1-4 0 2 2 0 0 1 4 0zM14 12a2 2 0 1 1-4 0 2 2 0 0 1 4 0zM14 18a2 2 0 1 1-4 0 2 2 0 0 1 4 0z" />
									</svg>
								</div>

								<!-- Position number -->
								<span class="text-sm font-medium text-gray-500 w-6">{index + 1}.</span>

								<!-- Stack name -->
								<span class="flex-1 font-medium text-gray-900">{item.name}</span>

								<!-- Up/Down buttons -->
								<div class="flex flex-col gap-0.5">
									<button
										type="button"
										onclick={() => moveItemUp(index)}
										disabled={index === 0}
										class="p-1 rounded hover:bg-gray-200 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
										title="Move up"
									>
										<svg class="w-4 h-4 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
										</svg>
									</button>
									<button
										type="button"
										onclick={() => moveItemDown(index)}
										disabled={index === reorderList.length - 1}
										class="p-1 rounded hover:bg-gray-200 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
										title="Move down"
									>
										<svg class="w-4 h-4 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
										</svg>
									</button>
								</div>
							</div>
						{/each}
					</div>

					<div class="flex justify-end gap-3 mt-6 pt-4 border-t border-gray-200">
						<button
							type="button"
							onclick={closeReorderPopup}
							class="btn-secondary"
							disabled={reordering}
						>
							Cancel
						</button>
						<button
							type="button"
							onclick={saveReorder}
							class="btn-primary"
							disabled={reordering}
						>
							{reordering ? 'Saving...' : 'Save Order'}
						</button>
					</div>
				</div>
			</div>
		</div>
	{/if}
</div>
