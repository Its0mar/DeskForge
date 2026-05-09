import { useState, useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Loader2, FolderPlus, AlertTriangle, CheckCircle2 } from "lucide-react";
import { isAxiosError } from "axios";

import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";
import { useAuthStore } from "@/store/useAuthStore";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage, FormDescription } from "@/components/ui/form";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

// 1. Zod Schema strictly matching your C# FluentValidation rules
const createCategorySchema = z.object({
    name: z.string()
        .min(3, "Category name must be at least 3 characters.")
        .max(30, "Category name cannot exceed 30 characters."),
    // Description is optional, but if provided, must be 15-300 chars
    description: z.string()
        .max(300, "Description cannot exceed 300 characters.")
        .optional()
        .refine(val => !val || val.length >= 15, {
            message: "Description must be at least 15 characters long if provided."
        }),
    targetTeamId: z.string().min(1, "Please select a target team."),
});

type CreateCategoryValues = z.infer<typeof createCategorySchema>;

interface Team {
    teamId: string;
    teamName: string;
}

export default function CreateCategoryPage() {
    const { user } = useAuthStore();
    
    const [teams, setTeams] = useState<Team[]>([]);
    const [isLoadingTeams, setIsLoadingTeams] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);
    
    // Status states
    const [serverError, setServerError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    // RBAC Enforcement: Must be Owner or Manager
    const isAuthorized = ["Owner", "Manager"].includes(user?.role || "");

    const form = useForm<CreateCategoryValues>({
        resolver: zodResolver(createCategorySchema),
        defaultValues: {
            name: "",
            description: "",
            targetTeamId: "",
        },
    });

    // 2. Fetch Teams for the Dropdown
    useEffect(() => {
        if (!isAuthorized) return;

        async function fetchTeams() {
            try {
                const response = await apiClient.get<Team[]>(API_ROUTES.TEAMS.BASE);
                setTeams(response.data);
            } catch (err) {
                setServerError("Failed to load teams. Please refresh the page.");
            } finally {
                setIsLoadingTeams(false);
            }
        }
        fetchTeams();
    }, [isAuthorized]);

    // 3. Handle Form Submission
    async function onSubmit(data: CreateCategoryValues) {
        setIsSubmitting(true);
        setServerError(null);
        setSuccessMessage(null);

        try {
            // Send the exact payload expected by CreateCategoryCommand
            await apiClient.post(API_ROUTES.CATEGORIES.CREATE, {
                name: data.name,
                description: data.description || null, // Convert empty string to null for C#
                targetTeamId: data.targetTeamId
            });

            setSuccessMessage(`Category "${data.name}" was created successfully!`);
            form.reset(); // Clear the form for the next entry
            
        } catch (err) {
            if (isAxiosError(err) && err.response) {
                // Catch the exact 404 and 409 status codes from your ValidateAsync method
                if (err.response.status === 409) {
                    form.setError("name", { type: "server", message: "A category with this name already exists in the selected team." });
                } else if (err.response.status === 404) {
                    setServerError("The selected team no longer exists.");
                } else {
                    setServerError(err.response.data?.detail || "An unexpected error occurred.");
                }
            } else {
                setServerError("Network error. Please check your connection.");
            }
        } finally {
            setIsSubmitting(false);
        }
    }

    // --- ACCESS DENIED STATE ---
    if (!isAuthorized) {
        return (
            <div className="p-8 max-w-3xl mx-auto mt-12 text-center">
                <AlertTriangle className="w-12 h-12 text-destructive mx-auto mb-4" />
                <h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
                <p className="text-muted-foreground mt-2">Only Workspace Owners and Managers can create request categories.</p>
            </div>
        );
    }

    // --- MAIN UI RENDER ---
    return (
        <div className="p-8 max-w-3xl mx-auto space-y-8">
            <div>
                <h1 className="text-3xl font-bold tracking-tight">Create Category</h1>
                <p className="text-muted-foreground">Define a new request category and route it to a specific team.</p>
            </div>

            <Card className="shadow-sm">
                <CardHeader>
                    <div className="flex items-center gap-2">
                        <FolderPlus className="w-5 h-5 text-primary" />
                        <CardTitle>Category Details</CardTitle>
                    </div>
                    <CardDescription>
                        Categories help requesters organize their tickets and automatically route them to the correct department.
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <Form {...form}>
                        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                            
                            {/* Target Team Dropdown */}
                            <FormField
                                control={form.control}
                                name="targetTeamId"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Routing Team <span className="text-destructive">*</span></FormLabel>
                                        <Select 
                                            disabled={isSubmitting || isLoadingTeams} 
                                            onValueChange={field.onChange} 
                                            value={field.value}
                                        >
                                            <FormControl>
                                                <SelectTrigger>
                                                    <SelectValue placeholder={isLoadingTeams ? "Loading teams..." : "Select the team to handle these requests"} />
                                                </SelectTrigger>
                                            </FormControl>
                                            <SelectContent>
                                                {teams.map(team => (
                                                    <SelectItem key={team.teamId} value={team.teamId}>
                                                        {team.teamName}
                                                    </SelectItem>
                                                ))}
                                                {teams.length === 0 && !isLoadingTeams && (
                                                    <SelectItem value="none" disabled>No teams found in this workspace.</SelectItem>
                                                )}
                                            </SelectContent>
                                        </Select>
                                        <FormDescription>
                                            Tickets created under this category will be automatically assigned to this team.
                                        </FormDescription>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Category Name */}
                            <FormField
                                control={form.control}
                                name="name"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Category Name <span className="text-destructive">*</span></FormLabel>
                                        <FormControl>
                                            <Input placeholder="e.g. Hardware Issues, Billing Inquiry" {...field} disabled={isSubmitting} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Optional Description */}
                            <FormField
                                control={form.control}
                                name="description"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Description (Optional)</FormLabel>
                                        <FormControl>
                                            <Textarea 
                                                placeholder="Provide guidance to requesters on when to use this category (min 15 characters)..." 
                                                className="resize-none"
                                                {...field} 
                                                disabled={isSubmitting} 
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Status Messages */}
                            {serverError && (
                                <div className="text-sm font-medium text-destructive flex items-center gap-2 bg-destructive/10 p-3 rounded-md">
                                    <AlertTriangle className="w-4 h-4" /> {serverError}
                                </div>
                            )}
                            
                            {successMessage && (
                                <div className="text-sm font-medium text-green-600 flex items-center gap-2 bg-green-50 p-3 rounded-md border border-green-200">
                                    <CheckCircle2 className="w-4 h-4" /> {successMessage}
                                </div>
                            )}

                            {/* Submit Button */}
                            <Button type="submit" disabled={isSubmitting || isLoadingTeams}>
                                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                                Create Category
                            </Button>
                        </form>
                    </Form>
                </CardContent>
            </Card>
        </div>
    );
}