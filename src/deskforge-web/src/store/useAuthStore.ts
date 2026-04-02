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
  login: (user: UserInfo) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: JSON.parse(localStorage.getItem('userInfo') || 'null'),
  isAuthenticated: !!localStorage.getItem('accessToken'),

  login: (user) => {
    localStorage.setItem('userInfo', JSON.stringify(user));
    set({ user, isAuthenticated: true });
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('userInfo');
    
    set({ user: null, isAuthenticated: false });
    
    window.location.href = '/login'; 
  },
}));