<script lang="ts">
	import { onMount } from 'svelte';
	import confetti from 'canvas-confetti';
	import { t } from 'svelte-i18n';

	interface Props {
		onclose: () => void;
	}

	let { onclose }: Props = $props();

	onMount(() => {
		// Fire confetti explosion
		const duration = 3000;
		const end = Date.now() + duration;

		const colors = ['#6366f1', '#8b5cf6', '#a855f7', '#22c55e', '#3b82f6'];

		function frame() {
			confetti({
				particleCount: 5,
				angle: 60,
				spread: 55,
				origin: { x: 0, y: 0.7 },
				colors: colors
			});
			confetti({
				particleCount: 5,
				angle: 120,
				spread: 55,
				origin: { x: 1, y: 0.7 },
				colors: colors
			});

			if (Date.now() < end) {
				requestAnimationFrame(frame);
			}
		}

		// Initial big burst
		confetti({
			particleCount: 100,
			spread: 100,
			origin: { y: 0.6 },
			colors: colors
		});

		// Continuous side cannons
		frame();
	});
</script>

<div
	class="fixed inset-0 bg-black bg-opacity-60 flex items-center justify-center z-50 p-4"
	role="dialog"
	aria-modal="true"
>
	<div
		class="bg-white rounded-2xl shadow-2xl max-w-lg w-full p-8 text-center transform animate-bounce-in"
	>
		<!-- Celebration icon -->
		<div class="mb-6">
			<div
				class="w-20 h-20 mx-auto bg-gradient-to-br from-primary-400 to-primary-600 rounded-full flex items-center justify-center shadow-lg"
			>
				<span class="text-4xl">🎉</span>
			</div>
		</div>

		<!-- Welcome message -->
		<h2 class="text-2xl font-bold text-gray-900 mb-3">{$t('onboarding.welcome.title')}</h2>

		<p class="text-gray-600 mb-6">
			{@html $t('onboarding.welcome.subtitle')}
		</p>

		<!-- Feature highlights -->
		<div class="bg-gray-50 rounded-xl p-4 mb-6 text-left">
			<h3 class="font-medium text-gray-900 mb-3">{$t('onboarding.welcome.hereYouCan')}</h3>
			<ul class="space-y-2 text-sm text-gray-600">
				<li class="flex items-start gap-2">
					<svg class="w-5 h-5 text-green-500 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 24 24">
						<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
					</svg>
					<span>{$t('onboarding.welcome.features.trackHabits')}</span>
				</li>
				<li class="flex items-start gap-2">
					<svg class="w-5 h-5 text-green-500 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 24 24">
						<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
					</svg>
					<span>{$t('onboarding.welcome.features.completeTasks')}</span>
				</li>
				<li class="flex items-start gap-2">
					<svg class="w-5 h-5 text-green-500 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 24 24">
						<path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z" />
					</svg>
					<span>{$t('onboarding.welcome.features.reinforceIdentity')}</span>
				</li>
			</ul>
		</div>

		<!-- CTA button -->
		<button onclick={onclose} class="btn-primary w-full text-lg py-3">
			{$t('onboarding.welcome.letsGo')}
			<svg class="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
			</svg>
		</button>
	</div>
</div>

<style>
	@keyframes bounce-in {
		0% {
			opacity: 0;
			transform: scale(0.3);
		}
		50% {
			transform: scale(1.05);
		}
		70% {
			transform: scale(0.9);
		}
		100% {
			opacity: 1;
			transform: scale(1);
		}
	}

	.animate-bounce-in {
		animation: bounce-in 0.5s ease-out;
	}
</style>
