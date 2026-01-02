<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

	let isOpen = $state(false);

	function handleLogout() {
		auth.logout();
		goto('/');
	}

	function handleClickOutside(event: MouseEvent) {
		const target = event.target as HTMLElement;
		if (!target.closest('.user-dropdown')) {
			isOpen = false;
		}
	}

	function toggleDropdown() {
		isOpen = !isOpen;
	}

	function handleSettingsClick() {
		isOpen = false;
		goto('/settings');
	}
</script>

<svelte:window onclick={handleClickOutside} />

<div class="user-dropdown relative">
	<button
		onclick={toggleDropdown}
		class="flex items-center gap-2 text-sm text-gray-600 hover:text-gray-900 px-2 py-1 rounded hover:bg-gray-100"
	>
		<span class="hidden sm:block">
			{$auth.user?.displayName || $auth.user?.username}
		</span>
		<svg
			class="w-4 h-4 transition-transform {isOpen ? 'rotate-180' : ''}"
			fill="none"
			stroke="currentColor"
			viewBox="0 0 24 24"
		>
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
		</svg>
	</button>

	{#if isOpen}
		<div
			class="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50"
		>
			<button
				onclick={handleSettingsClick}
				class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
			>
				Settings
			</button>
			<hr class="my-1 border-gray-200" />
			<button
				onclick={handleLogout}
				class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
			>
				Sign out
			</button>
		</div>
	{/if}
</div>
