//string Name, string UserName, string FirstName, string LastName, string TenantCode, string Email, string Password
import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

import { apiClient } from "@/lib/apiClient";
import { useAuthStore } from "@/store/useAuthStore";
import { isAxiosError } from 'axios';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Button } from "@/components/ui/button";
import { Loader2 } from 'lucide-react';

import { API_ROUTES } from '@/lib/apiRoutes';


const orgRegisterSchema = z.object({
    name: z.string().min(3, "Name must be at least 3 characters"),
    userName: z.string().min(3, "Username must be at least 3 characters"),
    firstName: z.string().min(3, "First name must be at least 3 characters"),
    lastName: z.string().min(3, "Last name must be at least 3 characters"),
    tenantCode: z.string()
        .min(4, "Tenant code must be at least 4 characters")
        .regex(/^[a-z0-9-]+$/, "Tenant code may only contain lowercase letters, numbers, and hyphens")
        .refine(v => !v.startsWith("-") && !v.endsWith("-"), "Tenant code must not start or end with a hyphen"),
    email: z.string().email("Email is invalid"),
    password: z.string().min(8, "Password must be at least 8 characters"),
    confirmPassword: z.string().min(8, "Confirm password must be at least 8 characters"),
}).refine(data => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
});

export type OrgRegisterFormValues = z.infer<typeof orgRegisterSchema>;

export function OrgRegisterForm() {
    const [isLoading, setIsLoading] = useState(false);
    const [globalError, setGlobalError] = useState<string | null>(null);

    const login = useAuthStore((state) => state.login);

    const form = useForm<OrgRegisterFormValues>({
        resolver: zodResolver(orgRegisterSchema),
        defaultValues: {
            name: "",
            userName: "",
            firstName: "",
            lastName: "",
            tenantCode: "",
            email: "",
            password: "",
            confirmPassword: "",
        },
    });

    async function onSubmit(data:OrgRegisterFormValues) {
        setIsLoading(true);
        setGlobalError(null);

        try {
            const response = await apiClient.post(`${API_ROUTES.AUTH.REGISTER_ORG}`, {
                name: data.name,
                userName: data.userName,
                firstName: data.firstName,
                lastName: data.lastName,
                tenantCode: data.tenantCode,
                email: data.email,
                password: data.password,
            });

            login(
                response.data.user, 
                response.data.accessToken, 
                response.data.refreshToken,
                response.data.expiresOnUtc
            );
            
            window.location.href = "/dashboard";
        } catch (error : any) {
            if (isAxiosError(error) && error.response?.data) {
                setGlobalError(error.response?.data.detail || "An unexpected error occurred.");
            } else {
                setGlobalError("Unable to connect to the server. Please try again later.");
            }
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <div className="grid gap-6">
            <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">

                    <FormField
                        control={form.control}
                        name="name"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Organization Name</FormLabel>
                                <FormControl>
                                    <Input placeholder="Organization Name" {...field} disabled={isLoading} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="userName"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>User Name</FormLabel>
                                <FormControl>
                                    <Input placeholder="User Name" {...field} disabled={isLoading} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    <div className="grid grid-cols-2 gap-4">
                        <FormField
                            control={form.control}
                            name="firstName"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>First Name</FormLabel>
                                    <FormControl>
                                        <Input placeholder="First Name" {...field} disabled={isLoading} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="lastName"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Last Name</FormLabel>
                                    <FormControl>
                                        <Input placeholder="Last Name" {...field} disabled={isLoading} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </div>


                    <FormField
                        control={form.control}
                        name="tenantCode"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Tenant Code</FormLabel>
                                <FormControl>
                                    <Input placeholder="Tenant Code" {...field} disabled={isLoading} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="email"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Email</FormLabel>
                                <FormControl>
                                    <Input placeholder="Email" {...field} disabled={isLoading} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    <div className='grid grid-cols-2 gap-4'>
                        <FormField
                            control={form.control}
                            name="password"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Password</FormLabel>
                                    <FormControl>
                                        <Input type="password" placeholder="Password" {...field} disabled={isLoading} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="confirmPassword"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Confirm Password</FormLabel>
                                    <FormControl>
                                        <Input type="password" placeholder="Confirm Password" {...field} disabled={isLoading} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </div>

                    {globalError && (
                        <div className="text-sm font-medium text-destructive">
                            {globalError}
                        </div>
                    )}

                    <Button type="submit" className="w-full" disabled={isLoading}>
                        {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                        Sign Up
                    </Button>



                </form>
            </Form>
        </div>
    )
}