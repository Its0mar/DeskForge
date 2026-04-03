import axios from 'axios';
import { API_ROUTES } from './apiRoutes';

const API_BASE_URL = import.meta.env.VITE_API_URL;

export const apiClient = axios.create({
    baseURL: API_BASE_URL, 
    headers: {
        'Content-Type': 'application/json',
    }
});

apiClient.interceptors.request.use((config) => { 
    const token = localStorage.getItem("accessToken");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

apiClient.interceptors.response.use(
    (response) => response,

    async (error) => {
        const originalRequest = error.config;
        const isAuthEndpoint = 
            originalRequest.url?.includes(API_ROUTES.AUTH.LOGIN) || 
            originalRequest.url?.includes(API_ROUTES.AUTH.REFRESH);
        if (error.response?.status === 401 && !originalRequest._retry && !isAuthEndpoint) {
            originalRequest._retry = true;

            try {
                const response = await axios.post(`${API_BASE_URL}${API_ROUTES.AUTH.REFRESH}`, {
                    accessToken: localStorage.getItem('accessToken'),
                    refreshToken: localStorage.getItem('refreshToken')
                });
                
                localStorage.setItem('accessToken', response.data.accessToken);
                localStorage.setItem('refreshToken', response.data.refreshToken);

                originalRequest.headers.Authorization = `Bearer ${response.data.accessToken}`;
                return apiClient(originalRequest);
                
            } catch (refreshError) {
                localStorage.removeItem('accessToken');
                localStorage.removeItem('refreshToken');
                window.location.href = '/login';
                return Promise.reject(refreshError);
            }
        }
        return Promise.reject(error);
    }
);