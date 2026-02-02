<script lang="ts">
	import { t } from 'svelte-i18n';

	interface Props {
		onselect: (mode: 'manual' | 'ai') => void;
	}

	let { onselect }: Props = $props();
</script>

<div class="min-h-screen bg-gradient-to-b from-primary-50 to-white">
	<div class="max-w-2xl mx-auto px-4 py-12">
		<div class="text-center mb-10">
			<h1 class="text-3xl font-bold text-cocoa-800 mb-3">{$t('onboarding.modeSelect.title')}</h1>
			<p class="text-cocoa-600">
				{$t('onboarding.modeSelect.subtitle')}
			</p>
		</div>

		<div class="grid gap-6 md:grid-cols-2">
			<!-- Manual Setup Card -->
			<button
				onclick={() => onselect('manual')}
				class="card p-6 text-left hover:border-primary-300 hover:shadow-lg transition-all duration-300 group cursor-pointer"
			>
				<div
					class="w-14 h-14 bg-gray-100 rounded-xl flex items-center justify-center mb-4 group-hover:bg-primary-100 transition-colors"
				>
					<svg
						class="w-7 h-7 text-cocoa-600 group-hover:text-primary-600"
						fill="none"
						stroke="currentColor"
						viewBox="0 0 24 24"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"
						/>
					</svg>
				</div>
				<h2 class="text-xl font-semibold text-cocoa-800 mb-2">
					{$t('onboarding.modeSelect.manual.title')}
				</h2>
				<p class="text-cocoa-600 text-sm mb-4">
					{$t('onboarding.modeSelect.manual.description')}
				</p>
				<span
					class="inline-flex items-center text-sm font-medium text-primary-600 group-hover:text-primary-700"
				>
					{$t('onboarding.modeSelect.manual.cta')}
					<svg class="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
					</svg>
				</span>
			</button>

			<!-- AI Assistant Card -->
			<button
				onclick={() => onselect('ai')}
				class="card p-6 text-left hover:border-primary-300 hover:shadow-lg transition-all duration-300 group cursor-pointer relative overflow-hidden"
			>
				<div
					class="absolute top-3 right-3 px-2 py-1 bg-primary-100 text-primary-700 text-xs font-medium rounded-full"
				>
					{$t('onboarding.modeSelect.ai.recommended')}
				</div>
				<div
					class="w-14 h-14 bg-primary-100 rounded-xl flex items-center justify-center mb-4 group-hover:bg-primary-200 transition-colors"
				>
					<svg
						class="w-7 h-7 text-primary-600"
						fill="none"
						stroke="currentColor"
						viewBox="0 0 24 24"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
						/>
					</svg>
				</div>
				<h2 class="text-xl font-semibold text-cocoa-800 mb-2">
					{$t('onboarding.modeSelect.ai.title')}
				</h2>
				<p class="text-cocoa-600 text-sm mb-4">
					{$t('onboarding.modeSelect.ai.description')}
				</p>
				<div class="flex items-center gap-3 text-xs text-cocoa-500 mb-4">
					<span class="flex items-center gap-1">
						<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 116 0v6a3 3 0 01-3 3z"
							/>
						</svg>
						{$t('onboarding.modeSelect.ai.voiceInput')}
					</span>
					<span class="flex items-center gap-1">
						<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M13 10V3L4 14h7v7l9-11h-7z"
							/>
						</svg>
						{$t('onboarding.modeSelect.ai.instantSuggestions')}
					</span>
				</div>
				<span
					class="inline-flex items-center text-sm font-medium text-primary-600 group-hover:text-primary-700"
				>
					{$t('onboarding.modeSelect.ai.cta')}
					<svg class="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
					</svg>
				</span>
			</button>
		</div>
	</div>
</div>
