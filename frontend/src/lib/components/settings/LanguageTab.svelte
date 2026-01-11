<script lang="ts">
	import { t } from 'svelte-i18n';
	import { setLocale, getLanguageFromLocale, locale } from '$lib/i18n';
	import { updateLanguage } from '$lib/api/auth';
	import { auth } from '$lib/stores/auth';
	import type { Language } from '$lib/types';

	let saving = $state(false);
	let error = $state('');
	let success = $state('');

	let currentLocale = $derived($locale || 'en');

	async function handleLanguageChange(localeCode: string) {
		saving = true;
		error = '';
		success = '';

		try {
			// Update locale immediately for instant feedback
			setLocale(localeCode);

			// Persist to backend
			const language = getLanguageFromLocale(localeCode) as Language;
			const updatedUser = await updateLanguage(language);
			auth.updateUser(updatedUser);

			success = $t('common.success');
		} catch (e) {
			error = e instanceof Error ? e.message : $t('errors.generic');
			// Revert locale on error
			if ($auth.user?.preferredLanguage) {
				const userLocale = $auth.user.preferredLanguage === 'Danish' ? 'da' : 'en';
				setLocale(userLocale);
			}
		} finally {
			saving = false;
		}
	}
</script>

<div>
	<h2 class="text-lg font-semibold text-gray-900 mb-4">{$t('settings.language.title')}</h2>

	<p class="text-sm text-gray-600 mb-6">{$t('settings.language.description')}</p>

	{#if error}
		<div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4">
			{error}
		</div>
	{/if}

	{#if success}
		<div class="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg text-sm mb-4">
			{success}
		</div>
	{/if}

	<div class="space-y-3 max-w-md">
		<!-- English Option -->
		<button
			onclick={() => handleLanguageChange('en')}
			disabled={saving}
			class="w-full card p-4 text-left transition-all duration-200 cursor-pointer {currentLocale ===
			'en'
				? 'border-primary-500 ring-2 ring-primary-200 bg-primary-50'
				: 'hover:border-gray-300 hover:shadow-md'}"
		>
			<div class="flex items-center gap-4">
				<div class="w-10 h-10 rounded-full flex items-center justify-center text-lg bg-gray-100">
					EN
				</div>
				<div class="flex-1">
					<h3 class="font-medium text-gray-900">{$t('language.english')}</h3>
					<p class="text-sm text-gray-500">English</p>
				</div>
				{#if currentLocale === 'en'}
					<svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
					</svg>
				{/if}
			</div>
		</button>

		<!-- Danish Option -->
		<button
			onclick={() => handleLanguageChange('da')}
			disabled={saving}
			class="w-full card p-4 text-left transition-all duration-200 cursor-pointer {currentLocale ===
			'da'
				? 'border-primary-500 ring-2 ring-primary-200 bg-primary-50'
				: 'hover:border-gray-300 hover:shadow-md'}"
		>
			<div class="flex items-center gap-4">
				<div class="w-10 h-10 rounded-full flex items-center justify-center text-lg bg-gray-100">
					DK
				</div>
				<div class="flex-1">
					<h3 class="font-medium text-gray-900">{$t('language.danish')}</h3>
					<p class="text-sm text-gray-500">Dansk</p>
				</div>
				{#if currentLocale === 'da'}
					<svg class="w-5 h-5 text-primary-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
					</svg>
				{/if}
			</div>
		</button>
	</div>

	{#if saving}
		<p class="text-sm text-gray-500 mt-4">{$t('common.saving')}</p>
	{/if}
</div>
