import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Loader2, Shield, AlertTriangle, Building2 } from "lucide-react";

import { useAuthStore } from "@/store/useAuthStore";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";

import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import {
    AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent,
    AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

const updateProfileSchema = z.object({
    firstName: z.string().min(2, "First name must be at least 2 characters"),
    lastName: z.string().min(2, "Last name must be at least 2 characters"),
});

type UpdateProfileValues = z.infer<typeof updateProfileSchema>;

export default function ProfilePage() {
    const { user, login, logout } = useAuthStore();

    const [isUpdating, setIsUpdating] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);
    const [updateMessage, setUpdateMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);

    const form = useForm<UpdateProfileValues>({
        resolver: zodResolver(updateProfileSchema),
        defaultValues: {
            firstName: user?.firstName || "",
            lastName: user?.lastName || "",
        },
    });

    if (!user) return <div className="p-8 text-center text-muted-foreground">Loading profile...</div>;

    // --- HANDLERS ---

    async function onUpdateProfile(data: UpdateProfileValues) {
        setIsUpdating(true);
        setUpdateMessage(null);

        try {
            //TODO : Send the PUT request to backend
            const response = await apiClient.put(API_ROUTES.USERS.ME, {
                firstName: data.firstName,
                lastName: data.lastName
            });

            // If successful, update the Zustand store so the Navbar updates instantly!
            //TODO : backend returns the updated user object
            login(response.data, localStorage.getItem('accessToken')!, localStorage.getItem('refreshToken')!, "todo-expires");

            setUpdateMessage({ type: 'success', text: 'Profile updated successfully.' });
        } catch (error) {
            setUpdateMessage({ type: 'error', text: 'Failed to update profile. Please try again.' });
        } finally {
            setIsUpdating(false);
        }
    }

    async function onDeleteAccount() {
        setIsDeleting(true);
        try {
            //TODO : Send the DELETE request to backend
            await apiClient.delete(API_ROUTES.USERS.ME);

            // Wipe the tokens and kick them out!
            logout();
        } catch (error) {
            setUpdateMessage({ type: 'error', text: 'Failed to delete account. Please contact support.' });
            setIsDeleting(false);
        }
    }

    // --- UI RENDER ---

    return (
        <div className="p-8 max-w-3xl mx-auto space-y-8">
            <div>
                <h1 className="text-3xl font-bold tracking-tight">Account Settings</h1>
                <p className="text-muted-foreground">Manage your profile, organization, and security preferences.</p>
            </div>

            {/* SECTION 1: Organization Info (Read Only) */}
            <Card>
                <CardHeader>
                    <div className="flex items-center gap-2">
                        <Building2 className="w-5 h-5 text-primary" />
                        <CardTitle>Organization Details</CardTitle>
                    </div>
                </CardHeader>
                <CardContent className="grid grid-cols-2 gap-4">
                    <div className="space-y-1">
                        <Label className="text-muted-foreground">Workspace / Tenant Code</Label>
                        <p className="font-medium text-lg">{user.tenantCode}</p>
                    </div>
                    <div className="space-y-1">
                        <Label className="text-muted-foreground">Your Role</Label>
                        <div>
                            <Badge variant={user.role === "Admin" ? "default" : "secondary"}>
                                <Shield className="w-3 h-3 mr-1" />
                                {user.role}
                            </Badge>
                        </div>
                    </div>
                </CardContent>
            </Card>

            {/* SECTION 2: Editable Profile */}
            <Card>
                <CardHeader>
                    <CardTitle>Personal Information</CardTitle>
                    <CardDescription>Update your personal details here.</CardDescription>
                </CardHeader>
                <CardContent>
                    <Form {...form}>
                        <form onSubmit={form.handleSubmit(onUpdateProfile)} className="space-y-4">

                            {/* Uneditable fields */}
                            <div className="grid grid-cols-2 gap-4 pb-4 border-b">
                                <div className="space-y-2">
                                    <Label>Username</Label>
                                    <Input defaultValue={user.username} disabled className="bg-muted/50" />
                                </div>
                                <div className="space-y-2">
                                    <Label>Email</Label>
                                    <Input defaultValue={user.email} disabled className="bg-muted/50" />
                                </div>
                            </div>

                            {/* Editable fields */}
                            <div className="grid grid-cols-2 gap-4 pt-2">
                                <FormField
                                    control={form.control}
                                    name="firstName"
                                    render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>First Name</FormLabel>
                                            <FormControl>
                                                <Input {...field} disabled={isUpdating} />
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
                                                <Input {...field} disabled={isUpdating} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            </div>

                            {updateMessage && (
                                <div className={`text-sm font-medium ${updateMessage.type === 'success' ? 'text-green-600' : 'text-destructive'}`}>
                                    {updateMessage.text}
                                </div>
                            )}

                            <Button type="submit" disabled={isUpdating || !form.formState.isDirty}>
                                {isUpdating && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                                Save Changes
                            </Button>
                        </form>
                    </Form>
                </CardContent>
            </Card>

            <Card className="border-destructive/50 shadow-sm">
                <CardHeader>
                    <div className="flex items-center gap-2 text-destructive">
                        <AlertTriangle className="w-5 h-5" />
                        <CardTitle>Danger Zone</CardTitle>
                    </div>
                    <CardDescription>
                        Permanently remove your account and all associated data. This action cannot be undone.
                    </CardDescription>
                </CardHeader>
                <CardFooter>
                    <AlertDialog>
                        <AlertDialogTrigger asChild>
                            <Button variant="destructive" disabled={isDeleting}>
                                {isDeleting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : "Delete Account"}
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    This action cannot be undone. This will permanently delete your account
                                    and remove your personal data from our servers.
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                                <AlertDialogCancel>Cancel</AlertDialogCancel>
                                <AlertDialogAction onClick={onDeleteAccount} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                                    Yes, delete my account
                                </AlertDialogAction>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
                </CardFooter>
            </Card>
        </div>
    );
}