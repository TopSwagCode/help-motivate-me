export interface TourStep {
	id: string;
	page: string;
	element: string;
	titleKey: string;
	descriptionKey: string;
	position: 'top' | 'bottom' | 'left' | 'right';
}

export interface TourState {
	isActive: boolean;
	hasCompletedTour: boolean;
	currentStepIndex: number;
	currentPage: string;
}
