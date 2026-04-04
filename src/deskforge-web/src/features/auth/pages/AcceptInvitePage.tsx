import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { AcceptInviteForm } from "../components/AcceptInviteForm";

export default function AcceptInvitePage() {
    return (
        <div className="min-h-screen bg-background flex flex-col justify-center items-center p-4">
            <div className="mb-8 flex items-center gap-2 font-bold text-2xl tracking-tight text-foreground">
                <div className="h-8 w-8 bg-primary rounded-md"></div>
                DeskForge
            </div>

            <Card className="w-full max-w-2xl shadow-sm">
                <CardHeader className="space-y-1 text-center">
                    <CardTitle className="text-2xl font-semibold tracking-tight">Accept Invitation</CardTitle>
                    <CardDescription>
                        Set up your profile and password to join your team's workspace
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <AcceptInviteForm />
                </CardContent>
            </Card>
        </div>
    );
}