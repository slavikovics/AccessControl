import axios from "axios";

// withCredentials so the browser sends/accepts the HttpOnly auth cookie set
// by the API; the token itself is never touched by frontend JavaScript.
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5080",
  withCredentials: true,
});

export function errorMessage(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { message?: string } | undefined;
    if (data?.message) return data.message;
    if (err.response?.status === 429) return "Too many failed attempts. Try again later.";
  }
  return fallback;
}
