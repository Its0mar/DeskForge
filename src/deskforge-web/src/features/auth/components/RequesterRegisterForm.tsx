import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";
import { useAuthStore } from "@/store/useAuthStore";
import { zodResolver } from "@hookform/resolvers/zod";
import { isAxiosError } from "axios";
import { Loader2 } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useParams } from "react-router-dom";
import { z } from "zod";


const requesterRegisterSchema = z.object({
    userName : z.string().min(3, "Username must be at least 3 characters"),
    email : z.string().email("Email is invalid"),
    firstName : z.string().min(3, "First name must be at least 3 characters"),
    lastName : z.string().min(3, "Last name must be at least 3 characters"),
    password : z.string().min(6, "Password must be at least 6 characters"),
    confirmPassword : z.string().min(6, "Confirm password must be at least 6 characters"),
}).refine(data => data.password === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
});

export type RequesterRegisterFormValues = z.infer<typeof requesterRegisterSchema>;

export function RequesterRegisterForm() {
    const [isLoading, setIsLoading] = useState(false);
    const [globalError, setGlobalError] = useState<string | null>(null);
    const {orgSlug} = useParams<{orgSlug: string}>();

    const login = useAuthStore((state) => state.login);

    const form = useForm<RequesterRegisterFormValues>({
        resolver: zodResolver(requesterRegisterSchema),
        defaultValues: {
            userName: "",
            email: "",
            firstName: "",
            lastName: "",
            password: "",
            confirmPassword: "",
        },
    });

    async function onSubmit(data:RequesterRegisterFormValues) {
        setIsLoading(true);
        setGlobalError(null);

        if (!orgSlug) {
            setGlobalError("Organization slug is missing from the URL.");
            setIsLoading(false);
            return;
        }

        try {
                const response = await apiClient.post(`${API_ROUTES.AUTH.REGISTER_REQUESTER(orgSlug)}`, {
                    userName: data.userName,
                    email: data.email,
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
                        name="userName"
                        render={({field}) => (
                            <FormItem>
                                <FormLabel>User Name</FormLabel>
                                <FormControl>
                                    <Input placeholder="Joe" {...field} disabled={isLoading} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="email"
                        render={({field}) => (
                            <FormItem>
                                <FormLabel>Email</FormLabel>
                                <FormControl>
                                    <Input placeholder="[EMAIL_ADDRESS]" {...field} disabled={isLoading} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    <div className="grid grid-cols-2 gap-4">
                        <FormField
                            control={form.control}
                            name="firstName"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>First Name</FormLabel>
                                    <FormControl>
                                        <Input placeholder="Joe" {...field} disabled={isLoading} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="lastName"
                            render={({field}) => (
                                <FormItem>
                                    <FormLabel>Last Name</FormLabel>
                                    <FormControl>
                                        <Input placeholder="Smith" {...field} disabled={isLoading} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </div>

                    <FormField
                        control={form.control}
                        name="password"
                        render={({field}) => (
                            <FormItem>
                                <FormLabel>Password</FormLabel>
                                <FormControl>
                                    <Input placeholder="Password" type="password" {...field} disabled={isLoading} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="confirmPassword"
                        render={({field}) => (
                            <FormItem>
                                <FormLabel>Confirm Password</FormLabel>
                                <FormControl>
                                    <Input placeholder="Confirm Password" type="password" {...field} disabled={isLoading} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    {globalError && (
                        <div className="text-red-500 text-sm">{globalError}</div>
                    )}

                    <Button type="submit" className="w-full" disabled={isLoading}>
                        {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                        Register
                    </Button>
                </form>
            </Form>
        </div>
    )
}


