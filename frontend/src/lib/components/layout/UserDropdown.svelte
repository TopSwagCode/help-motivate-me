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

	function handleAdminClick() {
		isOpen = false;
		goto('/admin');
	}

	const isAdmin = $derived($auth.user?.role === 'Admin');
</script>

<svelte:window onclick={handleClickOutside} />

<div class="user-dropdown relative">
	<button
		onclick={toggleDropdown}
		class="flex items-center gap-2 text-sm text-cocoa-600 hover:text-cocoa-800 px-2 py-1 rounded hover:bg-primary-50"
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
			class="absolute right-0 mt-2 w-48 bg-warm-paper rounded-2xl shadow-lg border border-primary-100 py-1 z-50"
		>
			{#if isAdmin}
				<button
					onclick={handleAdminClick}
					class="w-full text-left px-4 py-2 text-sm text-purple-700 hover:bg-purple-50 flex items-center gap-2"
				>
					<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
						/>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
						/>
					</svg>
					Admin Dashboard
				</button>
				<hr class="my-1 border-primary-100" />
			{/if}
			<button
				onclick={handleSettingsClick}
				class="w-full text-left px-4 py-2 text-sm text-cocoa-700 hover:bg-primary-50"
			>
				Settings
			</button>
			<hr class="my-1 border-primary-100" />
			<button
				onclick={handleLogout}
				class="w-full text-left px-4 py-2 text-sm text-cocoa-700 hover:bg-primary-50"
			>
				Sign out
			</button>
		</div>
	{/if}
</div>
