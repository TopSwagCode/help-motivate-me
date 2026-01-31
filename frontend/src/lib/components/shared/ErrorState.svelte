<script lang="ts">
	import { t } from 'svelte-i18n';

	interface Props {
		/**
		 * Optional specific error message to show (for debugging or more context)
		 * If not provided, uses the generic error message from translations
		 */
		message?: string;
		/**
		 * Whether to show a retry button
		 */
		showRetry?: boolean;
		/**
		 * Callback when retry button is clicked
		 */
		onRetry?: () => void;
		/**
		 * Size variant: 'sm' for inline errors, 'md' for section errors, 'lg' for full page
		 */
		size?: 'sm' | 'md' | 'lg';
	}

	let { 
		message = '', 
		showRetry = true, 
		onRetry, 
		size = 'md' 
	}: Props = $props();

	const sizeClasses = {
		sm: {
			container: 'py-6 px-4',
			icon: 'w-10 h-10',
			iconWrapper: 'w-16 h-16',
			title: 'text-base',
			message: 'text-sm',
			suggestion: 'text-xs',
			button: 'px-3 py-2 text-sm'
		},
		md: {
			container: 'py-10 px-6',
			icon: 'w-12 h-12',
			iconWrapper: 'w-20 h-20',
			title: 'text-lg',
			message: 'text-base',
			suggestion: 'text-sm',
			button: 'px-4 py-2.5 text-sm'
		},
		lg: {
			container: 'py-16 px-8',
			icon: 'w-14 h-14',
			iconWrapper: 'w-24 h-24',
			title: 'text-xl',
			message: 'text-lg',
			suggestion: 'text-base',
			button: 'px-5 py-3 text-base'
		}
	};

	let classes = $derived(sizeClasses[size]);
</script>

<div class="flex flex-col items-center justify-center text-center {classes.container}">
	<!-- Friendly Icon -->
	<div class="{classes.iconWrapper} mx-auto bg-indigo-50 rounded-full flex items-center justify-center mb-4">
		<svg class="{classes.icon} text-indigo-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 9h.01M9 9h.01" />
			<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 13c.5.5 1.5 1 3 1s2.5-.5 3-1" />
		</svg>
	</div>

	<!-- Title -->
	<h3 class="font-semibold text-gray-900 mb-2 {classes.title}">
		{$t('errorState.title')}
	</h3>

	<!-- Message -->
	<p class="text-gray-600 mb-1 {classes.message}">
		{$t('errorState.message')}
	</p>

	<!-- Suggestion -->
	<p class="text-gray-500 mb-5 max-w-md {classes.suggestion}">
		{$t('errorState.suggestion')}
	</p>

	<!-- Technical error (smaller, for support) -->
	{#if message}
		<p class="text-xs text-gray-400 mb-4 font-mono bg-gray-50 px-3 py-1.5 rounded-md max-w-full overflow-x-auto">
			{message}
		</p>
	{/if}

	<!-- Retry Button -->
	{#if showRetry && onRetry}
		<button
			onclick={onRetry}
			class="inline-flex items-center justify-center gap-2 {classes.button} border border-transparent font-medium rounded-xl text-white bg-indigo-600 hover:bg-indigo-700 transition-colors"
		>
			<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
			</svg>
			{$t('errorState.tryAgain')}
		</button>
	{/if}
</div>
