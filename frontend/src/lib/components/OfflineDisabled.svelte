<script lang="ts">
	import { pwaStore } from '$lib/stores/pwa';
	import type { Snippet } from 'svelte';

	interface Props {
		children: Snippet;
		class?: string;
	}

	let { children, class: className = '' }: Props = $props();
	let isOnline = $derived($pwaStore.isOnline);
</script>

<div class={className} class:opacity-50={!isOnline} class:pointer-events-none={!isOnline}>
	{@render children()}
</div>

{#if !isOnline}
	<span class="sr-only">Disabled while offline</span>
{/if}
