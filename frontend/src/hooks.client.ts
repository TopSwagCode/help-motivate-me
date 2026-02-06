import type { HandleClientError } from '@sveltejs/kit';

export const handleError: HandleClientError = async ({ error, event, status, message }) => {
	// Log errors in development mode
	if (import.meta.env.DEV) {
		// eslint-disable-next-line no-console
		console.error('[Client Error]', {
			status,
			message,
			url: event.url.pathname,
			error
		});
	}

	// Detect network errors (fetch failures)
	const errorMessage = error instanceof Error ? error.message : String(error);
	const isNetworkError = errorMessage.includes('fetch') || errorMessage.includes('Failed to fetch');

	return {
		message: isNetworkError ? 'Unable to connect to server' : message,
		code: isNetworkError ? 'NETWORK_ERROR' : undefined
	};
};
