<script lang="ts">
	import { page } from '$app/stores';
	import { t } from 'svelte-i18n';

	// Helper to check if route is active
	function isActive(path: string): boolean {
		const currentPath = $page.url.pathname;
		return currentPath.startsWith(path);
	}
</script>

<!-- Floating Bottom Navigation Bar -->
<nav
	class="fixed left-1/2 -translate-x-1/2 w-[calc(100%-2rem)] md:w-auto z-40 rounded-2xl shadow-lg shadow-primary-500/20 px-2 py-2 md:px-4"
	style="background: linear-gradient(135deg, #fdfcfa 0%, #faf7f2 50%, #f9e8d2 100%); bottom: max(1rem, env(safe-area-inset-bottom));"
>
	<div class="flex items-center justify-around md:justify-center md:gap-2">
		<!-- Today -->
		<a
			href="/today"
			class="flex flex-col items-center gap-0.5 px-2 py-1.5 rounded-2xl transition-all duration-300 min-w-[48px] {isActive('/today')
				? 'bg-primary-500 text-white'
				: 'text-cocoa-600 hover:text-cocoa-800'}"
		>
			<svg class="w-5 h-5" fill={isActive('/today') ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24">
				{#if isActive('/today')}
					<path fill-rule="evenodd" clip-rule="evenodd" d="M6 2a1 1 0 011 1v1h10V3a1 1 0 112 0v1h1a3 3 0 013 3v12a3 3 0 01-3 3H4a3 3 0 01-3-3V7a3 3 0 013-3h1V3a1 1 0 011-1zm-2 8v9a1 1 0 001 1h14a1 1 0 001-1v-9H4z" />
				{:else}
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
				{/if}
			</svg>
			<span class="text-[10px] font-medium">{$t('nav.today')}</span>
		</a>

		<!-- Goals -->
		<a
			href="/goals"
			class="flex flex-col items-center gap-0.5 px-2 py-1.5 rounded-2xl transition-all duration-300 min-w-[48px] {isActive('/goals')
				? 'bg-primary-500 text-white'
				: 'text-cocoa-600 hover:text-cocoa-800'}"
		>
			<svg class="w-5 h-5" fill={isActive('/goals') ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24">
				{#if isActive('/goals')}
					<path fill-rule="evenodd" clip-rule="evenodd" d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
				{:else}
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
				{/if}
			</svg>
			<span class="text-[10px] font-medium">{$t('nav.goals')}</span>
		</a>

		<!-- Habit Stacks -->
		<a
			href="/habit-stacks"
			class="flex flex-col items-center gap-0.5 px-2 py-1.5 rounded-2xl transition-all duration-300 min-w-[48px] {isActive('/habit-stacks')
				? 'bg-primary-500 text-white'
				: 'text-cocoa-600 hover:text-cocoa-800'}"
		>
			<svg class="w-5 h-5" fill={isActive('/habit-stacks') ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24">
				{#if isActive('/habit-stacks')}
					<path fill-rule="evenodd" clip-rule="evenodd" d="M3 5a1 1 0 011-1h16a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h16a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h16a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h16a1 1 0 110 2H4a1 1 0 01-1-1z" />
				{:else}
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
				{/if}
			</svg>
			<span class="text-[10px] font-medium">{$t('nav.stacks')}</span>
		</a>

		<!-- Journal -->
		<a
			href="/journal"
			class="flex flex-col items-center gap-0.5 px-2 py-1.5 rounded-2xl transition-all duration-300 min-w-[48px] {isActive('/journal')
				? 'bg-primary-500 text-white'
				: 'text-cocoa-600 hover:text-cocoa-800'}"
		>
			<svg class="w-5 h-5" fill={isActive('/journal') ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24">
				{#if isActive('/journal')}
					<path fill-rule="evenodd" clip-rule="evenodd" d="M4 4a2 2 0 012-2h8a2 2 0 012 2v1h2a2 2 0 012 2v13a2 2 0 01-2 2H6a2 2 0 01-2-2V4zm10 0H6v16h12V7h-2a2 2 0 01-2-2V4z" />
				{:else}
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
				{/if}
			</svg>
			<span class="text-[10px] font-medium">{$t('nav.journal')}</span>
		</a>

		<!-- Identities -->
		<a
			href="/identities"
			class="flex flex-col items-center gap-0.5 px-2 py-1.5 rounded-2xl transition-all duration-300 min-w-[48px] {isActive('/identities')
				? 'bg-primary-500 text-white'
				: 'text-cocoa-600 hover:text-cocoa-800'}"
		>
			<svg class="w-5 h-5" fill={isActive('/identities') ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24">
				{#if isActive('/identities')}
					<path fill-rule="evenodd" clip-rule="evenodd" d="M12 2a5 5 0 100 10 5 5 0 000-10zM4 19a8 8 0 1116 0H4z" />
				{:else}
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
				{/if}
			</svg>
			<span class="text-[10px] font-medium">{$t('nav.identities')}</span>
		</a>

		<!-- Buddies -->
		<a
			href="/buddies"
			class="flex flex-col items-center gap-0.5 px-2 py-1.5 rounded-2xl transition-all duration-300 min-w-[48px] {isActive('/buddies')
				? 'bg-primary-500 text-white'
				: 'text-cocoa-600 hover:text-cocoa-800'}"
		>
			<svg class="w-5 h-5" fill={isActive('/buddies') ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24">
				{#if isActive('/buddies')}
					<path fill-rule="evenodd" clip-rule="evenodd" d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3zM6 8a2 2 0 11-4 0 2 2 0 014 0zM16 18v-3a5.97 5.97 0 00-.75-2.906A3.005 3.005 0 0119 15v3h-3zM4.75 12.094A5.97 5.97 0 004 15v3H1v-3a3 3 0 013.75-2.906z" />
				{:else}
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
				{/if}
			</svg>
			<span class="text-[10px] font-medium">{$t('nav.buddies')}</span>
		</a>
	</div>
</nav>
