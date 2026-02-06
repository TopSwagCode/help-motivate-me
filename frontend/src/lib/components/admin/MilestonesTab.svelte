<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { MilestoneDefinition, UserMilestone, CreateMilestoneRequest, UpdateMilestoneRequest } from '$lib/types/milestone';
	import { 
		getMilestoneDefinitions, 
		createMilestoneDefinition, 
		updateMilestoneDefinition, 
		toggleMilestoneDefinition, 
		deleteMilestoneDefinition 
	} from '$lib/api/milestones';
	import { milestoneStore } from '$lib/stores/milestones';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';

	let definitions = $state<MilestoneDefinition[]>([]);
	let loading = $state(true);
	let error = $state('');
	let saving = $state(false);

	// Modal states
	let showCreateModal = $state(false);
	let showEditModal = $state(false);
	let showDeleteModal = $state(false);
	let selectedDefinition = $state<MilestoneDefinition | null>(null);

	// Form state
	let formData = $state<CreateMilestoneRequest>({
		code: '',
		titleKey: '',
		descriptionKey: '',
		icon: '🎯',
		triggerEvent: 'HabitCompleted',
		ruleType: 'count',
		ruleData: '{"field": "total_habits_completed", "threshold": 1}',
		animationType: 'confetti',
		animationData: null,
		sortOrder: 0,
		isActive: true
	});

	// Animation data form fields
	let confettiAmount = $state(150);
	let iconType = $state<'emoji' | 'svg' | 'webm'>('emoji');
	let webmUrl = $state('');
	let svgPath = $state('');

	// Available WebM files in static folder
	const availableWebmFiles = [
		{ value: '/celebrate.webm', label: 'Celebrate' },
		{ value: '/celebration2.webm', label: 'Celebration 2' },
		{ value: '/welcome.webm', label: 'Welcome' },
		{ value: '/thinking.webm', label: 'Thinking' },
		{ value: '/manual.webm', label: 'Manual' }
	];

	const triggerEvents = [
		'UserLoggedIn',
		'HabitCompleted',
		'TaskCompleted',
		'IdentityProofAdded',
		'JournalEntryCreated'
	];

	const ruleTypes = [
		{ value: 'count', label: 'Count' },
		{ value: 'window_count', label: 'Window Count' },
		{ value: 'return_after_gap', label: 'Return After Gap' }
	];

	const statFields = [
		'login_count',
		'total_wins',
		'total_habits_completed',
		'total_tasks_completed',
		'total_identity_proofs',
		'total_journal_entries'
	];

	$effect(() => {
		loadData();
	});

	async function loadData() {
		loading = true;
		error = '';
		try {
			definitions = await getMilestoneDefinitions();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load milestone definitions';
		} finally {
			loading = false;
		}
	}

	function previewMilestone(definition: MilestoneDefinition) {
		const previewMilestone: UserMilestone = {
			id: 'preview-' + definition.id,
			milestoneDefinitionId: definition.id,
			code: definition.code,
			titleKey: definition.titleKey,
			descriptionKey: definition.descriptionKey,
			icon: definition.icon,
			animationType: definition.animationType,
			animationData: definition.animationData,
			awardedAt: new Date().toISOString(),
			hasBeenSeen: false
		};
		milestoneStore.previewMilestone(previewMilestone);
	}

	function getRuleTypeLabel(ruleType: string): string {
		const found = ruleTypes.find(r => r.value === ruleType);
		return found?.label ?? ruleType;
	}

	function formatRuleData(ruleData: string): string {
		try {
			const data = JSON.parse(ruleData);
			const parts: string[] = [];
			if (data.field) parts.push(`field: ${data.field}`);
			if (data.threshold) parts.push(`threshold: ${data.threshold}`);
			if (data.count) parts.push(`count: ${data.count}`);
			if (data.days) parts.push(`days: ${data.days}`);
			if (data.gap_days) parts.push(`gap: ${data.gap_days} days`);
			return parts.join(', ');
		} catch {
			return ruleData;
		}
	}

	function openCreateModal() {
		formData = {
			code: '',
			titleKey: 'milestones..title',
			descriptionKey: 'milestones..description',
			icon: '🎯',
			triggerEvent: 'HabitCompleted',
			ruleType: 'count',
			ruleData: '{"field": "total_habits_completed", "threshold": 1}',
			animationType: 'confetti',
			animationData: null,
			sortOrder: definitions.length,
			isActive: true
		};
		confettiAmount = 150;
		iconType = 'emoji';
		webmUrl = '';
		svgPath = '';
		showCreateModal = true;
	}

	function openEditModal(definition: MilestoneDefinition) {
		selectedDefinition = definition;
		formData = {
			code: definition.code,
			titleKey: definition.titleKey,
			descriptionKey: definition.descriptionKey,
			icon: definition.icon,
			triggerEvent: definition.triggerEvent,
			ruleType: definition.ruleType,
			ruleData: definition.ruleData,
			animationType: definition.animationType,
			animationData: definition.animationData,
			sortOrder: definition.sortOrder,
			isActive: definition.isActive
		};
		
		// Parse animation data
		if (definition.animationData) {
			try {
				const data = JSON.parse(definition.animationData);
				confettiAmount = data.confettiAmount ?? 150;
				iconType = data.iconType ?? 'emoji';
				webmUrl = data.webmUrl ?? '';
				svgPath = data.svgPath ?? '';
			} catch {
				confettiAmount = 150;
				iconType = 'emoji';
				webmUrl = '';
				svgPath = '';
			}
		} else {
			confettiAmount = 150;
			iconType = 'emoji';
			webmUrl = '';
			svgPath = '';
		}
		
		showEditModal = true;
	}

	function openDeleteModal(definition: MilestoneDefinition) {
		selectedDefinition = definition;
		showDeleteModal = true;
	}

	function closeModals() {
		showCreateModal = false;
		showEditModal = false;
		showDeleteModal = false;
		selectedDefinition = null;
	}

	function buildAnimationData(): string | null {
		const data: Record<string, unknown> = {};
		
		if (confettiAmount !== 150) {
			data.confettiAmount = confettiAmount;
		}
		
		if (iconType !== 'emoji') {
			data.iconType = iconType;
			if (iconType === 'webm' && webmUrl) {
				data.webmUrl = webmUrl;
			}
			if (iconType === 'svg' && svgPath) {
				data.svgPath = svgPath;
			}
		}
		
		return Object.keys(data).length > 0 ? JSON.stringify(data) : null;
	}

	async function handleCreate() {
		saving = true;
		error = '';
		try {
			const request: CreateMilestoneRequest = {
				...formData,
				animationData: buildAnimationData()
			};
			await createMilestoneDefinition(request);
			await loadData();
			closeModals();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to create milestone';
		} finally {
			saving = false;
		}
	}

	async function handleUpdate() {
		if (!selectedDefinition) return;
		saving = true;
		error = '';
		try {
			const request: UpdateMilestoneRequest = {
				code: formData.code,
				titleKey: formData.titleKey,
				descriptionKey: formData.descriptionKey,
				icon: formData.icon,
				triggerEvent: formData.triggerEvent,
				ruleType: formData.ruleType,
				ruleData: formData.ruleData,
				animationType: formData.animationType ?? 'confetti',
				animationData: buildAnimationData(),
				sortOrder: formData.sortOrder ?? 0,
				isActive: formData.isActive ?? true
			};
			await updateMilestoneDefinition(selectedDefinition.id, request);
			await loadData();
			closeModals();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to update milestone';
		} finally {
			saving = false;
		}
	}

	async function handleToggle(definition: MilestoneDefinition) {
		try {
			await toggleMilestoneDefinition(definition.id, !definition.isActive);
			await loadData();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to toggle milestone';
		}
	}

	async function handleDelete() {
		if (!selectedDefinition) return;
		saving = true;
		error = '';
		try {
			await deleteMilestoneDefinition(selectedDefinition.id);
			await loadData();
			closeModals();
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to delete milestone';
		} finally {
			saving = false;
		}
	}

	function updateCodeFromTitle() {
		if (formData.code === '' || formData.code === formData.titleKey.replace('milestones.', '').replace('.title', '')) {
			const newCode = formData.titleKey
				.replace('milestones.', '')
				.replace('.title', '');
			formData.code = newCode;
			formData.descriptionKey = `milestones.${newCode}.description`;
		}
	}
