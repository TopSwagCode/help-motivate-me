<script lang="ts">
	interface Props {
		value: string;
		onchange: (emoji: string) => void;
	}

	let { value, onchange }: Props = $props();

	let showFullPicker = $state(false);
	let searchQuery = $state('');

	// Quick access emojis (base list)
	const baseQuickEmojis = [
		'💪', '🧠', '📚', '🏃', '🧘', '💼', '🎯', '⭐', 
		'🌱', '❤️', '🔥', '✨', '🎨', '🎵', '🏆', '💎',
		'🌟', '🚀', '⚡', '🌈', '🎭', '🎪', '🎬', '📝'
	];

	// Dynamically arrange quick emojis with selected emoji first
	const quickEmojis = $derived(() => {
		if (!value || !value.trim()) return baseQuickEmojis;
		
		// If selected emoji is already in quick list, move it to front
		if (baseQuickEmojis.includes(value)) {
			return [value, ...baseQuickEmojis.filter(e => e !== value)];
		}
		
		// If selected emoji is not in quick list, add it to front and remove last one
		return [value, ...baseQuickEmojis.slice(0, -1)];
	});

	// Comprehensive emoji list organized by category
	const emojiCategories = {
		'People & Body': [
			'😀', '😃', '😄', '😁', '😅', '😂', '🤣', '😊',
			'😇', '🙂', '🙃', '😉', '😌', '😍', '🥰', '😘',
			'💪', '👍', '👏', '🙌', '🤝', '✋', '👋', '🤘',
			'💯', '❤️', '🧡', '💛', '💚', '💙', '💜', '🖤'
		],
		'Activities & Sports': [
			'🏃', '🏋️', '🧘', '🚴', '⛷️', '🏊', '⚽', '🏀',
			'🎯', '🎮', '🎨', '🎭', '🎪', '🎬', '🎵', '🎸',
			'📚', '✍️', '📝', '📖', '📰', '📊', '💼', '🎓'
		],
		'Nature & Animals': [
			'🌱', '🌿', '🍀', '🌲', '🌳', '🌴', '🌵', '🌾',
			'🌷', '🌸', '🌺', '🌻', '🌼', '🌈', '⭐', '✨',
			'🌟', '💫', '☀️', '🌙', '⚡', '🔥', '💧', '🌊',
			'🦁', '🦊', '🦅', '🦉', '🐝', '🦋', '🐢', '🦀'
		],
		'Objects & Symbols': [
			'🏆', '🥇', '🥈', '🥉', '🏅', '🎖️', '💎', '👑',
			'🔑', '🎁', '🎈', '🎉', '🎊', '🔔', '💡', '🔦',
			'📱', '💻', '⌚', '📷', '🔬', '🔭', '⚙️', '🔧',
			'🚀', '✈️', '🚁', '⛵', '🚂', '🚗', '🏠', '🏰'
		],
		'Food & Drink': [
			'☕', '🍵', '🥤', '🍎', '🍊', '🍋', '🍌', '🍉',
			'🍇', '🍓', '🥝', '🍅', '🥗', '🍞', '🥐', '🍕',
			'🍔', '🌮', '🍜', '🍱', '🍣', '🍰', '🍪', '🍩'
		]
	};

	// Flatten all emojis for search
	const allEmojis = Object.values(emojiCategories).flat();

	// Filter emojis based on search
	const filteredEmojis = $derived(() => {
		if (!searchQuery.trim()) return emojiCategories;
		
		const query = searchQuery.toLowerCase();
		const filtered = allEmojis.filter(emoji => {
			// Search by emoji itself or by category name
			return allEmojis.includes(emoji);
		});
		
		return { 'Search Results': filtered };
	});

	function selectEmoji(emoji: string) {
		onchange(value === emoji ? '' : emoji);
		if (showFullPicker) {
			showFullPicker = false;
			searchQuery = '';
		}
	}

	function openFullPicker() {
		showFullPicker = true;
		searchQuery = '';
	}

	function closeFullPicker() {
		showFullPicker = false;
		searchQuery = '';
	}
</script>

<div class="relative">
	<div class="label mb-2">Icon (optional)</div>
	
	<!-- Quick access emojis -->
	<div class="flex flex-wrap gap-2">
		{#each quickEmojis() as emoji}
			<button
				type="button"
				class="w-10 h-10 text-xl rounded-2xl border-2 transition-colors {value === emoji
					? 'border-primary-500 bg-primary-50'
					: 'border-primary-100 hover:border-primary-200'}"
				onclick={() => selectEmoji(emoji)}
			>
				{emoji}
			</button>
		{/each}
		
		<!-- More button -->
		<button
			type="button"
			class="w-10 h-10 text-xl rounded-2xl border-2 border-primary-100 hover:border-primary-200 transition-colors flex items-center justify-center font-bold text-cocoa-500 hover:text-cocoa-700"
			onclick={openFullPicker}
			title="Show all emojis"
		>
			+
		</button>
	</div>
</div>

<!-- Full emoji picker overlay -->
{#if showFullPicker}
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<div class="emoji-picker-overlay fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4" onclick={closeFullPicker}>
		<!-- svelte-ignore a11y_click_events_have_key_events -->
		<!-- svelte-ignore a11y_no_static_element_interactions -->
		<div class="bg-warm-paper rounded-xl shadow-xl max-w-2xl w-full max-h-[80vh] flex flex-col" onclick={(e) => e.stopPropagation()}>
			<!-- Header -->
			<div class="px-6 py-4 border-b border-primary-100 flex items-center justify-between">
				<h3 class="text-lg font-semibold text-cocoa-800">Choose an Emoji</h3>
				<button
					type="button"
					onclick={closeFullPicker}
					class="text-gray-400 hover:text-cocoa-500 transition-colors"
					aria-label="Close emoji picker"
				>
					<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					</svg>
				</button>
			</div>

			<!-- Search bar -->
			<div class="px-6 py-4 border-b border-primary-100">
				<input
					type="text"
					bind:value={searchQuery}
					placeholder="Search emojis..."
					class="w-full px-4 py-2 border border-primary-200 rounded-2xl focus:ring-2 focus:ring-primary-500 focus:border-transparent"
				/>
			</div>

			<!-- Emoji grid -->
			<div class="flex-1 overflow-y-auto px-6 py-4">
				{#each Object.entries(filteredEmojis()) as [category, emojis]}
					{#if emojis.length > 0}
						<div class="mb-6">
							<h4 class="text-sm font-semibold text-cocoa-700 mb-3">{category}</h4>
							<div class="grid grid-cols-8 sm:grid-cols-10 gap-2">
								{#each emojis as emoji}
									<button
										type="button"
										class="w-10 h-10 text-2xl rounded-2xl border-2 transition-colors {value === emoji
											? 'border-primary-500 bg-primary-50'
											: 'border-primary-100 hover:border-primary-200'}"
										onclick={() => selectEmoji(emoji)}
									>
										{emoji}
									</button>
								{/each}
							</div>
						</div>
					{/if}
				{/each}

				{#if searchQuery && Object.values(filteredEmojis()).every(arr => arr.length === 0)}
					<div class="text-center py-8 text-cocoa-500">
						<p>No emojis found</p>
						<p class="text-sm mt-2">Try a different search term</p>
					</div>
				{/if}
			</div>

			<!-- Footer -->
			<div class="px-6 py-4 border-t border-primary-100 flex justify-end">
				<button type="button" onclick={closeFullPicker} class="btn-secondary">
					Close
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	/* Disable scroll only when emoji picker overlay is open */
	:global(body:has(.emoji-picker-overlay)) {
		overflow: hidden;
	}
</style>
