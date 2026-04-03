import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./features/auth/pages/LoginPage";
import OrgRegisterPage from "./features/auth/pages/OrgRegisterPage";



function App() {


  return (
    
      <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<OrgRegisterPage />} /> {/* Add this */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>

  )
}

export default App
