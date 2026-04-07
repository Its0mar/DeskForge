import { useState } from "react";
import { MoreHorizontal, UserMinus, ShieldAlert, Loader2 } from "lucide-react";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel,
  DropdownMenuSeparator, DropdownMenuTrigger, DropdownMenuSub,
  DropdownMenuSubContent, DropdownMenuSubTrigger, DropdownMenuRadioGroup, DropdownMenuRadioItem
} from "@/components/ui/dropdown-menu";

const ROLE_MAP: Record<string, number> = {
    "Manager": 1,
    "Staff": 2,
    "Requester": 3
};

interface Props {
    userId: string;
    currentRole: string;
    currentUserRole: string;
    onSuccess: () => void;
}

export function MemberActionsMenu({ userId, currentRole, currentUserRole, onSuccess }: Props) {
    const [isLoading, setIsLoading] = useState(false);

    const isRoleLocked = currentRole === "Requester";

    const availableRoles = currentUserRole === "Owner" 
        ? ["Manager", "Staff"] 
        : ["Staff"];

    async function handleRoleChange(newRole: string) {
        if (newRole === currentRole) return;
        setIsLoading(true);
        try {
            const payload = {
                role: ROLE_MAP[newRole]
            };
            await apiClient.put(API_ROUTES.ORGANIZATIONS.MEMBER_ROLE(userId), payload);
            onSuccess();
        } catch (error) {
            alert("Failed to update role"); 
        } finally {
            setIsLoading(false);
        }
    }

    async function handleRemove() {
        if (!confirm("Are you sure you want to remove this user from the workspace?")) return;
        setIsLoading(true);
        try {
            await apiClient.delete(API_ROUTES.ORGANIZATIONS.MEMBER_REMOVE(userId));
            onSuccess();
        } catch (error) {
            alert("Failed to remove user");
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" disabled={isLoading}>
                    {isLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <MoreHorizontal className="h-4 w-4" />}
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-48">
                <DropdownMenuLabel>Manage User</DropdownMenuLabel>
                <DropdownMenuSeparator />
                
                {!isRoleLocked && (
                    <DropdownMenuSub>
                        <DropdownMenuSubTrigger>
                            <ShieldAlert className="mr-2 h-4 w-4" />
                            <span>Change Role</span>
                        </DropdownMenuSubTrigger>
                        <DropdownMenuSubContent>
                            <DropdownMenuRadioGroup value={currentRole} onValueChange={handleRoleChange}>
                                {availableRoles.map(role => (
                                    <DropdownMenuRadioItem key={role} value={role}>
                                        {role}
                                    </DropdownMenuRadioItem>
                                ))}
                            </DropdownMenuRadioGroup>
                        </DropdownMenuSubContent>
                    </DropdownMenuSub>
                )}

                {!isRoleLocked && <DropdownMenuSeparator />}

                <DropdownMenuItem onClick={handleRemove} className="text-destructive focus:bg-destructive focus:text-destructive-foreground">
                    <UserMinus className="mr-2 h-4 w-4" />
                    <span>Remove from Workspace</span>
                </DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>
    );
}