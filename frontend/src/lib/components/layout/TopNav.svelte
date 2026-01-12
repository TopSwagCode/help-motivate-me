<script lang="ts">
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { t } from 'svelte-i18n';
	import UserDropdown from './UserDropdown.svelte';

	// Helper to check if route is active
	function isActive(path: string): boolean {
		const currentPath = $page.url.pathname;
		return currentPath.startsWith(path);
	}
</script>

<header class="bg-white border-b border-gray-200 sticky top-0 z-50">
	<div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
		<div class="flex justify-between items-center h-14">
			<!-- Logo -->
			<a href="/today" class="text-lg font-bold text-primary-600 flex-shrink-0">
				HelpMotivateMe
			</a>

			<!-- Navigation Links -->
			<nav class="hidden sm:flex items-center gap-1">
				<a
					href="/today"
					class="px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive('/today')
						? 'bg-primary-100 text-primary-700'
						: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
				>
					{$t('nav.today')}
				</a>
				<a
					href="/goals"
					class="px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive('/goals')
						? 'bg-primary-100 text-primary-700'
						: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
				>
					{$t('nav.goals')}
				</a>
				<a
					href="/habit-stacks"
					class="px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive('/habit-stacks')
						? 'bg-primary-100 text-primary-700'
						: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
				>
					{$t('nav.habitStacks')}
				</a>
				<a
					href="/journal"
					class="px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive('/journal')
						? 'bg-primary-100 text-primary-700'
						: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
				>
					{$t('nav.journal')}
				</a>
				<a
					href="/identities"
					class="px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive('/identities')
						? 'bg-primary-100 text-primary-700'
						: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
				>
					{$t('nav.identities')}
				</a>
				<!--<a
					href="/analytics"
					class="px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive('/analytics')
						? 'bg-primary-100 text-primary-700'
						: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
				>
					{$t('nav.analytics')}
				</a>-->
				<a
					href="/buddies"
					class="px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive('/buddies')
						? 'bg-primary-100 text-primary-700'
						: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
				>
					{$t('nav.buddies')}
				</a>
			</nav>

			<!-- User Menu -->
			<div class="flex items-center gap-3">
				<UserDropdown />
			</div>
		</div>

		<!-- Mobile Navigation -->
		<nav class="sm:hidden flex items-center gap-1 pb-2 overflow-x-auto">
			<a
				href="/today"
				class="px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors {isActive('/today')
					? 'bg-primary-100 text-primary-700'
					: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
			>
				{$t('nav.today')}
			</a>
			<a
				href="/goals"
				class="px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors {isActive('/goals')
					? 'bg-primary-100 text-primary-700'
					: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
			>
				{$t('nav.goals')}
			</a>
			<a
				href="/habit-stacks"
				class="px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors {isActive('/habit-stacks')
					? 'bg-primary-100 text-primary-700'
					: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
			>
				{$t('nav.habitStacks')}
			</a>
			<a
				href="/journal"
				class="px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors {isActive('/journal')
					? 'bg-primary-100 text-primary-700'
					: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
			>
				{$t('nav.journal')}
			</a>
			<a
				href="/identities"
				class="px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors {isActive('/identities')
					? 'bg-primary-100 text-primary-700'
					: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
			>
				{$t('nav.identities')}
			</a>
			<a
				href="/analytics"
				class="px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors {isActive('/analytics')
					? 'bg-primary-100 text-primary-700'
					: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
			>
				{$t('nav.analytics')}
			</a>
			<a
				href="/buddies"
				class="px-3 py-1.5 rounded-lg text-sm font-medium whitespace-nowrap transition-colors {isActive('/buddies')
					? 'bg-primary-100 text-primary-700'
					: 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}"
			>
				{$t('nav.buddies')}
			</a>
		</nav>
	</div>
</header>
