<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { t } from 'svelte-i18n';
	import { browser } from '$app/environment';
	import { milestoneStore } from '$lib/stores/milestones';
	import type { UserMilestone } from '$lib/types/milestone';

	let canvas: HTMLCanvasElement;
	let ctx: CanvasRenderingContext2D | null = null;
	let animationId: number;
	let confettiPieces: ConfettiPiece[] = [];

	interface ConfettiPiece {
		x: number;
		y: number;
		vx: number;
		vy: number;
		color: string;
		rotation: number;
		rotationSpeed: number;
		size: number;
	}

	const COLORS = [
		'#f97316', // orange-500
		'#eab308', // yellow-500
		'#22c55e', // green-500
		'#3b82f6', // blue-500
		'#a855f7', // purple-500
		'#ec4899', // pink-500
		'#ef4444'  // red-500
	];

	function initConfetti() {
		if (!browser || !canvas) return;
		ctx = canvas.getContext('2d');
		if (!ctx) return;

		canvas.width = window.innerWidth;
		canvas.height = window.innerHeight;

		// Create initial confetti burst
		for (let i = 0; i < 150; i++) {
			confettiPieces.push({
				x: canvas.width / 2,
				y: canvas.height / 2,
				vx: (Math.random() - 0.5) * 20,
				vy: Math.random() * -15 - 5,
				color: COLORS[Math.floor(Math.random() * COLORS.length)],
				rotation: Math.random() * 360,
				rotationSpeed: (Math.random() - 0.5) * 10,
				size: Math.random() * 8 + 4
			});
		}

		animateConfetti();
	}

	function animateConfetti() {
		if (!ctx || !canvas) return;

		ctx.clearRect(0, 0, canvas.width, canvas.height);

		confettiPieces.forEach((piece, index) => {
			// Update physics
			piece.x += piece.vx;
			piece.y += piece.vy;
			piece.vy += 0.3; // gravity
			piece.vx *= 0.99; // air resistance
			piece.rotation += piece.rotationSpeed;

			// Draw
			ctx!.save();
			ctx!.translate(piece.x, piece.y);
			ctx!.rotate((piece.rotation * Math.PI) / 180);
			ctx!.fillStyle = piece.color;
			ctx!.fillRect(-piece.size / 2, -piece.size / 2, piece.size, piece.size * 0.6);
			ctx!.restore();

			// Remove if off screen
			if (piece.y > canvas.height + 50) {
				confettiPieces.splice(index, 1);
			}
		});

		if (confettiPieces.length > 0) {
			animationId = requestAnimationFrame(animateConfetti);
		}
	}

	onMount(() => {
		if (browser) {
			initConfetti();
		}
	});

	onDestroy(() => {
		if (browser && animationId) {
			cancelAnimationFrame(animationId);
		}
	});

	function handleDismiss() {
		milestoneStore.dismissCelebration();
	}

	function handleNext() {
		milestoneStore.nextMilestone();
	}

	function handlePrev() {
		milestoneStore.prevMilestone();
	}

	let state = $derived($milestoneStore);
	let currentMilestone = $derived(state.pendingMilestones[state.currentIndex] as UserMilestone | undefined);
	let isMultiple = $derived(state.pendingMilestones.length > 1);
</script>

{#if state.showCelebration && currentMilestone}
	<div class="fixed inset-0 z-[300] flex items-center justify-center bg-black/60 backdrop-blur-sm">
		<!-- Confetti canvas -->
		<canvas bind:this={canvas} class="absolute inset-0 pointer-events-none"></canvas>

		<!-- Modal -->
		<div class="relative w-full max-w-md mx-4 bg-warm-paper rounded-3xl shadow-2xl overflow-hidden animate-bounce-in">
			<!-- Close button -->
			<button
				onclick={handleDismiss}
				class="absolute top-4 right-4 p-2 text-cocoa-400 hover:text-cocoa-600 transition-colors z-10"
				aria-label="Close"
			>
				<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
				</svg>
			</button>

			<!-- Icon area -->
			<div class="bg-gradient-to-br from-primary-400 via-primary-500 to-primary-600 pt-12 pb-8 px-6 text-center">
				<div class="w-24 h-24 mx-auto bg-warm-paper/20 rounded-full flex items-center justify-center text-5xl animate-pulse-scale">
					{currentMilestone.icon}
				</div>
			</div>

			<!-- Content -->
			<div class="px-8 py-6 text-center">
				<h2 class="text-2xl font-bold text-cocoa-800 mb-2">
					{$t('milestones.celebration.title')}
				</h2>

				<h3 class="text-xl font-semibold text-primary-600 mb-3">
					{$t(currentMilestone.titleKey)}
				</h3>

				<p class="text-cocoa-600 mb-6">
					{$t(currentMilestone.descriptionKey)}
				</p>

				<!-- Navigation dots for multiple milestones -->
				{#if isMultiple}
					<div class="flex justify-center gap-2 mb-6">
						{#each state.pendingMilestones as _, index}
							<button
								onclick={() => {
									if (index < state.currentIndex) {
										milestoneStore.prevMilestone();
									} else if (index > state.currentIndex) {
										milestoneStore.nextMilestone();
									}
								}}
								class="w-2 h-2 rounded-full transition-colors {index === state.currentIndex ? 'bg-primary-500' : 'bg-cocoa-200 hover:bg-cocoa-300'}"
								aria-label="Go to milestone {index + 1}"
							></button>
						{/each}
					</div>
				{/if}

				<!-- Action buttons -->
				<div class="flex gap-3">
					{#if isMultiple && state.currentIndex > 0}
						<button
							onclick={handlePrev}
							class="flex-1 px-4 py-3 text-cocoa-600 font-medium rounded-xl hover:bg-cocoa-100 transition-colors"
						>
							{$t('common.previous')}
						</button>
					{/if}

					{#if isMultiple && state.currentIndex < state.pendingMilestones.length - 1}
						<button
							onclick={handleNext}
							class="flex-1 px-4 py-3 bg-primary-600 text-white font-semibold rounded-xl hover:bg-primary-700 transition-colors"
						>
							{$t('common.next')}
						</button>
					{:else}
						<button
							onclick={handleDismiss}
							class="flex-1 px-4 py-3 bg-primary-600 text-white font-semibold rounded-xl hover:bg-primary-700 transition-colors"
						>
							{$t('milestones.celebration.awesome')}
						</button>
					{/if}
				</div>
			</div>
		</div>
	</div>
{/if}

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

	@keyframes pulse-scale {
		0%, 100% {
			transform: scale(1);
		}
		50% {
			transform: scale(1.1);
		}
	}

	.animate-bounce-in {
		animation: bounce-in 0.5s ease-out;
	}

	.animate-pulse-scale {
		animation: pulse-scale 2s ease-in-out infinite;
	}
</style>
