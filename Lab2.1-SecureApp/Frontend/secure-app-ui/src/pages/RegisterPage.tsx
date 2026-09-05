import { useState } from "react";
import type { FormEvent } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Alert, Box, Button, Link, Paper, Stack, TextField, Typography } from "@mui/material";
import PersonAddOutlinedIcon from "@mui/icons-material/PersonAddOutlined";
import { useAuth } from "../context/AuthContext";

export default function RegisterPage() {
  const { register, login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (password.length < 8) {
      setError("Password must be at least 8 characters.");
      return;
    }

    setSubmitting(true);
    try {
      await register(username, password);
      await login(username, password);
      navigate("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Registration failed.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Box
      sx={{
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        minHeight: "100vh",
        bgcolor: "background.default",
      }}
    >
      <Paper elevation={3} sx={{ p: 4, width: 360 }}>
        <Stack spacing={2} sx={{ alignItems: "center" }}>
          <PersonAddOutlinedIcon color="primary" fontSize="large" />
          <Typography variant="h5" component="h1">
            Create account
          </Typography>
          <Box component="form" onSubmit={handleSubmit} sx={{ width: "100%" }}>
            <Stack spacing={2}>
              {error && <Alert severity="error">{error}</Alert>}
              <TextField
                label="Username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                autoFocus
                required
                fullWidth
              />
              <TextField
                label="Password"
                type="password"
                helperText="At least 8 characters"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                fullWidth
              />
              <Button type="submit" variant="contained" size="large" disabled={submitting} fullWidth>
                Register
              </Button>
              <Typography variant="body2" align="center">
                Already have an account? <Link component={RouterLink} to="/login">Sign in</Link>
              </Typography>
            </Stack>
          </Box>
        </Stack>
      </Paper>
    </Box>
  );
}
