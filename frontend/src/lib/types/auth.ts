export type MembershipTier = 'Free' | 'Plus' | 'Pro';
export type UserRole = 'User' | 'Admin';
export type Language = 'English' | 'Danish';

export interface User {
	id: string;
	email: string;
	displayName: string | null;
	createdAt: string;
	linkedProviders: string[];
	hasPassword: boolean;
	membershipTier: MembershipTier;
	hasCompletedOnboarding: boolean;
	role: UserRole;
	preferredLanguage: Language;
}

export interface LoginRequest {
	email: string;
	password: string;
}

export interface RegisterRequest {
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

export interface UpdateLanguageRequest {
	language: Language;
}
