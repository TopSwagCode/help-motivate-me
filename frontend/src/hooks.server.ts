import type { HandleServerError } from '@sveltejs/kit';

export const handleError: HandleServerError = async ({ error, event, status, message }) => {
	// Log errors with timestamp and URL
	const timestamp = new Date().toISOString();
	console.error(`[${timestamp}] Server Error at ${event.url.pathname}:`, {
		status,
		message,
		error
	});

	// Return sanitized error message (don't expose internal details)
	return {
		message: status === 404 ? 'Page not found' : 'An unexpected error occurred'
	};
};
