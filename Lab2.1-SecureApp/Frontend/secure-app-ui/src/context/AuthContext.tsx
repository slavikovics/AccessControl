import { createContext, useCallback, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import { api, errorMessage } from "../api/client";
import type { UserInfo } from "../api/types";

interface AuthContextValue {
  user: UserInfo | null;
  loading: boolean;
  login: (username: string, password: string) => Promise<void>;
  register: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get<UserInfo>("/api/auth/me")
      .then((r) => setUser(r.data))
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  const login = useCallback(async (username: string, password: string) => {
    try {
      const res = await api.post<UserInfo>("/api/auth/login", { username, password });
      setUser(res.data);
    } catch (err) {
      throw new Error(errorMessage(err, "Login failed."));
    }
  }, []);

  const register = useCallback(async (username: string, password: string) => {
    try {
      await api.post("/api/auth/register", { username, password });
    } catch (err) {
      throw new Error(errorMessage(err, "Registration failed."));
    }
  }, []);

  const logout = useCallback(async () => {
    await api.post("/api/auth/logout").catch(() => undefined);
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
