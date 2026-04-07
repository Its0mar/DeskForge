import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Loader2, Mail } from "lucide-react";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

const ROLE_MAP: Record<string, number> = {
    "Manager": 1,
    "Staff": 2,
    "Requester": 3
};

const inviteSchema = z.object({
    email: z.string().email("Invalid email address"),
    role: z.string().min(1, "Please select a role"),
});

type InviteValues = z.infer<typeof inviteSchema>;

interface Props {
    isOpen: boolean;
    onOpenChange: (open: boolean) => void;
    onSuccess: () => void;
    currentUserRole: string;
}

export function InviteMemberDialog({ isOpen, onOpenChange, onSuccess, currentUserRole }: Props) {
    const [isInviting, setIsInviting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const form = useForm<InviteValues>({
        resolver: zodResolver(inviteSchema),
        defaultValues: { email: "", role: "Requester" },
    });

    const availableRoles = currentUserRole === "Owner" 
        ? ["Manager", "Staff", "Requester"] 
        : ["Staff", "Requester"];

    async function onSubmit(data: InviteValues) {
        setIsInviting(true);
        setError(null);
        try {
            const payload = {
                email: data.email,
                role: ROLE_MAP[data.role]
            };
            await apiClient.post(API_ROUTES.ORGANIZATIONS.INVITE, payload);
            form.reset();
            onSuccess();
            onOpenChange(false);
        } catch (err: any) {
            setError(err.response?.data?.detail || "Failed to send invitation.");
        } finally {
            setIsInviting(false);
        }
    }

    return (
        <Dialog open={isOpen} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Invite Team Member</DialogTitle>
                    <DialogDescription>
                        Send an email invitation to add someone to your workspace.
                    </DialogDescription>
                </DialogHeader>
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="email"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Email Address</FormLabel>
                                    <FormControl>
                                        <Input placeholder="colleague@company.com" {...field} disabled={isInviting} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="role"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Workspace Role</FormLabel>
                                    <Select disabled={isInviting} onValueChange={field.onChange} defaultValue={field.value}>
                                        <FormControl>
                                            <SelectTrigger>
                                                <SelectValue placeholder="Select a role" />
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent>
                                            {availableRoles.map(role => (
                                                <SelectItem key={role} value={role}>{role}</SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        {error && <div className="text-sm font-medium text-destructive">{error}</div>}
                        <Button type="submit" className="w-full" disabled={isInviting}>
                            {isInviting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <><Mail className="mr-2 h-4 w-4"/> Send Invite</>}
                        </Button>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}