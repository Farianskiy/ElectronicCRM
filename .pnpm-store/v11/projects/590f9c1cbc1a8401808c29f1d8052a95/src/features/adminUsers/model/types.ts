import type { UserType } from "@/shared/api/authToken";

export type CreatableUserType = "Regular" | "Manager" | "Technical" | "Administrator";
export type UserStatus = "Active" | "Blocked";

export interface AdminUserListItem {
  id: string;
  displayName: string;
  email: string | null;
  userType: UserType;
  status: UserStatus;
  createdAtUtc: string;
}

export interface GetAdminUsersResponse {
  items: AdminUserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateAdminUserRequest {
  displayName: string;
  email: string;
  password: string;
  userType: CreatableUserType;
}

export interface CreateAdminUserResponse {
  id: string;
}

export interface UserPermissionItem {
  code: string;
  group: string;
  label: string;
  isAllowed: boolean;
  isOverridden: boolean;
}

export interface GetUserAccessResponse {
  userId: string;
  userType: UserType;
  permissions: UserPermissionItem[];
}

export interface UpdateUserAccessRequest {
  userType: CreatableUserType;
  allowedPermissions: string[];
}