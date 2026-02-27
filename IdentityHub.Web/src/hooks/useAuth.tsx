import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { authService } from '../services/authService';

interface AuthContextType {
  isAuthenticated: boolean;
  email: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(authService.isAuthenticated());
  const [email, setEmail] = useState(authService.getEmail());

  const login = useCallback(async (email: string, password: string) => {
    const response = await authService.login(email, password);
    setIsAuthenticated(true);
    setEmail(response.email);
  }, []);

  const register = useCallback(async (email: string, password: string) => {
    const response = await authService.register(email, password);
    setIsAuthenticated(true);
    setEmail(response.email);
  }, []);

  const logout = useCallback(() => {
    authService.logout();
    setIsAuthenticated(false);
    setEmail(null);
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated, email, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
}
