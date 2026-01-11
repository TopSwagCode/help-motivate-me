<script lang="ts">
	import '../app.css';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import TopNav from '$lib/components/layout/TopNav.svelte';
	import { initI18n, setLocale, getLocaleFromLanguage } from '$lib/i18n';
	import { isLoading as i18nLoading } from 'svelte-i18n';

	// Initialize i18n
	initI18n();

	let { children } = $props();

	// Sync locale with user's preferred language when auth state changes
	$effect(() => {
		if ($auth.user?.preferredLanguage) {
			const userLocale = getLocaleFromLanguage($auth.user.preferredLanguage);
			setLocale(userLocale);
		}
	});

	// Routes that should NOT show the nav
	const publicRoutes = ['/', '/auth/login', '/auth/register', '/auth/callback', '/faq', '/privacy', '/terms', '/pricing', '/about', '/contact'];

	function shouldShowNav(): boolean {
		const path = $page.url.pathname;
		return $auth.user !== null && !publicRoutes.includes(path);
	}
</script>

<svelte:head>
	<title>Help Motivate Me</title>
	<meta name="description" content="A modern task and goal management app to help you stay motivated" />
</svelte:head>

{#if shouldShowNav()}
	<TopNav />
{/if}

{@render children()}
