export type MembershipTier = 'Free' | 'Plus' | 'Pro';

export interface User {
	id: string;
	username: string;
	email: string;
	displayName: string | null;
	createdAt: string;
	linkedProviders: string[];
	hasPassword: boolean;
	membershipTier: MembershipTier;
	hasCompletedOnboarding: boolean;
}

export interface LoginRequest {
	username: string;
	password: string;
}

export interface RegisterRequest {
	username: string;
	email: string;
	password: string;
	displayName?: string;
}

export interface UpdateProfileRequest {
	displayName?: string;
}

export interface ChangePasswordRequest {
	currentPassword: string;
	newPassword: string;
}

export interface UpdateMembershipRequest {
	tier: MembershipTier;
}
