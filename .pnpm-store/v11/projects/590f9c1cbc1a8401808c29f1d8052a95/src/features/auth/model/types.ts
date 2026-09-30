import type { UserType } from "@/shared/api/authToken";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  userId: string;
  userType: UserType;
  displayName: string;
}