<script lang="ts">
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import UserDropdown from './UserDropdown.svelte';
</script>

<header
	class="sticky top-0 z-50 rounded-b-3xl shadow-lg shadow-primary-500/10"
	style="background: linear-gradient(135deg, #fdfcfa 0%, #faf7f2 50%, #f9e8d2 100%);"
>
	<div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8">
		<div class="flex justify-between items-center h-12 sm:h-14">
			<!-- Logo with Mascot -->
			<a href="/today" class="flex items-center gap-1 sm:gap-2 text-base sm:text-lg font-bold text-primary-600 flex-shrink-0">
				<!-- Sloth mascot - showing top 50% -->
				<div class="w-6 h-6 sm:w-8 sm:h-8 overflow-hidden flex-shrink-0">
					<img 
						src="/sloth.png" 
						alt="Sloth mascot" 
						class="w-full h-auto object-cover object-top"
						style="transform: scale(1.0);"
					/>
				</div>
				<span class="hidden xs:inline">HelpMotivateMe</span>
				<span class="xs:hidden">HMM</span>
			</a>

			<!-- Right Side: User Menu -->
			<UserDropdown />
		</div>
	</div>
</header>
