import { useState, useEffect, useCallback } from "react";
import { Users, Loader2, AlertTriangle, Plus, UserPlus } from "lucide-react";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";
import { useAuthStore } from "@/store/useAuthStore";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";

import * as CreateTeamDialog from "../components/CreateTeamDialog";
import { AddTeamMemberDialog } from "../components/AddTeamMemberDialog";

// Matches GetOrgTeamsResponse(Guid TeamId, string TeamName, int NumberOfMembers)
interface Team {
    teamId: string;
    teamName: string;
    numberOfMembers: number;
}

export default function TeamsPage() {
    const { user } = useAuthStore();
    
    const [teams, setTeams] = useState<Team[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    
    // Modal State
    const [isCreateOpen, setIsCreateOpen] = useState(false);
    const [addMemberData, setAddMemberData] = useState<{ isOpen: boolean, teamId: string | null, teamName: string }>({
        isOpen: false, teamId: null, teamName: ""
    });

    // 1. RBAC Check: Only Owners and Managers allowed!
    const isAuthorized = ["Owner", "Manager"].includes(user?.role || "");

    const fetchTeams = useCallback(async () => {
        setIsLoading(true);
        try {
            const response = await apiClient.get<Team[]>(API_ROUTES.TEAMS.BASE);
            setTeams(response.data);
        } catch (err) {
            setError("Failed to load teams.");
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        if (isAuthorized) fetchTeams();
    }, [isAuthorized, fetchTeams]);

    // Kick out unauthorized users
    if (!isAuthorized) {
        return (
            <div className="p-8 max-w-3xl mx-auto mt-12 text-center">
                <AlertTriangle className="w-12 h-12 text-destructive mx-auto mb-4" />
                <h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
                <p className="text-muted-foreground mt-2">Only Workspace Owners and Managers can manage Teams.</p>
            </div>
        );
    }

    return (
        <div className="p-8 max-w-5xl mx-auto space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">Teams Management</h1>
                    <p className="text-muted-foreground">Organize your staff into operational groups.</p>
                </div>
                <Button onClick={() => setIsCreateOpen(true)}>
                    <Plus className="w-4 h-4 mr-2" /> Create Team
                </Button>
            </div>

            <Card className="shadow-sm">
                <CardHeader>
                    <CardTitle>Organization Teams</CardTitle>
                    <CardDescription>Groups used for ticket routing and permissions.</CardDescription>
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
                                        <TableHead>Team Name</TableHead>
                                        <TableHead>Members</TableHead>
                                        <TableHead className="text-right">Actions</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {teams.map((team) => (
                                        <TableRow key={team.teamId}>
                                            <TableCell className="font-medium text-base">
                                                <div className="flex items-center gap-2">
                                                    <Users className="w-4 h-4 text-muted-foreground" />
                                                    {team.teamName}
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant="secondary">{team.numberOfMembers} users</Badge>
                                            </TableCell>
                                            <TableCell className="text-right">
                                                <Button 
                                                    variant="outline" 
                                                    size="sm"
                                                    onClick={() => setAddMemberData({ isOpen: true, teamId: team.teamId, teamName: team.teamName })}
                                                >
                                                    <UserPlus className="w-4 h-4 mr-2" /> Add Member
                                                </Button>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                    {teams.length === 0 && (
                                        <TableRow><TableCell colSpan={3} className="h-24 text-center text-muted-foreground">No teams found. Create one to get started!</TableCell></TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Hidden Modals */}
            <CreateTeamDialog.CreateTeamDialog isOpen={isCreateOpen} onOpenChange={setIsCreateOpen} onSuccess={fetchTeams} />
            <AddTeamMemberDialog 
                isOpen={addMemberData.isOpen} 
                teamId={addMemberData.teamId} 
                teamName={addMemberData.teamName}
                onOpenChange={(isOpen) => setAddMemberData(prev => ({ ...prev, isOpen }))} 
                onSuccess={fetchTeams} 
            />
        </div>
    );
}