import { useState, useEffect, useCallback } from "react";
import { Users, Shield, Loader2, AlertTriangle, UserCog, MoreHorizontal, ChevronLeft, ChevronRight } from "lucide-react";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";
import { useAuthStore } from "@/store/useAuthStore";
import type { PaginatedResult } from "@/types/pagination";

import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

import { InviteMemberDialog } from "../components/InviteMemberDialog";
import { MemberActionsMenu } from "../components/MemberActionsMenu";

interface Member {
    id: string; userName: string; email: string; firstName: string; lastName: string; role: string;
}

export default function MembersPage() {
    const { user } = useAuthStore();
    
    // Pagination State
    const [currentPage, setCurrentPage] = useState(1);
    const [data, setData] = useState<PaginatedResult<Member> | null>(null);
    
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isInviteOpen, setIsInviteOpen] = useState(false);

    const canAccessPage = user?.role !== "Requester";
    const canInviteUsers = ["Owner", "Manager"].includes(user?.role || "");

    const fetchMembers = useCallback(async (page: number) => {
        setIsLoading(true);
        try {
            const response = await apiClient.get<PaginatedResult<Member>>(API_ROUTES.ORGANIZATIONS.MEMBERS(page, true));
            setData(response.data);
        } catch (err) {
            setError("Failed to load organization members.");
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        if (canAccessPage) fetchMembers(currentPage);
    }, [canAccessPage, currentPage, fetchMembers]);

    if (!canAccessPage) return <div className="p-8 text-center text-destructive"><AlertTriangle className="mx-auto w-10 h-10 mb-2"/>Access Denied</div>;

    return (
        <div className="p-8 max-w-5xl mx-auto space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">Internal Directory</h1>
                    <p className="text-muted-foreground">Manage your workspace administrators and staff.</p>
                </div>
                {canInviteUsers && (
                    <Button onClick={() => setIsInviteOpen(true)}><Users className="w-4 h-4 mr-2" /> Invite Staff</Button>
                )}
            </div>

            <Card className="shadow-sm">
                <CardHeader>
                    <CardTitle>Team Members</CardTitle>
                    <CardDescription>All internal employees with access to the system.</CardDescription>
                </CardHeader>
                <CardContent>
                    {isLoading ? (
                        <div className="flex justify-center py-8"><Loader2 className="w-8 h-8 animate-spin text-muted-foreground" /></div>
                    ) : error ? (
                        <div className="text-center text-destructive py-8">{error}</div>
                    ) : (
                        <div className="rounded-md border">
                            <Table>
                                <TableHeader className="bg-muted/50">
                                    <TableRow>
                                        <TableHead>User</TableHead>
                                        <TableHead>Username</TableHead>
                                        <TableHead>Role</TableHead>
                                        {canInviteUsers && <TableHead className="text-right">Actions</TableHead>}
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {data?.items.map((member) => (
                                        <TableRow key={member.id}>
                                            <TableCell>
                                                <div className="font-medium">{member.firstName} {member.lastName}</div>
                                                <div className="text-sm text-muted-foreground">{member.email}</div>
                                            </TableCell>
                                            <TableCell className="text-muted-foreground">@{member.userName}</TableCell>
                                            <TableCell>
                                                <Badge variant={["Owner", "Manager"].includes(member.role) ? "default" : "secondary"}>
                                                    {["Owner", "Manager"].includes(member.role) ? <Shield className="w-3 h-3 mr-1" /> : <UserCog className="w-3 h-3 mr-1" />}
                                                    {member.role}
                                                </Badge>
                                            </TableCell>
                                            {canInviteUsers && (
                                                <TableCell className="text-right">
                                                    <MemberActionsMenu 
                                                        userId={member.id} currentRole={member.role} currentUserRole={user?.role || ""} onSuccess={() => fetchMembers(currentPage)}
                                                    />
                                                </TableCell>
                                            )}
                                        </TableRow>
                                    ))}
                                    {data?.items.length === 0 && (
                                        <TableRow><TableCell colSpan={4} className="h-24 text-center text-muted-foreground">No staff found.</TableCell></TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </div>
                    )}
                </CardContent>
                
                {/* PAGINATION CONTROLS */}
                {data && data.totalPages > 1 && (
                    <CardFooter className="flex items-center justify-between border-t p-4">
                        <p className="text-sm text-muted-foreground">
                            Showing page {data.pageNumber} of {data.totalPages} ({data.totalCount} total)
                        </p>
                        <div className="flex gap-2">
                            <Button variant="outline" size="sm" onClick={() => setCurrentPage(p => p - 1)} disabled={!data.hasPreviousPage}>
                                <ChevronLeft className="w-4 h-4 mr-1" /> Previous
                            </Button>
                            <Button variant="outline" size="sm" onClick={() => setCurrentPage(p => p + 1)} disabled={!data.hasNextPage}>
                                Next <ChevronRight className="w-4 h-4 ml-1" />
                            </Button>
                        </div>
                    </CardFooter>
                )}
            </Card>

            {canInviteUsers && <InviteMemberDialog isOpen={isInviteOpen} onOpenChange={setIsInviteOpen} onSuccess={() => fetchMembers(currentPage)} currentUserRole={user?.role || ""} />}
        </div>
    );
}