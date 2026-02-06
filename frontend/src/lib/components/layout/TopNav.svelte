<script lang="ts">
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import UserDropdown from './UserDropdown.svelte';

	interface Props {
		hideUserMenu?: boolean;
		onHelpClick?: () => void;
	}

	let { hideUserMenu = false, onHelpClick }: Props = $props();
</script>

<header
	class="sticky top-0 z-50 rounded-b-3xl shadow-lg shadow-primary-500/10"
	style="background: linear-gradient(135deg, #fdfcfa 0%, #faf7f2 50%, #f9e8d2 100%);"
>
	<div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8">
		<div class="flex justify-between items-center h-12 sm:h-14">
			<!-- Logo with Mascot -->
			<a href="/today" class="flex items-center gap-2 sm:gap-3 text-base sm:text-lg font-bold text-primary-600 flex-shrink-0 ml-4 sm:ml-6">
				<!-- Sloth mascot - overlapping the app bar edge -->
				<div class="w-20 h-20 sm:w-24 sm:h-24 flex-shrink-0 relative">
					<img
						src="/appbarsloth.png"
						alt="Sloth mascot"
						class="w-full h-auto object-contain absolute bottom-[14px] sm:bottom-[16px]"
					/>
				</div>
				<span>HelpMotivateMe</span>
			</a>

			<!-- Right Side: User Menu (hidden during onboarding) -->
			{#if !hideUserMenu}
				<UserDropdown {onHelpClick} />
			{/if}
		</div>
	</div>
</header>
