import { useState, useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Building2, Hash, Loader2 } from "lucide-react";

import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";
import { useAuthStore } from "@/store/useAuthStore";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";

interface OrganizationDetails {
    id: string;
    name: string;
    tenantCode: string;
}

const updateOrgSchema = z.object({
    name: z.string().min(3, "Organization name must be at least 3 characters"),
});

type UpdateOrgValues = z.infer<typeof updateOrgSchema>;

export default function OrganizationSettingsPage() {
    const { user, login } = useAuthStore();
    
    const [orgData, setOrgData] = useState<OrganizationDetails | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isUpdating, setIsUpdating] = useState(false);
    const [updateMessage, setUpdateMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);

    const canManageOrg = ["Owner", "Manager"].includes(user?.role || "");

    const form = useForm<UpdateOrgValues>({
        resolver: zodResolver(updateOrgSchema),
        defaultValues: { name: "" },
    });

    useEffect(() => {
        async function fetchOrganization() {
            try {
                const response = await apiClient.get<OrganizationDetails>(API_ROUTES.ORGANIZATIONS.MINE);
                setOrgData(response.data);
                form.reset({ name: response.data.name });
            } catch (err) {
                setUpdateMessage({ type: 'error', text: "Failed to load organization details." });
            } finally {
                setIsLoading(false);
            }
        }
        fetchOrganization();
    }, [form]);

    // --- HANDLERS ---
    async function onUpdateOrganization(data: UpdateOrgValues) {
        if (!canManageOrg) return;
        
        setIsUpdating(true);
        setUpdateMessage(null);

        try {
            await apiClient.put(API_ROUTES.ORGANIZATIONS.MINE, {
                name: data.name
            });
            
            setUpdateMessage({ type: 'success', text: 'Organization updated successfully.' });
            
            if (user) {
                login({ ...user, orgName: data.name }, localStorage.getItem('accessToken')!, localStorage.getItem('refreshToken')!, "todo-expires");
            }

        } catch (error) {
            setUpdateMessage({ type: 'error', text: 'Failed to update organization.' });
        } finally {
            setIsUpdating(false);
        }
    }

    // --- UI RENDER ---
    if (isLoading) {
        return (
            <div className="p-8 flex justify-center items-center h-[50vh]">
                <Loader2 className="h-8 w-8 animate-spin text-primary" />
            </div>
        );
    }

    if (!orgData) return <div className="p-8 text-center text-destructive">Error loading data.</div>;

    return (
        <div className="p-8 max-w-3xl mx-auto space-y-8">
            <div>
                <h1 className="text-3xl font-bold tracking-tight">Organization Settings</h1>
                <p className="text-muted-foreground">View and manage your workspace details.</p>
            </div>

            <Card>
                <CardHeader>
                    <div className="flex items-center gap-2">
                        <Building2 className="w-5 h-5 text-primary" />
                        <CardTitle>Workspace Profile</CardTitle>
                    </div>
                    <CardDescription>
                        {canManageOrg 
                            ? "Update your organization's core details below." 
                            : "These are the core details for your current organization."}
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                    
                    <Form {...form}>
                        <form onSubmit={form.handleSubmit(onUpdateOrganization)} className="space-y-6">
                            
                            {/* Editable Name Field */}
                            <FormField
                                control={form.control}
                                name="name"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Organization Name</FormLabel>
                                        <FormControl>
                                            {/* Disable input if they are just a Requester OR if a save is in progress */}
                                            <Input {...field} disabled={!canManageOrg || isUpdating} className={!canManageOrg ? "bg-muted/50" : ""} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Status Message */}
                            {updateMessage && (
                                <div className={`text-sm font-medium ${updateMessage.type === 'success' ? 'text-green-600' : 'text-destructive'}`}>
                                    {updateMessage.text}
                                </div>
                            )}

                            {canManageOrg && (
                                <Button type="submit" disabled={isUpdating || !form.formState.isDirty}>
                                    {isUpdating && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                                    Save Changes
                                </Button>
                            )}
                        </form>
                    </Form>

                    {canManageOrg && (
                        <div className="space-y-2 pt-6 border-t">
                            <Label htmlFor="tenantCode" className="flex items-center gap-1">
                                <Hash className="w-3 h-3" />
                                Tenant Routing Code
                            </Label>
                            <Input 
                                id="tenantCode" 
                                defaultValue={orgData.tenantCode} 
                                readOnly 
                                disabled
                                className="bg-muted/50 font-mono text-sm"
                            />
                            <p className="text-xs text-muted-foreground mt-1">
                                Used for API routing and custom login URLs. Cannot be changed.
                            </p>
                        </div>
                    )}

                </CardContent>
            </Card>
        </div>
    );
}