</script>

{#if loading}
	<div class="flex justify-center py-12">
		<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
	</div>
{:else if error && !showCreateModal && !showEditModal}
	<div class="card">
		<ErrorState message={error} onRetry={loadData} size="md" />
	</div>
{:else}
	<div class="card p-6">
		<div class="flex items-center justify-between mb-6">
			<h3 class="text-lg font-semibold text-cocoa-800">{$t('admin.milestones.title')}</h3>
			<div class="flex items-center gap-4">
				<span class="text-sm text-cocoa-500">
					{definitions.length} {$t('admin.milestones.definitions')}
				</span>
				<button
					onclick={openCreateModal}
					class="px-4 py-2 bg-primary-600 text-white font-medium rounded-lg hover:bg-primary-700 transition-colors flex items-center gap-2"
				>
					<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
					</svg>
					{$t('admin.milestones.create')}
				</button>
			</div>
		</div>

		<div class="overflow-x-auto">
			<table class="w-full">
				<thead>
					<tr class="border-b border-cocoa-200">
						<th class="text-left py-3 px-4 text-sm font-semibold text-cocoa-600">{$t('admin.milestones.icon')}</th>
						<th class="text-left py-3 px-4 text-sm font-semibold text-cocoa-600">{$t('admin.milestones.code')}</th>
						<th class="text-left py-3 px-4 text-sm font-semibold text-cocoa-600">{$t('admin.milestones.trigger')}</th>
						<th class="text-left py-3 px-4 text-sm font-semibold text-cocoa-600">{$t('admin.milestones.rule')}</th>
						<th class="text-left py-3 px-4 text-sm font-semibold text-cocoa-600">{$t('admin.milestones.status')}</th>
						<th class="text-left py-3 px-4 text-sm font-semibold text-cocoa-600">{$t('admin.milestones.actions')}</th>
					</tr>
				</thead>
				<tbody>
					{#each definitions as definition}
						<tr class="border-b border-cocoa-100 hover:bg-warm-cream/50 transition-colors">
							<td class="py-3 px-4">
								<span class="text-2xl">{definition.icon}</span>
							</td>
							<td class="py-3 px-4">
								<div class="font-medium text-cocoa-800">{definition.code}</div>
								<div class="text-xs text-cocoa-500">{$t(definition.titleKey)}</div>
							</td>
							<td class="py-3 px-4">
								<span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-700">
									{definition.triggerEvent}
								</span>
							</td>
							<td class="py-3 px-4">
								<div class="text-sm text-cocoa-700">{getRuleTypeLabel(definition.ruleType)}</div>
								<div class="text-xs text-cocoa-500">{formatRuleData(definition.ruleData)}</div>
							</td>
							<td class="py-3 px-4">
								<button
									onclick={() => handleToggle(definition)}
									class="relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none {definition.isActive ? 'bg-green-500' : 'bg-cocoa-300'}"
									role="switch"
									aria-checked={definition.isActive}
								>
									<span
										class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out {definition.isActive ? 'translate-x-5' : 'translate-x-0'}"
									></span>
								</button>
							</td>
							<td class="py-3 px-4">
								<div class="flex items-center gap-2">
									<button
										onclick={() => previewMilestone(definition)}
										class="px-3 py-1.5 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg transition-colors"
										title={$t('admin.milestones.preview')}
									>
										<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
										</svg>
									</button>
									<button
										onclick={() => openEditModal(definition)}
										class="px-3 py-1.5 text-sm font-medium text-cocoa-600 hover:bg-cocoa-100 rounded-lg transition-colors"
										title={$t('common.edit')}
									>
										<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
										</svg>
									</button>
									<button
										onclick={() => openDeleteModal(definition)}
										class="px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 rounded-lg transition-colors"
										title={$t('common.delete')}
									>
										<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
										</svg>
									</button>
								</div>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	</div>
{/if}

<!-- Create/Edit Modal -->
{#if showCreateModal || showEditModal}
	<div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" onclick={closeModals}>
		<div class="bg-warm-paper rounded-2xl shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto" onclick={(e) => e.stopPropagation()}>
			<div class="p-6 border-b border-cocoa-200">
				<h2 class="text-xl font-bold text-cocoa-800">
					{showCreateModal ? $t('admin.milestones.create') : $t('admin.milestones.edit')}
				</h2>
			</div>

			<div class="p-6 space-y-4">
				{#if error}
					<div class="p-3 bg-red-50 text-red-700 rounded-lg text-sm">{error}</div>
				{/if}

				<div class="grid grid-cols-2 gap-4">
					<!-- Code -->
					<div>
						<label class="block text-sm font-medium text-cocoa-700 mb-1">Code</label>
						<input
							type="text"
							bind:value={formData.code}
							class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
							placeholder="first_habit"
						/>
					</div>

					<!-- Icon -->
					<div>
						<label class="block text-sm font-medium text-cocoa-700 mb-1">Icon (Emoji)</label>
						<input
							type="text"
							bind:value={formData.icon}
							class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent text-2xl"
							placeholder="🎯"
						/>
					</div>
				</div>

				<div class="grid grid-cols-2 gap-4">
					<!-- Title Key -->
					<div>
						<label class="block text-sm font-medium text-cocoa-700 mb-1">Title Key (i18n)</label>
						<input
							type="text"
							bind:value={formData.titleKey}
							oninput={updateCodeFromTitle}
							class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
							placeholder="milestones.first_habit.title"
						/>
					</div>

					<!-- Description Key -->
					<div>
						<label class="block text-sm font-medium text-cocoa-700 mb-1">Description Key (i18n)</label>
						<input
							type="text"
							bind:value={formData.descriptionKey}
							class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
							placeholder="milestones.first_habit.description"
						/>
					</div>
				</div>

				<div class="grid grid-cols-2 gap-4">
					<!-- Trigger Event -->
					<div>
						<label class="block text-sm font-medium text-cocoa-700 mb-1">Trigger Event</label>
						<select
							bind:value={formData.triggerEvent}
							class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
						>
							{#each triggerEvents as event}
								<option value={event}>{event}</option>
							{/each}
						</select>
					</div>

					<!-- Rule Type -->
					<div>
						<label class="block text-sm font-medium text-cocoa-700 mb-1">Rule Type</label>
						<select
							bind:value={formData.ruleType}
							class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
						>
							{#each ruleTypes as rule}
								<option value={rule.value}>{rule.label}</option>
							{/each}
						</select>
					</div>
				</div>

				<!-- Rule Data -->
				<div>
					<label class="block text-sm font-medium text-cocoa-700 mb-1">Rule Data (JSON)</label>
					<textarea
						bind:value={formData.ruleData}
						rows="3"
						class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent font-mono text-sm"
						placeholder={`{"field": "total_habits_completed", "threshold": 1}`}
					></textarea>
					<p class="text-xs text-cocoa-500 mt-1">
						Fields: {statFields.join(', ')}
					</p>
				</div>

				<!-- Animation Settings -->
				<div class="border-t border-cocoa-200 pt-4">
					<h3 class="text-sm font-semibold text-cocoa-700 mb-3">Animation Settings</h3>
					
					<div class="grid grid-cols-2 gap-4">
						<!-- Confetti Amount -->
						<div>
							<label class="block text-sm font-medium text-cocoa-700 mb-1">Confetti Amount</label>
							<select
								bind:value={confettiAmount}
								class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
							>
								<option value={50}>Minimal (50)</option>
								<option value={150}>Normal (150)</option>
								<option value={300}>Exciting (300)</option>
								<option value={500}>Epic (500)</option>
							</select>
						</div>

						<!-- Icon Type -->
						<div>
							<label class="block text-sm font-medium text-cocoa-700 mb-1">Icon Display Type</label>
							<select
								bind:value={iconType}
								class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
							>
								<option value="emoji">Emoji</option>
								<option value="webm">WebM Video</option>
								<option value="svg">SVG Path</option>
							</select>
						</div>
					</div>

					{#if iconType === 'webm'}
						<div class="mt-4">
							<label class="block text-sm font-medium text-cocoa-700 mb-1">WebM File</label>
							<select
								bind:value={webmUrl}
								class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
							>
								<option value="">Select a WebM file...</option>
								{#each availableWebmFiles as file}
									<option value={file.value}>{file.label} ({file.value})</option>
								{/each}
							</select>
							{#if webmUrl}
								<div class="mt-2 flex items-center gap-3">
									<video
										src={webmUrl}
										autoplay
										loop
										muted
										playsinline
										class="w-16 h-16 object-contain rounded-lg bg-cocoa-100"
									></video>
									<span class="text-sm text-cocoa-500">Preview</span>
								</div>
							{/if}
						</div>
					{/if}

					{#if iconType === 'svg'}
						<div class="mt-4">
							<label class="block text-sm font-medium text-cocoa-700 mb-1">SVG Path</label>
							<textarea
								bind:value={svgPath}
								rows="2"
								class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent font-mono text-sm"
								placeholder="M12 2L15.09 8.26..."
							></textarea>
						</div>
					{/if}
				</div>

				<div class="grid grid-cols-2 gap-4">
					<!-- Sort Order -->
					<div>
						<label class="block text-sm font-medium text-cocoa-700 mb-1">Sort Order</label>
						<input
							type="number"
							bind:value={formData.sortOrder}
							class="w-full px-3 py-2 border border-cocoa-200 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
						/>
					</div>

					<!-- Is Active -->
					<div class="flex items-center gap-3 pt-6">
						<label class="relative inline-flex items-center cursor-pointer">
							<input type="checkbox" bind:checked={formData.isActive} class="sr-only peer" />
							<div class="w-11 h-6 bg-cocoa-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-cocoa-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-500"></div>
							<span class="ml-3 text-sm font-medium text-cocoa-700">Active</span>
						</label>
					</div>
				</div>
			</div>

			<div class="p-6 border-t border-cocoa-200 flex justify-end gap-3">
				<button
					onclick={closeModals}
					class="px-4 py-2 text-cocoa-600 font-medium rounded-lg hover:bg-cocoa-100 transition-colors"
				>
					{$t('common.cancel')}
				</button>
				<button
					onclick={showCreateModal ? handleCreate : handleUpdate}
					disabled={saving}
					class="px-4 py-2 bg-primary-600 text-white font-medium rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
				>
					{#if saving}
						<div class="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full"></div>
					{/if}
					{showCreateModal ? $t('common.create') : $t('common.save')}
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- Delete Confirmation Modal -->
{#if showDeleteModal && selectedDefinition}
	<div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" onclick={closeModals}>
		<div class="bg-warm-paper rounded-2xl shadow-xl max-w-md w-full" onclick={(e) => e.stopPropagation()}>
			<div class="p-6">
				<div class="flex items-center gap-4 mb-4">
					<div class="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center">
						<svg class="w-6 h-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
						</svg>
					</div>
					<div>
						<h3 class="text-lg font-bold text-cocoa-800">{$t('admin.milestones.deleteConfirm')}</h3>
						<p class="text-sm text-cocoa-600">
							This will also delete all user achievements for this milestone.
						</p>
					</div>
				</div>

				<div class="bg-cocoa-50 rounded-lg p-4 mb-6">
					<div class="flex items-center gap-3">
						<span class="text-2xl">{selectedDefinition.icon}</span>
						<div>
							<div class="font-medium text-cocoa-800">{selectedDefinition.code}</div>
							<div class="text-sm text-cocoa-500">{$t(selectedDefinition.titleKey)}</div>
						</div>
					</div>
				</div>

				{#if error}
					<div class="p-3 bg-red-50 text-red-700 rounded-lg text-sm mb-4">{error}</div>
				{/if}

				<div class="flex justify-end gap-3">
					<button
						onclick={closeModals}
						class="px-4 py-2 text-cocoa-600 font-medium rounded-lg hover:bg-cocoa-100 transition-colors"
					>
						{$t('common.cancel')}
					</button>
					<button
						onclick={handleDelete}
						disabled={saving}
						class="px-4 py-2 bg-red-600 text-white font-medium rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
					>
						{#if saving}
							<div class="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full"></div>
						{/if}
						{$t('common.delete')}
					</button>
				</div>
			</div>
		</div>
	</div>
{/if}
