import { useState } from "react";
import type { SyntheticEvent } from "react";
import { AppBar, Box, Container, Tab, Tabs, Toolbar, Typography, IconButton, Tooltip } from "@mui/material";
import LogoutIcon from "@mui/icons-material/Logout";
import ShieldOutlinedIcon from "@mui/icons-material/ShieldOutlined";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import RecordsPanel from "../components/RecordsPanel";

export default function DashboardPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [tab, setTab] = useState(0);

  function handleChange(_e: SyntheticEvent, value: number) {
    setTab(value);
  }

  async function handleLogout() {
    await logout();
    navigate("/login");
  }

  return (
    <Box>
      <AppBar position="static" color="primary" enableColorOnDark>
        <Toolbar>
          <ShieldOutlinedIcon sx={{ mr: 1 }} />
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            SecureApp
          </Typography>
          <Typography variant="body2" sx={{ mr: 2 }}>
            {user?.username}
          </Typography>
          <Tooltip title="Log out">
            <IconButton color="inherit" onClick={handleLogout}>
              <LogoutIcon />
            </IconButton>
          </Tooltip>
        </Toolbar>
      </AppBar>

      <Container maxWidth="md" sx={{ py: 4 }}>
        <Tabs value={tab} onChange={handleChange} sx={{ mb: 3 }} variant="scrollable" scrollButtons="auto">
          <Tab label="Confidential data" />
          <Tab label="Public data" />
          <Tab label="In-memory confidential" />
          <Tab label="In-memory public" />
        </Tabs>

        {tab === 0 && (
          <RecordsPanel
            kind="confidential"
            label="Confidential data"
            description="Visible only to you; encrypted at rest and never shown to other users."
          />
        )}
        {tab === 1 && (
          <RecordsPanel
            kind="public"
            label="Public data"
            description="Non-confidential notes, still scoped to your account."
          />
        )}
        {tab === 2 && (
          <RecordsPanel
            kind="memory-confidential"
            label="In-memory confidential data"
            description="Lab 3.1: held only in the server process's RAM (never written to disk), encrypted immediately on write. Lost on restart."
          />
        )}
        {tab === 3 && (
          <RecordsPanel
            kind="memory-public"
            label="In-memory public data"
            description="Lab 3.1: held only in the server process's RAM, in plain text. Lost on restart."
          />
        )}
      </Container>
    </Box>
  );
}
