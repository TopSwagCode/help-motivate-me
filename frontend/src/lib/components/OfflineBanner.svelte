<script lang="ts">
	import { pwaStore } from '$lib/stores/pwa';

	let isOnline = $derived($pwaStore.isOnline);
</script>

{#if !isOnline}
	<div
		class="fixed left-0 right-0 top-0 z-50 bg-amber-500 px-4 py-2 text-center text-sm font-medium text-amber-900"
	>
		<span>📴 You're offline - viewing cached data (read-only mode)</span>
	</div>
{/if}
