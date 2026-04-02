import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { LoginForm } from "../components/LoginForm";

export default function LoginPage() {
    return (
        // bg-background and text-foreground automatically adapt to the theme
        <div className="min-h-screen bg-background flex flex-col justify-center items-center p-4">

            {/* DeskForge Logo / Branding */}
            <div className="mb-8 flex items-center gap-2 font-bold text-2xl tracking-tight text-foreground">
                <div className="h-8 w-8 bg-primary rounded-md"></div>
                DeskForge
            </div>

            <Card className="w-full max-w-md shadow-sm">
                <CardHeader className="space-y-1 text-center">
                    <CardTitle className="text-2xl font-semibold tracking-tight">
                        Welcome back
                    </CardTitle>
                    <CardDescription>
                        Enter your credentials to access your workspace
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <LoginForm />
                </CardContent>
            </Card>

            <p className="mt-8 text-center text-sm text-muted-foreground">
                Don't have an organization yet?{" "}
                <a href="/register" className="font-semibold text-primary hover:underline">
                    Register here
                </a>
            </p>
        </div>
    );
}