export interface RegisterRegularUserRequest {
  displayName: string;
  email: string;
  password: string;
}

export interface RegisterUserResponse {
  id: string;
}