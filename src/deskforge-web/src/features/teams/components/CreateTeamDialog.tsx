import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import { Loader2, Users } from "lucide-react";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";

const createTeamSchema = z.object({
    name: z.string().min(2, "Team name must be at least 2 characters"),
    description: z.string().optional(),
});

type CreateTeamValues = z.infer<typeof createTeamSchema>;

interface Props {
    isOpen: boolean;
    onOpenChange: (open: boolean) => void;
    onSuccess: () => void; // Trigger a table refresh!
}

export function CreateTeamDialog({ isOpen, onOpenChange, onSuccess }: Props) {
    const [isCreating, setIsCreating] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const form = useForm<CreateTeamValues>({
        resolver: zodResolver(createTeamSchema),
        defaultValues: { name: "", description: "" },
    });

    async function onSubmit(data: CreateTeamValues) {
        setIsCreating(true);
        setError(null);
        try {
            // Matches: CreateTeamCommand(string Name, string? Description)
            await apiClient.post(API_ROUTES.TEAMS.BASE, data);
            form.reset();
            onSuccess();
            onOpenChange(false);
        } catch (err: any) {
            setError(err.response?.data?.detail || "Failed to create team.");
        } finally {
            setIsCreating(false);
        }
    }

    return (
        <Dialog open={isOpen} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Create New Team</DialogTitle>
                    <DialogDescription>Group users together to manage permissions and assignments.</DialogDescription>
                </DialogHeader>
                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        <FormField
                            control={form.control}
                            name="name"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Team Name</FormLabel>
                                    <FormControl><Input placeholder="e.g. Tier 1 Support" {...field} disabled={isCreating} /></FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="description"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Description (Optional)</FormLabel>
                                    <FormControl><Input placeholder="Handles general inquiries" {...field} disabled={isCreating} /></FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                        {error && <div className="text-sm font-medium text-destructive">{error}</div>}
                        <Button type="submit" className="w-full" disabled={isCreating}>
                            {isCreating ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <><Users className="mr-2 h-4 w-4"/> Create Team</>}
                        </Button>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    );
}