import axios from "axios";

interface ApiErrorResponse {
  detail?: unknown;
  message?: unknown;
  title?: unknown;
}

export function getApiErrorMessage(
  error: unknown,
  fallbackMessage = "Произошла неизвестная ошибка.",
): string {
  if (!axios.isAxiosError(error)) {
    return fallbackMessage;
  }

  const responseData: unknown = error.response?.data;

  if (typeof responseData === "string" && responseData.trim().length > 0) {
    return responseData;
  }

  if (!responseData || typeof responseData !== "object") {
    return fallbackMessage;
  }

  const apiError = responseData as ApiErrorResponse;

  if (typeof apiError.detail === "string" && apiError.detail.trim().length > 0) {
    return apiError.detail;
  }

  if (
    typeof apiError.message === "string" &&
    apiError.message.trim().length > 0
  ) {
    return apiError.message;
  }

  if (typeof apiError.title === "string" && apiError.title.trim().length > 0) {
    return apiError.title;
  }

  return fallbackMessage;
}