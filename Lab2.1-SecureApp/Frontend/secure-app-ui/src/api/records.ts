import { api } from "./client";
import type { RecordItem } from "./types";

export type RecordKind = "confidential" | "public";

export const recordsApi = (kind: RecordKind) => ({
  list: (query: string) =>
    api.get<RecordItem[]>(`/api/${kind}`, { params: query ? { query } : {} }).then((r) => r.data),

  create: (title: string, content: string) =>
    api.post<RecordItem>(`/api/${kind}`, { title, content }).then((r) => r.data),

  update: (id: number, title: string, content: string) =>
    api.put<RecordItem>(`/api/${kind}/${id}`, { title, content }).then((r) => r.data),

  remove: (id: number) => api.delete(`/api/${kind}/${id}`),
});
