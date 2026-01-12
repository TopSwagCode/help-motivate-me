<script lang="ts">
	import { browser } from '$app/environment';
	
	// Check if user has dismissed the banner (stored in localStorage)
	let dismissed = $state(false);
	
	$effect(() => {
		if (browser) {
			dismissed = localStorage.getItem('betaBannerDismissed') === 'true';
		}
	});
	
	function dismissBanner() {
		dismissed = true;
		if (browser) {
			localStorage.setItem('betaBannerDismissed', 'true');
		}
	}
</script>

{#if !dismissed}
	<div class="bg-gradient-to-r from-amber-500 to-orange-500 text-white">
		<div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8">
			<div class="flex flex-col sm:flex-row items-center justify-between gap-2 py-2.5 sm:py-2">
				<div class="flex items-center gap-2 text-center sm:text-left">
					<div class="hidden sm:flex items-center justify-center w-6 h-6 bg-white/20 rounded-full flex-shrink-0">
						<svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
							<path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
						</svg>
					</div>
					<div>
						<span class="font-semibold text-sm sm:text-base">🚧 Beta Version</span>
						<span class="hidden sm:inline text-sm sm:text-base mx-2">•</span>
						<span class="text-xs sm:text-sm text-white/95 block sm:inline mt-0.5 sm:mt-0">
							This app is in beta testing. Data may be reset without notice.
						</span>
					</div>
				</div>
				<button
					onclick={dismissBanner}
					class="flex items-center gap-1.5 px-3 py-1.5 text-xs sm:text-sm font-medium bg-white/10 hover:bg-white/20 
					       rounded-lg transition-colors duration-200 flex-shrink-0 whitespace-nowrap"
					aria-label="Dismiss banner"
				>
					<span>Got it</span>
					<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					</svg>
				</button>
			</div>
		</div>
	</div>
{/if}
