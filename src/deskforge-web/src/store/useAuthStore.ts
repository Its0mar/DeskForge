import { create } from 'zustand';

export interface UserInfo {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

interface AuthState {
  user: UserInfo | null;
  isAuthenticated: boolean;
  login: (user: UserInfo, accessToken: string, refreshToken: string, expiresOnUtc: string) => void;
  logout: () => void;
}

function isTokenValid(): boolean {
  const expiry = localStorage.getItem('tokenExpiresOnUtc');
  if (!expiry || !localStorage.getItem('accessToken')) return false;
  return new Date(expiry) > new Date();
}

export const useAuthStore = create<AuthState>((set) => ({
  user: JSON.parse(localStorage.getItem('userInfo') || 'null'),
  isAuthenticated: isTokenValid(),

  login: (user, accessToken, refreshToken, expiresOnUtc) => {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('tokenExpiresOnUtc', expiresOnUtc);
    localStorage.setItem('userInfo', JSON.stringify(user));
    set({ user, isAuthenticated: true });
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('tokenExpiresOnUtc');
    localStorage.removeItem('userInfo');
    
    set({ user: null, isAuthenticated: false });
    
    window.location.href = '/login'; 
  },
}));