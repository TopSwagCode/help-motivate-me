import { apiGet, apiPost, apiPatch, apiDelete } from './client';
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

export async function register(data: RegisterRequest): Promise<{ message: string; email: string }> {
	return apiPost<{ message: string; email: string }>('/auth/register', data);
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

export async function verifyEmail(token: string): Promise<User> {
	return apiPost<User>('/auth/verify-email', { token });
}

export async function resendVerification(email: string): Promise<{ message: string }> {
	return apiPost<{ message: string }>('/auth/resend-verification', { email });
}

export async function deleteAccount(password?: string): Promise<void> {
	return apiDelete<void>('/auth/account', { password });
}
