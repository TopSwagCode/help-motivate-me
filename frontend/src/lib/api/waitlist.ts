import { apiGet, apiPost } from './client';
import type { WhitelistCheckResponse, WaitlistSignupResponse } from '$lib/types';

export interface WaitlistSignupRequest {
	email: string;
	name: string;
}

export async function signupForWaitlist(data: WaitlistSignupRequest): Promise<WaitlistSignupResponse> {
	return apiPost<WaitlistSignupResponse>('/waitlist', data);
}

export async function checkWhitelistStatus(email: string): Promise<WhitelistCheckResponse> {
	return apiGet<WhitelistCheckResponse>(`/waitlist/check?email=${encodeURIComponent(email)}`);
}
