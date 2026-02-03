<script lang="ts">
	import { t } from 'svelte-i18n';
	import type { MilestoneDefinition, UserMilestone } from '$lib/types/milestone';
	import { getMilestoneDefinitions } from '$lib/api/milestones';
	import { milestoneStore } from '$lib/stores/milestones';
	import ErrorState from '$lib/components/shared/ErrorState.svelte';

	let definitions = $state<MilestoneDefinition[]>([]);
	let loading = $state(true);
	let error = $state('');

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
		// Create a fake UserMilestone for preview
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
		switch (ruleType) {
			case 'count': return 'Count';
			case 'window_count': return 'Window Count';
			case 'return_after_gap': return 'Return After Gap';
			default: return ruleType;
		}
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
</script>

{#if loading}
	<div class="flex justify-center py-12">
		<div class="animate-spin w-8 h-8 border-4 border-primary-600 border-t-transparent rounded-full"></div>
	</div>
{:else if error}
	<div class="card">
		<ErrorState message={error} onRetry={loadData} size="md" />
	</div>
{:else}
	<div class="card p-6">
		<div class="flex items-center justify-between mb-6">
			<h3 class="text-lg font-semibold text-cocoa-800">{$t('admin.milestones.title')}</h3>
			<span class="text-sm text-cocoa-500">
				{definitions.length} {$t('admin.milestones.definitions')}
			</span>
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
								{#if definition.isActive}
									<span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
										{$t('admin.milestones.active')}
									</span>
								{:else}
									<span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-cocoa-100 text-cocoa-600">
										{$t('admin.milestones.inactive')}
									</span>
								{/if}
							</td>
							<td class="py-3 px-4">
								<button
									onclick={() => previewMilestone(definition)}
									class="px-3 py-1.5 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg transition-colors"
								>
									{$t('admin.milestones.preview')}
								</button>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	</div>
{/if}
