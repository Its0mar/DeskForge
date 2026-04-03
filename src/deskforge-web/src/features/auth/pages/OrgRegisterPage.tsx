import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { OrgRegisterForm } from "../components/OrgRegisterForm";


export default function OrgRegisterPage() {
    return (
        <div className="min-h-screen bg-background flex flex-col justify-center items-center p-4">
        
            <div className="mb-8 flex items-center gap-2 font-bold text-2xl tracking-tight text-foreground">
                <div className="h-8 w-8 bg-primary rounded-md"></div>
                DeskForge
            </div>

            <Card className="w-full max-w-2xl shadow-sm">
                <CardHeader className="space-y-1 text-center">
                    <CardTitle className="text-2xl font-semibold tracking-tight">Organization Register</CardTitle>
                    <CardDescription>
                        Create a new workspace for your team to get started
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <OrgRegisterForm />
                </CardContent>
            </Card>

            <p className="mt-8 text-center text-sm text-muted-foreground">
                Already have an organization?{" "}
                <a href="/login" className="font-semibold text-primary hover:underline">
                    Login here
                </a>
            </p>

        </div>

    )
}