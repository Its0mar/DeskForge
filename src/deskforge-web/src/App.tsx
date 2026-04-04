import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./features/auth/pages/LoginPage";
import OrgRegisterPage from "./features/auth/pages/OrgRegisterPage";
import { PublicRoute } from "./components/PublicRoute";
import { ProtectedRoute } from "./components/ProtectedRoute";
import RequesterRegisterPage from "./features/auth/pages/RequesterRegisterPage";
import AcceptInvitePage from "./features/auth/pages/AcceptInvitePage";

function App() {
  return (
    <BrowserRouter>
      <Routes>

        <Route element={<PublicRoute />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<OrgRegisterPage />} />
          <Route path="/:orgSlug/register" element={<RequesterRegisterPage />} />
          <Route path="/accept-invite" element={<AcceptInvitePage />} />
        </Route>

        <Route element={<ProtectedRoute />}>
          <Route path="/dashboard" element={<div>Dashboard (coming soon)</div>} />
        </Route>

        <Route path="*" element={<Navigate to="/login" replace />} />

      </Routes>
    </BrowserRouter>
  );
}

export default App;
