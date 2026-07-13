import { httpClient } from "@/shared/api/httpClient";
import type { LoginRequest, LoginResponse } from "../model/types";

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await httpClient.post<LoginResponse>(
    "/api/auth/login",
    request,
  );

  return response.data;
}