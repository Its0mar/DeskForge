export const API_ROUTES = {
    AUTH: {
        LOGIN: '/auth/login',
        REFRESH: '/auth/refresh',
        REGISTER_ORG: '/auth/organization/register',
        REGISTER_REQUESTER: (orgSlug: string) => `/${orgSlug}/register`,
        ACCEPT_INVITE: '/auth/invites/accept',
    },
    USERS: {
        ME: '/auth/profile',
    },
    ORGANIZATIONS: {
        MINE: '/organizations',
        MEMBERS: (page: number, excludeRequesters: boolean) => 
            `/organizations/members?pageNumber=${page}&pageSize=10${excludeRequesters ? '&excludeRole=Requester' : '&role=Requester'}`,
        MEMBER_ROLE: (userId: string) => `/organizations/members/${userId}/role`,
        MEMBER_REMOVE: (userId: string) => `/organizations/members/${userId}`,
        INVITE : `/organizations/invite-employee`,
    }
} as const