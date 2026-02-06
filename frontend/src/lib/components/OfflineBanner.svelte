<script lang="ts">
	import { pwaStore } from '$lib/stores/pwa';
	import { connectionStore } from '$lib/stores/connection';

	// Only show banner if offline but API was previously reachable
	// (The full overlay handles the case when API is unreachable)
	let shouldShow = $derived(!$pwaStore.isOnline && $connectionStore.isApiReachable);
</script>

{#if shouldShow}
	<div
		class="fixed left-0 right-0 top-0 z-50 bg-amber-500 px-4 py-2 text-center text-sm font-medium text-amber-900"
	>
		<span>You're offline - viewing cached data (read-only mode)</span>
	</div>
{/if}
