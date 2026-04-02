import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Loader2 } from "lucide-react";
import { isAxiosError } from "axios";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";

import { apiClient } from "@/lib/apiClient";
import { useAuthStore } from "@/store/useAuthStore";

const loginSchema = z.object({
    identifier: z.string().min(3, "Email or username is required"),
    password: z.string().min(8, "Password must be at least 8 characters"),
})

type LoginFormValues = z.infer<typeof loginSchema>;

export function LoginForm() {
    const [isLoading, setIsLoading] = useState(false);
    const [globalError, setGlobalError] = useState<string | null>(null);

    const login = useAuthStore((state) => state.login);

    const form = useForm<LoginFormValues>({
        resolver: zodResolver(loginSchema),
        defaultValues : {identifier: "", password: ""}
    });

    async function onSubmit(data: LoginFormValues) {
        setIsLoading(true);
        setGlobalError(null);

        try {
            const response = await apiClient.post('/auth/login', {
                identifier: data.identifier,
                password: data.password
            });

            login(response.data.user);

            window.location.href = "/dashboard";
            
        } catch (error: any) {
            if (isAxiosError(error) && error.response?.data) {
                setGlobalError(error.response.data.detail || "An unexpected error occurred.");
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
                name="identifier"
                render={({ field }) => (
                <FormItem>
                    <FormLabel>Email or Username</FormLabel>
                    <FormControl>
                    <Input placeholder="name@example.com" {...field} disabled={isLoading} />
                    </FormControl>
                    <FormMessage />
                </FormItem>
                )}
            />

            <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                <FormItem>
                    <FormLabel>Password</FormLabel>
                    <FormControl>
                    <Input type="password" {...field} disabled={isLoading} />
                    </FormControl>
                    <div className="flex justify-end">
                    <a href="#" className="text-sm text-muted-foreground hover:underline">
                        Forgot password?
                    </a>
                    </div>
                    <FormMessage />
                </FormItem>
                )}
            />

            {globalError && (
                <div className="text-sm font-medium text-destructive">
                {globalError}
                </div>
            )}

            <Button type="submit" className="w-full" disabled={isLoading}>
                {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Sign In
            </Button>
            </form>
        </Form>
        </div>
    );
}