import { Route, Routes } from "react-router-dom";
import Layout from "./Layout";
import Dashboard from "./pages/Dashboard";
import Docs from "./pages/Docs";
import McpTools from "./pages/McpTools";
import Arquitetura from "./pages/Arquitetura";
import Backlog from "./pages/Backlog";

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="docs/*" element={<Docs />} />
        <Route path="mcp-tools" element={<McpTools />} />
        <Route path="arquitetura" element={<Arquitetura />} />
        <Route path="backlog" element={<Backlog />} />
      </Route>
    </Routes>
  );
}
