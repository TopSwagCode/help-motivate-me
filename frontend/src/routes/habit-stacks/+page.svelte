<script lang="ts">
	import { onMount, tick } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { commandBar } from '$lib/stores/commandBar';
	import { t } from 'svelte-i18n';
	import { get } from 'svelte/store';
	import {
		getHabitStacks,
		createHabitStack,
		updateHabitStack,
		deleteHabitStack,
		addStackItem,
		deleteStackItem,
		reorderHabitStacks
	} from '$lib/api/habitStacks';
	import { getIdentities } from '$lib/api/identities';
	import InfoOverlay from '$lib/components/common/InfoOverlay.svelte';
	import type {
		HabitStack,
		HabitStackItemRequest,
		Identity
	} from '$lib/types';

	let stacks = $state<HabitStack[]>([]);
	let identities = $state<Identity[]>([]);
	let loading = $state(true);
	let error = $state('');

	// Create popup state
	let showCreatePopup = $state(false);
	let createName = $state('');
	let createIdentityId = $state<string | undefined>(undefined);
	let createItems = $state<HabitStackItemRequest[]>([{ cueDescription: '', habitDescription: '' }]);
	let createLoading = $state(false);
	let createError = $state('');
	let createContainerRef = $state<HTMLDivElement | null>(null);

	// Edit popup state
	let showEditPopup = $state(false);
	let editingStack = $state<HabitStack | null>(null);
	let editName = $state('');
	let editIdentityId = $state<string | undefined>(undefined);
	let editIsActive = $state(true);
	let editLoading = $state(false);
	let editError = $state('');

	// Add item state
	let newItemCue = $state('');
	let newItemHabit = $state('');
	let newItemHabitInputRef = $state<HTMLInputElement | null>(null);
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

		await loadData();
	});

	async function loadData() {
		try {
			const [stacksData, identitiesData] = await Promise.all([
				getHabitStacks(),
				getIdentities()
			]);
			stacks = stacksData;
			identities = identitiesData;
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load data';
		} finally {
			loading = false;
		}
	}

	// Create popup functions
	function openCreatePopup() {
		createName = '';
		createIdentityId = undefined;
		createItems = [{ cueDescription: '', habitDescription: '' }];
		createError = '';
		showCreatePopup = true;
	}

	function closeCreatePopup() {
		showCreatePopup = false;
		createName = '';
		createIdentityId = undefined;
		createItems = [{ cueDescription: '', habitDescription: '' }];
		createError = '';
	}

	async function addCreateItem() {
		const lastItem = createItems[createItems.length - 1];
		createItems = [
			...createItems,
			{
				cueDescription: lastItem?.habitDescription || '',
				habitDescription: ''
			}
		];
		
		// Wait for DOM update, then scroll and focus
		await tick();
		
		// Scroll to absolute bottom of the container
		if (createContainerRef) {
			// Use a timeout to ensure content is fully rendered
			setTimeout(() => {
				if (createContainerRef) {
					createContainerRef.scrollTo({
						top: createContainerRef.scrollHeight + 1000, // Extra padding to ensure bottom
						behavior: 'smooth'
					});
				}
			}, 50);
		}
		
		// Focus the new "I will..." input
		const newIndex = createItems.length - 1;
		const habitInput = document.querySelector(
			`#create-habit-input-${newIndex}`
		) as HTMLInputElement;
		if (habitInput) {
			habitInput.focus();
		}
	}

	function handleCreateItemKeyPress(e: KeyboardEvent, index: number) {
		// If Enter is pressed in the last habit's "I will..." field, add another habit
		if (e.key === 'Enter' && index === createItems.length - 1) {
			e.preventDefault();
			addCreateItem();
		}
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
			createError = get(t)('habitStacks.errors.stackNameRequired');
			return;
		}

		const validItems = createItems.filter(
			(item) => item.cueDescription.trim() && item.habitDescription.trim()
		);
		if (validItems.length === 0) {
			createError = get(t)('habitStacks.errors.atLeastOneHabit');
			return;
		}

		createLoading = true;
		createError = '';

		try {
			const stack = await createHabitStack({ 
				name: createName.trim(), 
				identityId: createIdentityId,
				items: validItems 
			});
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
		editIdentityId = stack.identityId || undefined;
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
		editIdentityId = undefined;
		editIsActive = true;
		editError = '';
		newItemCue = '';
		newItemHabit = '';
	}

	async function handleSaveEdit() {
		if (!editingStack || !editName.trim()) {
			editError = get(t)('habitStacks.errors.stackNameRequired');
			return;
		}

		editLoading = true;
		editError = '';

		try {
			const updated = await updateHabitStack(editingStack.id, {
				name: editName.trim(),
				identityId: editIdentityId,
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
			
			// Wait for DOM update, then focus the "I will..." input for next item
			await tick();
			if (newItemHabitInputRef) {
				newItemHabitInputRef.focus();
			}
		} catch (e) {
			editError = e instanceof Error ? e.message : 'Failed to add item';
		} finally {
			addingItem = false;
		}
	}

	function handleEditItemKeyPress(e: KeyboardEvent, field: 'cue' | 'habit') {
		// If Enter is pressed in the "I will..." field and both fields are filled, add the item
		if (e.key === 'Enter' && field === 'habit' && newItemCue.trim() && newItemHabit.trim()) {
			e.preventDefault();
			handleAddItem();
		}
	}

	async function handleDeleteItem(itemId: string) {
		if (!editingStack) return;
		if (!confirm(get(t)('habitStacks.deleteItemConfirm'))) return;

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
		if (!confirm(get(t)('habitStacks.deleteConfirm'))) return;

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
	<main class="max-w-3xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
		<!-- Page Header -->
		<div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-4 sm:mb-6">
			<InfoOverlay 
				title={$t('habitStacks.pageTitle')} 
				description={$t('habitStacks.info.description')} 
			/>
			<div class="flex items-center gap-2">
				{#if stacks.length > 1}
					<button
						onclick={openReorderPopup}
						class="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition-colors touch-manipulation"
						title={$t('habitStacks.reorderPopup.title')}
					>
						<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
						</svg>
					</button>
				{/if}
				<button onclick={openCreatePopup} class="btn-primary text-sm flex-1 sm:flex-none justify-center">{$t('habitStacks.newStack')}</button>
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
			{#if stacks.length > 0}
				<div class="space-y-4" data-tour="stacks-list">
					{#each stacks as stack (stack.id)}
						<button
							type="button"
							onclick={() => openEditPopup(stack)}
							class="rounded-lg overflow-hidden text-left w-full transition-all hover:shadow-md"
							style="background-color: {stack.identityColor || '#6366f1'}08; border: 1px solid {stack.identityColor || '#6366f1'}20"
						>
							<!-- Header with identity color -->
							<div 
								class="flex items-center justify-between px-5 py-3"
								style="background-color: {stack.identityColor || '#6366f1'}15"
							>
								<div class="flex items-center gap-2">
									{#if stack.identityName}
										<span class="text-lg" title={stack.identityName}>
											{identities.find(i => i.id === stack.identityId)?.icon || '🎯'}
										</span>
									{/if}
									<h3 class="font-semibold text-gray-900 text-lg">{stack.name}</h3>
								</div>
								<div class="flex items-center gap-2">
									{#if stack.identityName}
										<span 
											class="text-xs px-2 py-0.5 rounded-full"
											style="background-color: {stack.identityColor || '#6366f1'}20; color: {stack.identityColor || '#6366f1'}"
										>
											{stack.identityName}
										</span>
									{/if}
									{#if !stack.isActive}
										<span class="text-xs px-2 py-0.5 rounded-full bg-gray-100 text-gray-600"
											>{$t('habitStacks.inactive')}</span
										>
									{/if}
									<span class="text-sm text-gray-500"
										>{stack.items.length} {stack.items.length !== 1 ? $t('habitStacks.habits') : $t('habitStacks.habit')}</span
									>
								</div>
							</div>

							<!-- Content -->
							<div class="p-5 pt-3">
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
													<span class="font-medium text-gray-700">{$t('habitStacks.chain.after')}</span>
													{item.cueDescription}
												</p>
												<p class="text-gray-900 mt-0.5">
													<span class="font-medium text-primary-600">{$t('habitStacks.chain.iWill')}</span>
													{item.habitDescription}
												</p>
												{#if item.currentStreak > 0}
													<span
														class="inline-flex items-center gap-1 text-xs text-orange-600 mt-1"
													>
														{getStreakEmoji(item.currentStreak)} {item.currentStreak} {$t('habitStacks.dayStreak')}
													</span>
												{/if}
											</div>
										</div>
									{/each}
								</div>
							{:else}
								<p class="text-sm text-gray-500 italic">{$t('habitStacks.noHabitsYet')}</p>
							{/if}
							</div>
						</button>
					{/each}
				</div>
			{:else}
				<div class="card p-8 sm:p-12">
					<div class="max-w-md mx-auto text-center">
						<div class="w-16 h-16 mx-auto mb-4 bg-gray-100 rounded-full flex items-center justify-center">
							<span class="text-3xl">🔗</span>
						</div>
						<h3 class="text-lg font-medium text-gray-900 mb-2">{$t('habitStacks.emptyTitle')}</h3>
						<p class="text-gray-500 mb-4">{$t('habitStacks.emptyDescription')}</p>
						<p class="text-gray-500 text-sm mb-6 flex items-center justify-center gap-1 flex-wrap">
							{$t('habitStacks.emptyHowTo')}
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
						</p>
						<button onclick={openCreatePopup} class="btn-primary">{$t('habitStacks.createFirst')}</button>
					</div>
				</div>
			{/if}
		{/if}
	</main>

	<!-- Create Popup -->
	{#if showCreatePopup}
		<div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
			<div 
				bind:this={createContainerRef}
				class="bg-white rounded-xl shadow-xl max-w-lg w-full max-h-[90vh] overflow-y-auto"
			>
				<div class="p-6">
					<div class="flex items-center justify-between mb-6">
						<h2 class="text-xl font-semibold text-gray-900">{$t('habitStacks.createPopup.title')}</h2>
						<button onclick={closeCreatePopup} class="text-gray-400 hover:text-gray-600" aria-label={$t('common.close')}>
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
								>{$t('habitStacks.form.name')}</label
							>
							<input
								type="text"
								id="createName"
								bind:value={createName}
								placeholder={$t('habitStacks.form.namePlaceholder')}
								class="input"
							/>
						</div>

						<!-- Identity selector -->
						<div>
							<label for="createIdentity" class="block text-sm font-medium text-gray-700 mb-1"
								>{$t('habitStacks.form.identity')}</label
							>
							<select
								id="createIdentity"
								bind:value={createIdentityId}
								class="input"
							>
								<option value={undefined}>{$t('habitStacks.form.noIdentity')}</option>
								{#each identities as identity (identity.id)}
									<option value={identity.id}>
										{identity.icon || '🎯'} {identity.name}
									</option>
								{/each}
							</select>
							<p class="text-xs text-gray-500 mt-1">{$t('habitStacks.form.identityHint')}</p>
						</div>

						<div>
							<span class="block text-sm font-medium text-gray-700 mb-3">{$t('habitStacks.createPopup.habitChain')}</span>
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
													<label for="create-cue-input-{i}" class="block text-xs font-medium text-gray-500 mb-1"
														>{$t('habitStacks.createPopup.afterI')}</label
													>
													<input
														id="create-cue-input-{i}"
														type="text"
														value={item.cueDescription}
														oninput={(e) =>
															updateCreateItem(
																i,
																'cueDescription',
																(e.target as HTMLInputElement).value
															)}
														placeholder={i === 0 ? $t('habitStacks.createPopup.placeholderCue') : $t('habitStacks.createPopup.placeholderPreviousHabit')}
														class="input text-sm"
													/>
												</div>
												<div>
													<label for="create-habit-input-{i}" class="block text-xs font-medium text-gray-500 mb-1"
														>{$t('habitStacks.createPopup.iWillDo')}</label
													>
													<input
														id="create-habit-input-{i}"
														type="text"
														value={item.habitDescription}
														oninput={(e) =>
															updateCreateItem(
																i,
																'habitDescription',
																(e.target as HTMLInputElement).value
															)}
														onkeydown={(e) => handleCreateItemKeyPress(e, i)}
														placeholder={$t('habitStacks.createPopup.placeholderHabit')}
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
													{$t('habitStacks.createPopup.remove')}
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
								{$t('habitStacks.createPopup.addAnother')}
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
							{$t('common.cancel')}
						</button>
						<button
							type="button"
							onclick={handleCreate}
							class="btn-primary"
							disabled={createLoading}
						>
							{createLoading ? $t('habitStacks.createPopup.creating') : $t('habitStacks.createPopup.createStack')}
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
						<h2 class="text-xl font-semibold text-gray-900">{$t('habitStacks.editPopup.title')}</h2>
						<button onclick={closeEditPopup} class="text-gray-400 hover:text-gray-600" aria-label={$t('common.close')}>
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
									>{$t('habitStacks.form.name')}</label
								>
								<input type="text" id="editName" bind:value={editName} class="input" />
							</div>

							<!-- Identity selector -->
							<div>
								<label for="editIdentity" class="block text-sm font-medium text-gray-700 mb-1"
									>{$t('habitStacks.form.identity')}</label
								>
								<select
									id="editIdentity"
									bind:value={editIdentityId}
									class="input"
								>
									<option value={undefined}>{$t('habitStacks.form.noIdentity')}</option>
									{#each identities as identity (identity.id)}
										<option value={identity.id}>
											{identity.icon || '🎯'} {identity.name}
										</option>
									{/each}
								</select>
							</div>

							<div class="flex items-center justify-between">
								<label for="editActive" class="text-sm font-medium text-gray-700">{$t('habitStacks.editPopup.active')}</label>
								<button
									type="button"
									id="editActive"
									onclick={() => (editIsActive = !editIsActive)}
									role="switch"
									aria-checked={editIsActive}
									aria-label={$t('habitStacks.editPopup.active')}
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
								{editLoading ? $t('habitStacks.editPopup.saving') : $t('habitStacks.editPopup.saveChanges')}
							</button>
						</div>

						<hr class="border-gray-200" />

						<!-- Existing Items -->
						<div>
							<h3 class="text-sm font-medium text-gray-700 mb-3">
								{$t('habitStacks.editPopup.habitsInStack')} ({editingStack.items.length})
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
													<span class="font-medium text-gray-800">{$t('habitStacks.chain.after')}</span>
													{item.cueDescription}
												</p>
												<p class="text-gray-900 mt-1">
													<span class="font-medium text-primary-600">{$t('habitStacks.chain.iWill')}</span>
													{item.habitDescription}
												</p>
												{#if item.currentStreak > 0}
													<p class="text-xs text-orange-600 mt-2 flex items-center gap-1">
														{getStreakEmoji(item.currentStreak)} {item.currentStreak} {$t('habitStacks.dayStreak')}
													</p>
												{/if}
												<button
													type="button"
													onclick={() => handleDeleteItem(item.id)}
													class="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity p-1 text-gray-400 hover:text-red-500"
													title={$t('common.delete')}
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
								<p class="text-sm text-gray-500 text-center py-4">{$t('habitStacks.noHabitsYet')}</p>
							{/if}
						</div>

						<hr class="border-gray-200" />

						<!-- Add New Item -->
						<div>
							<h3 class="text-sm font-medium text-gray-700 mb-3">{$t('habitStacks.editPopup.addNewHabit')}</h3>
							<div class="bg-gray-50 rounded-lg p-4 space-y-3">
								<div>
									<label for="newItemCue" class="block text-xs font-medium text-gray-500 mb-1">{$t('habitStacks.createPopup.afterI')}</label>
									<input
										id="newItemCue"
										type="text"
										bind:value={newItemCue}
										onkeydown={(e) => handleEditItemKeyPress(e, 'cue')}
										placeholder={editingStack.items.length > 0
											? editingStack.items[editingStack.items.length - 1].habitDescription
											: $t('habitStacks.createPopup.placeholderPreviousHabit')}
										class="input text-sm"
									/>
								</div>
								<div>
									<label for="newItemHabit" class="block text-xs font-medium text-gray-500 mb-1">{$t('habitStacks.createPopup.iWillDo')}</label>
									<input
										bind:this={newItemHabitInputRef}
										id="newItemHabit"
										type="text"
										bind:value={newItemHabit}
										onkeydown={(e) => handleEditItemKeyPress(e, 'habit')}
										placeholder={$t('habitStacks.createPopup.placeholderHabit')}
										class="input text-sm"
									/>
								</div>
								<button
									type="button"
									onclick={handleAddItem}
									class="btn-primary w-full text-sm"
									disabled={addingItem || !newItemCue.trim() || !newItemHabit.trim()}
								>
									{addingItem ? $t('habitStacks.editPopup.adding') : $t('habitStacks.editPopup.addHabit')}
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
								{$t('habitStacks.editPopup.deleteStack')}
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
						<h2 class="text-xl font-semibold text-gray-900">{$t('habitStacks.reorderPopup.title')}</h2>
						<button
							onclick={closeReorderPopup}
							class="text-gray-400 hover:text-gray-600"
							title={$t('common.close')}
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

					<p class="text-sm text-gray-500 mb-4">{$t('habitStacks.reorderPopup.description')}</p>

					<div class="space-y-2">
						{#each reorderList as item, index (item.id)}
							<div
								draggable="true"
								ondragstart={() => handleDragStart(index)}
								ondragover={(e) => handleDragOver(e, index)}
								ondragend={handleDragEnd}
								role="listitem"
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
										aria-label={$t('habitStacks.reorderPopup.moveUp')}
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
										aria-label={$t('habitStacks.reorderPopup.moveDown')}
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
							{$t('common.cancel')}
						</button>
						<button
							type="button"
							onclick={saveReorder}
							class="btn-primary"
							disabled={reordering}
						>
							{reordering ? $t('habitStacks.reorderPopup.saving') : $t('habitStacks.reorderPopup.saveOrder')}
						</button>
					</div>
				</div>
			</div>
		</div>
	{/if}
</div>
