import { apiGet, apiPost, apiPatch } from './client';
import type { User, LoginRequest, RegisterRequest, Language } from '$lib/types';

export async function getCurrentUser(): Promise<User> {
	return apiGet<User>('/auth/me');
}

export async function updateLanguage(language: Language): Promise<User> {
	return apiPatch<User>('/auth/language', { language });
}

export async function login(data: LoginRequest): Promise<User> {
	return apiPost<User>('/auth/login', data);
}

export async function register(data: RegisterRequest): Promise<User> {
	return apiPost<User>('/auth/register', data);
}

export async function logout(): Promise<void> {
	return apiPost<void>('/auth/logout');
}

export async function requestLoginLink(email: string): Promise<{ message: string }> {
	return apiPost<{ message: string }>('/auth/request-login-link', { email });
}

export async function loginWithToken(token: string): Promise<User> {
	return apiPost<User>('/auth/login-with-token', { token });
}
