<script lang="ts">
	import { t } from 'svelte-i18n';

	interface Props {
		oddWeekDays?: number;
		evenWeekDays?: number;
		disabled?: boolean;
	}

	let {
		oddWeekDays = $bindable(127),
		evenWeekDays = $bindable(127),
		disabled = false
	}: Props = $props();
	let usesTwoWeekPattern = $state(oddWeekDays !== evenWeekDays);

	const days = [
		{ value: 1, key: 'monday' },
		{ value: 2, key: 'tuesday' },
		{ value: 4, key: 'wednesday' },
		{ value: 8, key: 'thursday' },
		{ value: 16, key: 'friday' },
		{ value: 32, key: 'saturday' },
		{ value: 64, key: 'sunday' }
	];

	function setTwoWeekPattern(enabled: boolean) {
		usesTwoWeekPattern = enabled;
		if (!enabled) evenWeekDays = oddWeekDays || evenWeekDays;
		oddWeekDays ||= evenWeekDays;
	}

	function toggleDay(day: number, week: 'odd' | 'even') {
		if (!usesTwoWeekPattern) {
			const nextDays = oddWeekDays ^ day;
			if (nextDays !== 0) oddWeekDays = evenWeekDays = nextDays;
			return;
		}

		const nextDays = (week === 'odd' ? oddWeekDays : evenWeekDays) ^ day;
		if (nextDays === 0 && (week === 'odd' ? evenWeekDays : oddWeekDays) === 0) return;
		if (week === 'odd') oddWeekDays = nextDays;
		else evenWeekDays = nextDays;
	}
</script>

<fieldset class="space-y-3" {disabled}>
	<legend class="text-sm font-medium text-cocoa-700">{$t('habitStacks.schedule.title')}</legend>
	<p class="text-xs text-cocoa-500">{$t('habitStacks.schedule.description')}</p>

	<div class="grid grid-cols-2 gap-1 rounded-lg bg-gray-100 p-1" aria-label={$t('habitStacks.schedule.pattern')}>
		<button
			type="button"
			onclick={() => setTwoWeekPattern(false)}
			aria-pressed={!usesTwoWeekPattern}
			class="min-h-9 rounded-md px-2 py-1.5 text-xs font-medium transition-colors {!usesTwoWeekPattern ? 'bg-warm-paper text-cocoa-800 shadow-sm' : 'text-cocoa-500 hover:text-cocoa-700'}"
		>
			{$t('habitStacks.schedule.everyWeek')}
		</button>
		<button
			type="button"
			onclick={() => setTwoWeekPattern(true)}
			aria-pressed={usesTwoWeekPattern}
			class="min-h-9 rounded-md px-2 py-1.5 text-xs font-medium transition-colors {usesTwoWeekPattern ? 'bg-warm-paper text-cocoa-800 shadow-sm' : 'text-cocoa-500 hover:text-cocoa-700'}"
		>
			{$t('habitStacks.schedule.twoWeekPattern')}
		</button>
	</div>

	{#each usesTwoWeekPattern ? [{ key: 'oddWeeks', week: 'odd' as const, value: oddWeekDays }, { key: 'evenWeeks', week: 'even' as const, value: evenWeekDays }] : [{ key: 'everyWeek', week: 'odd' as const, value: oddWeekDays }] as row}
		<div class="space-y-1.5">
			{#if usesTwoWeekPattern}<p class="text-xs font-medium text-cocoa-600">{$t(`habitStacks.schedule.${row.key}`)}</p>{/if}
			<div class="grid grid-cols-7 gap-1" aria-label={$t(`habitStacks.schedule.${row.key}`)}>
			{#each days as day}
			<button
				type="button"
				onclick={() => toggleDay(day.value, row.week)}
				aria-pressed={(row.value & day.value) !== 0}
				title={$t(`habitStacks.schedule.${day.key}`)}
				class="h-9 rounded-lg border text-xs font-semibold transition-colors {(row.value & day.value) !== 0
					? 'border-primary-600 bg-primary-600 text-white'
					: 'border-gray-200 bg-warm-paper text-cocoa-600 hover:border-primary-300'}"
			>
				{$t(`habitStacks.schedule.${day.key}Short`)}
			</button>
		{/each}
			</div>
	</div>
	{/each}

	{#if usesTwoWeekPattern}
		<p class="text-xs text-cocoa-500">{$t('habitStacks.schedule.isoWeekHint')}</p>
	{/if}
</fieldset>
