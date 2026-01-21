import { browser } from '$app/environment';

const API_BASE = import.meta.env.VITE_API_URL !== undefined ? import.meta.env.VITE_API_URL : '';

export class ApiError extends Error {
	constructor(
		public status: number,
		message: string,
		public code?: string,
		public data?: Record<string, unknown>
	) {
		super(message);
		this.name = 'ApiError';
	}
}

export class OfflineError extends Error {
	constructor() {
		super('You are offline. This action requires an internet connection.');
		this.name = 'OfflineError';
	}
}

function isOnline(): boolean {
	return !browser || navigator.onLine;
}

async function handleResponse<T>(response: Response, skipAuthRedirect = false): Promise<T> {
	if (!response.ok) {
		if (response.status === 401 && !skipAuthRedirect) {
			// Redirect to login on unauthorized (unless we're checking auth status)
			window.location.href = '/auth/login';
		}

		const errorText = await response.text();
		let errorMessage = `HTTP ${response.status}`;
		let errorCode: string | undefined;
		let errorData: Record<string, unknown> | undefined;
		try {
			const errorJson = JSON.parse(errorText);
			errorMessage = errorJson.message || errorJson.title || errorMessage;
			errorCode = errorJson.code;
			errorData = errorJson;
		} catch {
			errorMessage = errorText || errorMessage;
		}
		throw new ApiError(response.status, errorMessage, errorCode, errorData);
	}

	if (response.status === 204) {
		return undefined as T;
	}

	return response.json();
}

export async function apiGet<T>(endpoint: string, options?: { skipAuthRedirect?: boolean }): Promise<T> {
	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'GET',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		}
	});
	return handleResponse<T>(response, options?.skipAuthRedirect);
}

export async function apiPost<T>(endpoint: string, data?: unknown): Promise<T> {
	if (!isOnline()) {
		throw new OfflineError();
	}

	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'POST',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: data ? JSON.stringify(data) : undefined
	});
	return handleResponse<T>(response);
}

export async function apiPut<T>(endpoint: string, data: unknown): Promise<T> {
	if (!isOnline()) {
		throw new OfflineError();
	}

	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'PUT',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: JSON.stringify(data)
	});
	return handleResponse<T>(response);
}

export async function apiPatch<T>(endpoint: string, data?: unknown): Promise<T> {
	if (!isOnline()) {
		throw new OfflineError();
	}

	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'PATCH',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: data ? JSON.stringify(data) : undefined
	});
	return handleResponse<T>(response);
}

export async function apiDelete<T>(endpoint: string, data?: unknown): Promise<T> {
	if (!isOnline()) {
		throw new OfflineError();
	}

	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'DELETE',
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			'X-CSRF': '1'
		},
		body: data ? JSON.stringify(data) : undefined
	});
	return handleResponse<T>(response);
}

export async function apiUpload<T>(endpoint: string, file: File): Promise<T> {
	if (!isOnline()) {
		throw new OfflineError();
	}

	const formData = new FormData();
	formData.append('file', file);

	const response = await fetch(`${API_BASE}/api${endpoint}`, {
		method: 'POST',
		credentials: 'include',
		headers: {
			'X-CSRF': '1'
		},
		body: formData
	});
	return handleResponse<T>(response);
}

export function getOAuthUrl(provider: string): string {
	return `${API_BASE}/api/auth/external/${provider}`;
}
