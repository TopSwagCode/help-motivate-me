import type { MembershipTier, UserRole } from './auth';

export interface AdminStats {
	totalUsers: number;
	activeUsers: number;
	membershipStats: MembershipStats;
	todayStats: TodayStats;
}

export interface MembershipStats {
	freeUsers: number;
	plusUsers: number;
	proUsers: number;
}

export interface TodayStats {
	tasksCreated: number;
	tasksUpdated: number;
	tasksCompleted: number;
}

export interface AdminUser {
	id: string;
	username: string;
	email: string;
	displayName: string | null;
	isActive: boolean;
	membershipTier: MembershipTier;
	role: UserRole;
	createdAt: string;
	lastActiveAt: string | null;
}

export interface UpdateRoleRequest {
	role: UserRole;
}
