import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateAdminUserRequest,
  CreateAdminUserResponse,
  GetAdminUsersResponse,
  GetUserAccessResponse,
  UpdateUserAccessRequest,
} from "../model/types";

export async function getAdminUsers(
  search: string,
  page: number,
  type?: string,
): Promise<GetAdminUsersResponse> {
  const query = new URLSearchParams({
    page: page.toString(),
    pageSize: "25",
  });

  if (search.trim().length > 0) {
    query.set("search", search.trim());
  }

  if (type) {
  query.set("type", type);
}

  const response = await httpClient.get<GetAdminUsersResponse>(
    `/api/users?${query.toString()}`,
  );

  return response.data;
}

export async function createAdminUser(
  request: CreateAdminUserRequest,
): Promise<CreateAdminUserResponse> {
  const roleSegment = request.userType.toLowerCase();

  const response = await httpClient.post<CreateAdminUserResponse>(
    `/api/users/${roleSegment}`,
    {
      displayName: request.displayName.trim(),
      email: request.email.trim(),
      password: request.password,
    },
  );

  return response.data;
}

export async function getUserAccess(userId: string): Promise<GetUserAccessResponse> {
  const response = await httpClient.get<GetUserAccessResponse>(`/api/users/${userId}/access`);

  return response.data;
}

export async function updateUserAccess(userId: string, request: UpdateUserAccessRequest): Promise<void> {
  await httpClient.put(`/api/users/${userId}/access`, request);
}