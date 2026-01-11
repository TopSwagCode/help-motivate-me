<script lang="ts">
	import { t } from 'svelte-i18n';
	import { setLocale, getLanguageFromLocale, locale } from '$lib/i18n';
	import { updateLanguage } from '$lib/api/auth';
	import { auth } from '$lib/stores/auth';
	import type { Language } from '$lib/types';

	interface Props {
		oncontinue: () => void;
	}

	let { oncontinue }: Props = $props();

	let selectedLocale = $state($locale || 'en');
	let saving = $state(false);

	// Detect browser language on mount
	$effect(() => {
		if (!$locale) {
			const browserLang = navigator.language;
			if (browserLang.startsWith('da')) {
				selectedLocale = 'da';
			}
		}
	});

	function selectLanguage(localeCode: string) {
		selectedLocale = localeCode;
		setLocale(localeCode);
	}

	async function handleContinue() {
		saving = true;
		try {
			const language = getLanguageFromLocale(selectedLocale) as Language;
			const updatedUser = await updateLanguage(language);
			auth.updateUser(updatedUser);
			oncontinue();
		} catch (e) {
			console.error('Failed to update language:', e);
			// Continue anyway, language is already set locally
			oncontinue();
		} finally {
			saving = false;
		}
	}
</script>

<div class="min-h-screen bg-gradient-to-b from-primary-50 to-white">
	<div class="max-w-2xl mx-auto px-4 py-12">
		<div class="text-center mb-10">
			<h1 class="text-3xl font-bold text-gray-900 mb-3">{$t('onboarding.language.title')}</h1>
			<p class="text-gray-600">{$t('onboarding.language.subtitle')}</p>
		</div>

		<div class="grid gap-4 md:grid-cols-2 mb-8">
			<!-- English Option -->
			<button
				onclick={() => selectLanguage('en')}
				class="card p-6 text-left transition-all duration-200 cursor-pointer {selectedLocale === 'en'
					? 'border-primary-500 ring-2 ring-primary-200 bg-primary-50'
					: 'hover:border-gray-300 hover:shadow-md'}"
			>
				<div class="flex items-center gap-4">
					<div
						class="w-12 h-12 rounded-full flex items-center justify-center text-2xl bg-gray-100"
					>
						<span class="text-3xl">EN</span>
					</div>
					<div>
						<h2 class="text-lg font-semibold text-gray-900">{$t('language.english')}</h2>
						<p class="text-sm text-gray-500">English</p>
					</div>
					{#if selectedLocale === 'en'}
						<div class="ml-auto">
							<svg
								class="w-6 h-6 text-primary-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M5 13l4 4L19 7"
								/>
							</svg>
						</div>
					{/if}
				</div>
			</button>

			<!-- Danish Option -->
			<button
				onclick={() => selectLanguage('da')}
				class="card p-6 text-left transition-all duration-200 cursor-pointer {selectedLocale === 'da'
					? 'border-primary-500 ring-2 ring-primary-200 bg-primary-50'
					: 'hover:border-gray-300 hover:shadow-md'}"
			>
				<div class="flex items-center gap-4">
					<div
						class="w-12 h-12 rounded-full flex items-center justify-center text-2xl bg-gray-100"
					>
						<span class="text-3xl">DK</span>
					</div>
					<div>
						<h2 class="text-lg font-semibold text-gray-900">{$t('language.danish')}</h2>
						<p class="text-sm text-gray-500">Dansk</p>
					</div>
					{#if selectedLocale === 'da'}
						<div class="ml-auto">
							<svg
								class="w-6 h-6 text-primary-600"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M5 13l4 4L19 7"
								/>
							</svg>
						</div>
					{/if}
				</div>
			</button>
		</div>

		<div class="flex justify-center">
			<button onclick={handleContinue} disabled={saving} class="btn btn-primary px-8 py-3 text-lg">
				{saving ? $t('common.saving') : $t('onboarding.language.continue')}
			</button>
		</div>
	</div>
</div>
