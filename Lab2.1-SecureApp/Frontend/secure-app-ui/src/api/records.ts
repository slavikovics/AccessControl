import { api } from "./client";
import type { RecordItem } from "./types";

export type RecordKind = "confidential" | "public" | "memory-confidential" | "memory-public";

const PATHS: Record<RecordKind, string> = {
  confidential: "/api/confidential",
  public: "/api/public",
  "memory-confidential": "/api/memory/confidential",
  "memory-public": "/api/memory/public",
};

export const recordsApi = (kind: RecordKind) => {
  const base = PATHS[kind];
  return {
    list: (query: string) =>
      api.get<RecordItem[]>(base, { params: query ? { query } : {} }).then((r) => r.data),

    create: (title: string, content: string) =>
      api.post<RecordItem>(base, { title, content }).then((r) => r.data),

    update: (id: number, title: string, content: string) =>
      api.put<RecordItem>(`${base}/${id}`, { title, content }).then((r) => r.data),

    remove: (id: number) => api.delete(`${base}/${id}`),
  };
};
