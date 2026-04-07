import { useState, useEffect, useCallback } from "react";
import { Loader2, AlertTriangle, User, ChevronLeft, ChevronRight } from "lucide-react";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";
import { useAuthStore } from "@/store/useAuthStore";
import type { PaginatedResult } from "@/types/pagination";

import { Card, CardContent, CardHeader, CardTitle, CardFooter } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

interface Member {
    id: string; userName: string; email: string; firstName: string; lastName: string; role: string;
}

export default function RequestersPage() {
    const { user } = useAuthStore();
    const [currentPage, setCurrentPage] = useState(1);
    const [data, setData] = useState<PaginatedResult<Member> | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    const canAccessPage = user?.role !== "Requester";

    const fetchRequesters = useCallback(async (page: number) => {
        setIsLoading(true);
        try {
            const response = await apiClient.get<PaginatedResult<Member>>(API_ROUTES.ORGANIZATIONS.MEMBERS(page, false));
            setData(response.data);
        } catch (err) {
            console.error(err);
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        if (canAccessPage) fetchRequesters(currentPage);
    }, [canAccessPage, currentPage, fetchRequesters]);

    if (!canAccessPage) return <div className="p-8 text-center text-destructive"><AlertTriangle className="mx-auto w-10 h-10 mb-2"/>Access Denied</div>;

    return (
        <div className="p-8 max-w-5xl mx-auto space-y-6">
            <div>
                <h1 className="text-3xl font-bold tracking-tight">External Requesters</h1>
                <p className="text-muted-foreground">View all clients and customers associated with your workspace.</p>
            </div>

            <Card className="shadow-sm">
                <CardHeader>
                    <CardTitle>Client Directory</CardTitle>
                </CardHeader>
                <CardContent>
                    {isLoading ? (
                        <div className="flex justify-center py-8"><Loader2 className="w-8 h-8 animate-spin text-muted-foreground" /></div>
                    ) : (
                        <div className="rounded-md border">
                            <Table>
                                <TableHeader className="bg-muted/50">
                                    <TableRow>
                                        <TableHead>User</TableHead>
                                        <TableHead>Username</TableHead>
                                        <TableHead>Role</TableHead>
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
                                                <Badge variant="default" className="bg-slate-50">
                                                    <User className="w-3 h-3 mr-1 text-slate-500" />
                                                    {member.role}
                                                </Badge>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                    {data?.items.length === 0 && (
                                        <TableRow><TableCell colSpan={3} className="h-24 text-center text-muted-foreground">No requesters found.</TableCell></TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </div>
                    )}
                </CardContent>
                
                {data && data.totalPages > 1 && (
                    <CardFooter className="flex items-center justify-between border-t p-4">
                        <p className="text-sm text-muted-foreground">Showing page {data.pageNumber} of {data.totalPages}</p>
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
        </div>
    );
}