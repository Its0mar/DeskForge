import { useState } from "react";
import { useForm } from "react-hook-form";
import { useSearchParams } from "react-router-dom";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { isAxiosError } from "axios";
import { Loader2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";

import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";
import { useAuthStore } from "@/store/useAuthStore";

const acceptInviteSchema = z.object({
    userName: z.string().min(3, "Username must be at least 3 characters").max(30),
    firstName: z.string().min(3, "First name must be at least 3 characters").max(20),
    lastName: z.string().min(3, "Last name must be at least 3 characters").max(20),
    password: z.string().min(6, "Password must be at least 6 characters"),
    confirmPassword: z.string().min(6, "Confirm password is required"),
}).refine(data => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
});

type AcceptInviteFormValues = z.infer<typeof acceptInviteSchema>;

export function AcceptInviteForm() {
    const [searchParams] = useSearchParams();
    const token = searchParams.get("token");

    const [isLoading, setIsLoading] = useState(false);
    const [globalError, setGlobalError] = useState<string | null>(null);
    const login = useAuthStore((state) => state.login);

    const form = useForm<AcceptInviteFormValues>({
        resolver: zodResolver(acceptInviteSchema),
        defaultValues: {
            userName: "",
            firstName: "",
            lastName: "",
            password: "",
            confirmPassword: "",
        },
    });

    async function onSubmit(data: AcceptInviteFormValues) {
        setIsLoading(true);
        setGlobalError(null);

        if (!token) {
            setGlobalError("Invalid or missing invitation token. Please check your email link.");
            setIsLoading(false);
            return;
        }

        try {
            const response = await apiClient.post(API_ROUTES.AUTH.ACCEPT_INVITE, {
                token: token,
                userName: data.userName,
                firstName: data.firstName,
                lastName: data.lastName,
                password: data.password,
            });

            login(
                response.data.user,
                response.data.accessToken,
                response.data.refreshToken,
                response.data.expiresOnUtc
            );

            window.location.href = "/dashboard";
        } catch (error: any) {
            if (isAxiosError(error) && error.response?.data) {
                setGlobalError(error.response.data.detail || "Failed to accept invitation.");
            } else {
                setGlobalError("Unable to connect to the server. Please try again later.");
            }
        } finally {
            setIsLoading(false);
        }
    }

    if (!token) {
        return (
            <div className="text-center p-4 bg-destructive/10 text-destructive rounded-md border border-destructive/20">
                <p className="font-semibold">Invalid Invitation Link</p>
                <p className="text-sm mt-1">This link is missing a security token. Please click the exact link from your email.</p>
            </div>
        );
    }

    return (
        <div className="grid gap-6">
            <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                    <FormField
                        control={form.control}
                        name="userName"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Username</FormLabel>
                                <FormControl>
                                    <Input placeholder="johndoe123" {...field} disabled={isLoading} />
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
                                        <Input placeholder="John" {...field} disabled={isLoading} />
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
                                        <Input placeholder="Doe" {...field} disabled={isLoading} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <FormField
                            control={form.control}
                            name="password"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Password</FormLabel>
                                    <FormControl>
                                        <Input type="password" {...field} disabled={isLoading} />
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
                                        <Input type="password" {...field} disabled={isLoading} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </div>

                    {globalError && (
                        <div className="text-sm font-medium text-destructive text-center">
                            {globalError}
                        </div>
                    )}

                    <Button type="submit" className="w-full" disabled={isLoading}>
                        {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                        Complete Account Setup
                    </Button>
                </form>
            </Form>
        </div>
    );
}