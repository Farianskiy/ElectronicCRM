import { httpClient } from "@/shared/api/httpClient";
import type {
  RegisterRegularUserRequest,
  RegisterUserResponse,
} from "../model/registerTypes";

export async function registerRegularUser(
  request: RegisterRegularUserRequest,
): Promise<RegisterUserResponse> {
  const response = await httpClient.post<RegisterUserResponse>(
    "/api/users/regular",
    request,
  );

  return response.data;
}