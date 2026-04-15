import { useState, useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Loader2, UserPlus } from "lucide-react";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";

import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { PaginatedResult } from "@/types/pagination";

const addMemberSchema = z.object({
    userId: z.string().min(1, "Please select a user"),
});

type AddMemberValues = z.infer<typeof addMemberSchema>;

interface Props {
    isOpen: boolean;
    teamId: string | null;
    teamName: string;
    onOpenChange: (open: boolean) => void;
    onSuccess: () => void;
}

export function AddTeamMemberDialog({ isOpen, teamId, teamName, onOpenChange, onSuccess }: Props) {
    const [isAdding, setIsAdding] = useState(false);
    const [users, setUsers] = useState<{id: string, firstName: string, lastName: string}[]>([]);
    
    const form = useForm<AddMemberValues>({
        resolver: zodResolver(addMemberSchema),
        defaultValues: { userId: "" },
    });

    // Fetch internal staff to populate the dropdown when the modal opens
    useEffect(() => {
        if (isOpen) {
            apiClient.get<PaginatedResult<any>>(API_ROUTES.ORGANIZATIONS.MEMBERS(1, true))
                .then(res => setUsers(res.data.items))
                .catch(console.error);
        } else {
            form.reset(); // Clear form when closed
        }
    }, [isOpen, form]);

    async function onSubmit(data: AddMemberValues) {
        if (!teamId) return;
        setIsAdding(true);
        try {
            // Matches: AddTeamMemberCommand(Guid UserId, Guid TeamId)
            await apiClient.post(API_ROUTES.TEAMS.ADD_MEMBER, {
                userId: data.userId,
                teamId: teamId
            });
            onSuccess();
            onOpenChange(false);
        } catch (err: any) {
            alert(err.response?.data?.detail || "Failed to add member.");
        } finally {
            setIsAdding(false);
        }
    }

    return (
        <Dialog open={isOpen} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Add to {teamName}</DialogTitle>
                    <DialogDescription>Select a user to add to this team.</DialogDescription>
                </DialogHeader>
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="userId"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Select User</FormLabel>
                                    <Select disabled={isAdding || users.length === 0} onValueChange={field.onChange} value={field.value}>
                                        <FormControl>
                                            <SelectTrigger>
                                                <SelectValue placeholder={users.length === 0 ? "Loading users..." : "Select a team member"} />
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent>
                                            {users.map(u => (
                                                <SelectItem key={u.id} value={u.id}>
                                                    {u.firstName} {u.lastName}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <Button type="submit" className="w-full" disabled={isAdding}>
                            {isAdding ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <><UserPlus className="mr-2 h-4 w-4"/> Add Member</>}
                        </Button>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}