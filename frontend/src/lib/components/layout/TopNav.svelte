<script lang="ts">
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import UserDropdown from './UserDropdown.svelte';
</script>

<header
	class="sticky top-0 z-50 rounded-b-3xl shadow-lg shadow-primary-500/10 overflow-hidden"
	style="background: linear-gradient(135deg, #fdfcfa 0%, #faf7f2 50%, #f9e8d2 100%);"
>
	<div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8">
		<div class="flex justify-between items-center h-12 sm:h-14">
			<!-- Logo with Mascot -->
			<a href="/today" class="flex items-end gap-1 sm:gap-2 text-base sm:text-lg font-bold text-primary-600 flex-shrink-0 self-end">
				<!-- Sloth mascot - peeking over the header like a countertop -->
				<img 
					src="/sloth.png" 
					alt="Sloth mascot" 
					class="w-10 sm:w-14 h-auto flex-shrink-0 -mb-4 sm:-mb-6"
				/>
				<span class="hidden xs:inline pb-2 sm:pb-3">HelpMotivateMe</span>
				<span class="xs:hidden pb-2 sm:pb-3">HMM</span>
			</a>

			<!-- Right Side: User Menu -->
			<UserDropdown />
		</div>
	</div>
</header>
