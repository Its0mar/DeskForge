import axios from 'axios';

export const apiClient = axios.create({
    baseURL: 'http://localhost:5098/api', 
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
        const isAuthEndpoint = originalRequest.url?.includes('/auth/login') || originalRequest.url?.includes('/auth/refresh');
        if (error.response?.status === 401 && !originalRequest._retry && !isAuthEndpoint) {
            originalRequest._retry = true;

            try {
                const response = await axios.post('http://localhost:5098/api/auth/refresh', {
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