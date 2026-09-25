<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { auth } from '$lib/stores/auth';

	onMount(async () => {
		if (!$auth.initialized) {
			await auth.init();
		}

		await goto(resolve($auth.user ? '/today' : '/auth/login'), { replaceState: true });
	});
</script>

<svelte:head>
	<title>Help Motivate Me</title>
</svelte:head>

<div class="min-h-screen bg-warm-paper"></div>
