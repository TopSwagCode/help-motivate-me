<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { t, locale } from 'svelte-i18n';
	import { driver, type DriveStep, type Driver } from 'driver.js';
	import 'driver.js/dist/driver.css';
	import { tour } from '$lib/stores/tour';
	import { tourSteps } from '$lib/config/tourSteps';

	let driverInstance: Driver | null = null;
	let isInitialized = false;

	function getStepsForCurrentPage(currentPage: string): DriveStep[] {
		const pageSteps = tourSteps.filter((step) => step.page === currentPage);
		const currentStepIndex = $tour.currentStepIndex;

		return pageSteps.map((step, index) => {
			const globalIndex = tourSteps.findIndex((s) => s.id === step.id);
			const isLastStep = globalIndex === tourSteps.length - 1;
			const isLastOnPage = index === pageSteps.length - 1;
			const nextStep = tourSteps[globalIndex + 1];
			const needsNavigation = isLastOnPage && !isLastStep && nextStep?.page !== currentPage;

			return {
				element: step.element,
				popover: {
					title: $t(step.titleKey),
					description: $t(step.descriptionKey),
					side: step.position,
					align: 'center' as const,
					showButtons: ['next', 'previous', 'close'],
					nextBtnText: isLastStep ? $t('tour.controls.finish') : $t('tour.controls.next'),
					prevBtnText: $t('tour.controls.previous'),
					doneBtnText: $t('tour.controls.finish'),
					closeBtnText: $t('tour.controls.skip'),
					onNextClick: () => {
						if (isLastStep) {
							tour.completeTour();
							driverInstance?.destroy();
							goto('/today');
						} else if (needsNavigation && nextStep) {
							tour.setStep(globalIndex + 1, nextStep.page);
							driverInstance?.destroy();
							goto(nextStep.page);
						} else {
							tour.setStep(globalIndex + 1, currentPage);
							driverInstance?.moveNext();
						}
					},
					onPrevClick: () => {
						if (globalIndex > 0) {
							const prevStep = tourSteps[globalIndex - 1];
							if (prevStep.page !== currentPage) {
								tour.setStep(globalIndex - 1, prevStep.page);
								driverInstance?.destroy();
								goto(prevStep.page);
							} else {
								tour.setStep(globalIndex - 1, currentPage);
								driverInstance?.movePrevious();
							}
						}
					},
					onCloseClick: () => {
						tour.skipTour();
						driverInstance?.destroy();
					}
				}
			};
		});
	}

	function initTour() {
		if (!$tour.isActive || isInitialized) return;

		const currentPath = $page.url.pathname;
		const currentStep = tourSteps[$tour.currentStepIndex];

		// If we're not on the right page, navigate there
		if (currentStep && currentStep.page !== currentPath) {
			goto(currentStep.page);
			return;
		}

		const steps = getStepsForCurrentPage(currentPath);
		if (steps.length === 0) return;

		// Find which step index to start at on this page
		const firstStepOnPage = tourSteps.findIndex(
			(s) => s.page === currentPath && s.id === tourSteps[$tour.currentStepIndex]?.id
		);
		const localStartIndex = steps.findIndex(
			(_, i) =>
				tourSteps.find((s) => s.page === currentPath && tourSteps.indexOf(s) === i + firstStepOnPage)
		);

		isInitialized = true;

		// Wait for elements to be rendered
		setTimeout(() => {
			driverInstance = driver({
				showProgress: true,
				progressText: $t('tour.controls.step', { values: { current: '{{current}}', total: '{{total}}' } }),
				steps,
				allowClose: true,
				overlayColor: 'rgba(0, 0, 0, 0.6)',
				stagePadding: 8,
				stageRadius: 8,
				popoverClass: 'tour-popover',
				onDestroyed: () => {
					isInitialized = false;
				}
			});

			// Calculate which step to start at within this page
			const pageStepIds = steps.map((_, i) => i);
			const currentGlobalStep = $tour.currentStepIndex;
			const stepsBeforeThisPage = tourSteps
				.slice(0, currentGlobalStep)
				.filter((s) => s.page === currentPath).length;

			driverInstance.drive(stepsBeforeThisPage);
		}, 300);
	}

	// Watch for tour activation
	$effect(() => {
		if ($tour.isActive && !isInitialized) {
			initTour();
		}
	});

	// Watch for page changes during tour
	$effect(() => {
		const currentPath = $page.url.pathname;
		if ($tour.isActive && $tour.currentPage === currentPath && !isInitialized) {
			initTour();
		}
	});

	// Re-initialize when locale changes
	$effect(() => {
		if ($locale && $tour.isActive && driverInstance) {
			driverInstance.destroy();
			isInitialized = false;
			initTour();
		}
	});

	onDestroy(() => {
		if (driverInstance) {
			driverInstance.destroy();
		}
	});
</script>

<style>
	:global(.tour-popover) {
		--popover-bg: white;
		--popover-title-color: #111827;
		--popover-description-color: #4b5563;
	}

	:global(.driver-popover) {
		max-width: 90vw;
	}

	@media (min-width: 640px) {
		:global(.driver-popover) {
			max-width: 400px;
		}
	}

	:global(.driver-popover-title) {
		font-size: 1.125rem;
		font-weight: 600;
		color: #111827;
	}

	:global(.driver-popover-description) {
		font-size: 0.875rem;
		line-height: 1.5;
		color: #4b5563;
	}

	:global(.driver-popover-navigation-btns) {
		display: flex;
		gap: 0.5rem;
		margin-top: 1rem;
	}

	:global(.driver-popover-next-btn) {
		background-color: #0284c7;
		color: white;
		padding: 0.5rem 1rem;
		border-radius: 0.5rem;
		font-size: 0.875rem;
		font-weight: 500;
		border: none;
		cursor: pointer;
	}

	:global(.driver-popover-next-btn:hover) {
		background-color: #0369a1;
	}

	:global(.driver-popover-prev-btn) {
		background-color: #f3f4f6;
		color: #374151;
		padding: 0.5rem 1rem;
		border-radius: 0.5rem;
		font-size: 0.875rem;
		font-weight: 500;
		border: none;
		cursor: pointer;
	}

	:global(.driver-popover-prev-btn:hover) {
		background-color: #e5e7eb;
	}

	:global(.driver-popover-close-btn) {
		color: #6b7280;
	}

	:global(.driver-popover-close-btn:hover) {
		color: #374151;
	}

	:global(.driver-popover-progress-text) {
		font-size: 0.75rem;
		color: #9ca3af;
	}
</style>
