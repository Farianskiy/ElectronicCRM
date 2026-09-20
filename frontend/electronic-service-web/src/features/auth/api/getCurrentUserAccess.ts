import { httpClient } from "@/shared/api/httpClient";
import type { CurrentUserAccessResponse } from "../model/userPermissions";

export async function getCurrentUserAccess(): Promise<CurrentUserAccessResponse> {
  const response = await httpClient.get<CurrentUserAccessResponse>("/api/users/me/access");

  return response.data;
}