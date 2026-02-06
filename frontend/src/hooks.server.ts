import type { HandleServerError } from '@sveltejs/kit';

export const handleError: HandleServerError = async ({ error, event, status, message }) => {
	// Log errors in development mode only
	if (import.meta.env.DEV) {
		const timestamp = new Date().toISOString();
		// eslint-disable-next-line no-console
		console.error(`[${timestamp}] Server Error at ${event.url.pathname}:`, {
			status,
			message,
			error
		});
	}

	// Return sanitized error message (don't expose internal details)
	return {
		message: status === 404 ? 'Page not found' : 'An unexpected error occurred'
	};
};
