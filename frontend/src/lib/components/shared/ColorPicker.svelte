<script lang="ts">
	interface Props {
		value: string;
		onchange: (color: string) => void;
	}

	let { value, onchange }: Props = $props();

	let showCustomPicker = $state(false);
	let customColor = $state('#6366f1');

	// Theme-appropriate default colors
	const defaultColors = [
		{ color: '#6366f1', name: 'Indigo' },      // Primary brand color
		{ color: '#8b5cf6', name: 'Purple' },      // Creative/Spiritual
		{ color: '#ec4899', name: 'Pink' },        // Passion/Love
		{ color: '#ef4444', name: 'Red' },         // Energy/Power
		{ color: '#f59e0b', name: 'Amber' },       // Warmth/Optimism
		{ color: '#10b981', name: 'Emerald' },     // Growth/Health
		{ color: '#06b6d4', name: 'Cyan' },        // Calm/Focus
		{ color: '#3b82f6', name: 'Blue' },        // Trust/Productivity
		{ color: '#64748b', name: 'Slate' },       // Neutral/Professional
		{ color: '#78716c', name: 'Stone' }        // Grounded/Stable
	];

	function selectColor(color: string) {
		onchange(color);
		customColor = color;
	}

	function openCustomPicker() {
		customColor = value || '#6366f1';
		showCustomPicker = true;
	}

	function closeCustomPicker() {
		showCustomPicker = false;
	}

	function applyCustomColor() {
		onchange(customColor);
		showCustomPicker = false;
	}

	// Update customColor when value changes externally
	$effect(() => {
		if (value && !showCustomPicker) {
			customColor = value;
		}
	});
</script>

<div class="relative">
	<div class="label mb-2">Color</div>
	
	<!-- Default color swatches -->
	<div class="flex flex-wrap gap-2">
		{#each defaultColors as { color, name }}
			<button
				type="button"
				class="w-10 h-10 rounded-lg border-2 transition-all {value === color
					? 'border-gray-900 scale-110'
					: 'border-gray-200 hover:border-gray-300 hover:scale-105'}"
				style="background-color: {color};"
				title={name}
				onclick={() => selectColor(color)}
			></button>
		{/each}
		
		<!-- Custom color button -->
		<button
			type="button"
			class="w-10 h-10 rounded-lg border-2 border-gray-200 hover:border-gray-300 transition-colors flex items-center justify-center font-bold text-gray-500 hover:text-gray-700 bg-white"
			onclick={openCustomPicker}
			title="Choose custom color"
		>
			+
		</button>
	</div>

	<!-- Selected color indicator -->
	{#if value && !defaultColors.some(c => c.color === value)}
		<div class="mt-2 flex items-center gap-2 text-sm text-gray-600">
			<div class="w-4 h-4 rounded border border-gray-300" style="background-color: {value};"></div>
			<span>Custom color: {value}</span>
		</div>
	{/if}
</div>

<!-- Custom color picker overlay -->
{#if showCustomPicker}
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<div class="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4" onclick={closeCustomPicker}>
		<!-- svelte-ignore a11y_click_events_have_key_events -->
		<!-- svelte-ignore a11y_no_static_element_interactions -->
		<div class="bg-white rounded-xl shadow-xl max-w-md w-full" onclick={(e) => e.stopPropagation()}>
			<!-- Header -->
			<div class="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
				<h3 class="text-lg font-semibold text-gray-900">Choose Custom Color</h3>
				<button
					type="button"
					onclick={closeCustomPicker}
					class="text-gray-400 hover:text-gray-500 transition-colors"
					aria-label="Close color picker"
				>
					<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					</svg>
				</button>
			</div>

			<!-- Color picker content -->
			<div class="px-6 py-6 space-y-4">
				<!-- Large color input -->
				<div class="flex items-center justify-center">
					<input
						type="color"
						bind:value={customColor}
						class="w-32 h-32 rounded-lg cursor-pointer border-2 border-gray-200"
					/>
				</div>

				<!-- Hex value input -->
				<div>
					<label for="hex-input" class="label">Hex Color Code</label>
					<input
						id="hex-input"
						type="text"
						bind:value={customColor}
						placeholder="#6366f1"
						maxlength="7"
						pattern="^#[0-9A-Fa-f]{6}$"
						class="input font-mono"
					/>
				</div>

				<!-- Preview -->
				<div class="flex items-center gap-3 p-4 bg-gray-50 rounded-lg">
					<div class="w-12 h-12 rounded-lg border-2 border-gray-300" style="background-color: {customColor};"></div>
					<div class="flex-1">
						<div class="text-sm font-medium text-gray-700">Preview</div>
						<div class="text-xs text-gray-500 font-mono">{customColor}</div>
					</div>
				</div>
			</div>

			<!-- Footer -->
			<div class="px-6 py-4 border-t border-gray-200 flex gap-3 justify-end">
				<button type="button" onclick={closeCustomPicker} class="btn-secondary">
					Cancel
				</button>
				<button type="button" onclick={applyCustomColor} class="btn-primary">
					Apply Color
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	/* Ensure overlay appears above everything */
	:global(body:has(.fixed.z-50)) {
		overflow: hidden;
	}
	
	/* Custom styling for color input */
	input[type="color"] {
		-webkit-appearance: none;
		-moz-appearance: none;
		appearance: none;
		background-color: transparent;
		border: none;
		cursor: pointer;
	}
	
	input[type="color"]::-webkit-color-swatch-wrapper {
		padding: 0;
		border-radius: 0.5rem;
	}
	
	input[type="color"]::-webkit-color-swatch {
		border: none;
		border-radius: 0.5rem;
	}
	
	input[type="color"]::-moz-color-swatch {
		border: none;
		border-radius: 0.5rem;
	}
</style>
