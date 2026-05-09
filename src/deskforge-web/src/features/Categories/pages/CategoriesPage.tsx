import { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { FolderTree, Loader2, Plus, Users, AlertTriangle, ChevronLeft, ChevronRight, Table, Badge } from "lucide-react";
import { apiClient } from "@/lib/apiClient";
import { API_ROUTES } from "@/lib/apiRoutes";
import { useAuthStore } from "@/store/useAuthStore";
import type { PaginatedResult } from "@/types/pagination"; // Reusing the type we built earlier
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";


// Matches GetCategoryResponse(Guid Id, string Name, string? Description, string TargetTeamName, Guid TargetTeamId)
interface Category {
    id: string;
    name: string;
    description: string | null;
    targetTeamName: string;
    targetTeamId: string;
}

export default function CategoriesPage() {
    const navigate = useNavigate();
    const { user } = useAuthStore();
    
    // Pagination State
    const [currentPage, setCurrentPage] = useState(1);
    const [data, setData] = useState<PaginatedResult<Category> | null>(null);
    
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Enforcement: Only Owners and Managers per your C# Policy
    const isAuthorized = ["Owner", "Manager"].includes(user?.role || "");

    const fetchCategories = useCallback(async (page: number) => {
        setIsLoading(true);
        try {
            const response = await apiClient.get<PaginatedResult<Category>>(API_ROUTES.CATEGORIES.BASE(page));
            setData(response.data);
        } catch (err) {
            setError("Failed to load categories.");
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        if (isAuthorized) {
            fetchCategories(currentPage);
        }
    }, [isAuthorized, currentPage, fetchCategories]);


    // --- ACCESS DENIED UI ---
    if (!isAuthorized) {
        return (
            <div className="p-8 max-w-3xl mx-auto mt-12 text-center">
                <AlertTriangle className="w-12 h-12 text-destructive mx-auto mb-4" />
                <h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
                <p className="text-muted-foreground mt-2">Only Workspace Owners and Managers can manage request categories.</p>
            </div>
        );
    }

    // --- MAIN UI RENDER ---
    return (
        <div className="p-8 max-w-5xl mx-auto space-y-6">
            {/* Header Section */}
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">Request Categories</h1>
                    <p className="text-muted-foreground">Manage how incoming tickets are classified and routed.</p>
                </div>
                <Button onClick={() => navigate("/categories/new")}>
                    <Plus className="w-4 h-4 mr-2" /> Create Category
                </Button>
            </div>

            <Card className="shadow-sm">
                <CardHeader>
                    <div className="flex items-center gap-2">
                        <FolderTree className="w-5 h-5 text-primary" />
                        <CardTitle>Routing Directory</CardTitle>
                    </div>
                    <CardDescription>
                        Categories determine which operational team automatically receives incoming requests.
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {isLoading ? (
                        <div className="flex justify-center py-12">
                            <Loader2 className="w-8 h-8 animate-spin text-muted-foreground" />
                        </div>
                    ) : error ? (
                        <div className="text-center text-destructive py-8">{error}</div>
                    ) : (
                        <div className="rounded-md border">
                            <Table>
                                <TableHeader className="bg-muted/50">
                                    <TableRow>
                                        <TableHead>Category Name</TableHead>
                                        <TableHead>Description</TableHead>
                                        <TableHead>Routing Target</TableHead>
                                        <TableHead className="text-right">Actions</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {data?.items.map((category) => (
                                        <TableRow key={category.id}>
                                            <TableCell className="font-medium whitespace-nowrap">
                                                {category.name}
                                            </TableCell>
                                            <TableCell className="text-muted-foreground max-w-md truncate">
                                                {category.description || <span className="italic opacity-50">No description</span>}
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant="secondary" className="flex w-max items-center gap-1 bg-blue-50 text-blue-700 hover:bg-blue-50 border-blue-200">
                                                    <Users className="w-3 h-3" />
                                                    {category.targetTeamName}
                                                </Badge>
                                            </TableCell>
                                            <TableCell className="text-right">
                                                <Button variant="ghost" size="sm" className="text-muted-foreground">
                                                    Edit
                                                </Button>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                    {data?.items.length === 0 && (
                                        <TableRow>
                                            <TableCell colSpan={4} className="h-32 text-center text-muted-foreground">
                                                No categories found. Click "Create Category" to set up your routing.
                                            </TableCell>
                                        </TableRow>
                                    )}
                                </TableBody>
                            </Table>
                        </div>
                    )}
                </CardContent>

                {/* PAGINATION CONTROLS */}
                {data && data.totalPages > 1 && (
                    <div className="flex items-center justify-between border-t p-4 bg-muted/20">
                        <p className="text-sm text-muted-foreground">
                            Showing page {data.pageNumber} of {data.totalPages} ({data.totalCount} total)
                        </p>
                        <div className="flex gap-2">
                            <Button 
                                variant="outline" 
                                size="sm" 
                                onClick={() => setCurrentPage(p => p - 1)} 
                                disabled={!data.hasPreviousPage}
                            >
                                <ChevronLeft className="w-4 h-4 mr-1" /> Previous
                            </Button>
                            <Button 
                                variant="outline" 
                                size="sm" 
                                onClick={() => setCurrentPage(p => p + 1)} 
                                disabled={!data.hasNextPage}
                            >
                                Next <ChevronRight className="w-4 h-4 ml-1" />
                            </Button>
                        </div>
                    </div>
                )}
            </Card>
        </div>
    );
}