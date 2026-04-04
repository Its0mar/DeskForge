export const API_ROUTES = {
    AUTH: {
        LOGIN: '/auth/login',
        REFRESH: '/auth/refresh',
        REGISTER_ORG: '/auth/organization/register',
        REGISTER_REQUESTER: (orgSlug: string) => `/${orgSlug}/register`,
        ACCEPT_INVITE: '/auth/invites/accept',
    }
} as const