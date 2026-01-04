export interface WaitlistEntry {
	id: string;
	email: string;
	name: string;
	createdAt: string;
}

export interface WhitelistEntry {
	id: string;
	email: string;
	addedAt: string;
	addedByUsername?: string;
	invitedAt?: string;
}

export interface WhitelistCheckResponse {
	canSignup: boolean;
}

export interface WaitlistSignupResponse {
	message: string;
	canSignup?: boolean;
}

export interface SignupSettingsResponse {
	allowSignups: boolean;
}
