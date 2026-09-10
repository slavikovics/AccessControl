import { useCallback, useEffect, useState } from "react";
import {
  Alert,
  Box,
  Chip,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import SearchIcon from "@mui/icons-material/Search";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import Fab from "@mui/material/Fab";
import { recordsApi, type RecordKind } from "../api/records";
import type { RecordItem } from "../api/types";
import { errorMessage } from "../api/client";
import RecordDialog from "./RecordDialog";

interface Props {
  kind: RecordKind;
  label: string;
  description: string;
}

export default function RecordsPanel({ kind, label, description }: Props) {
  const api = recordsApi(kind);
  const [records, setRecords] = useState<RecordItem[]>([]);
  const [query, setQuery] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<RecordItem | null>(null);

  const load = useCallback(
    async (q: string) => {
      try {
        setRecords(await api.list(q));
        setError(null);
      } catch (err) {
        setError(errorMessage(err, `Could not load ${label.toLowerCase()} records.`));
      }
    },
    [kind],
  );

  useEffect(() => {
    load(query);
  }, [kind]);

  function openCreate() {
    setEditing(null);
    setDialogOpen(true);
  }

  function openEdit(record: RecordItem) {
    setEditing(record);
    setDialogOpen(true);
  }

  async function handleSave(title: string, content: string) {
    if (editing) {
      await api.update(editing.id, title, content);
    } else {
      await api.create(title, content);
    }
    setDialogOpen(false);
    await load(query);
  }

  async function handleDelete(id: number) {
    await api.remove(id);
    await load(query);
  }

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "flex-start", mb: 2 }}>
        <Box>
          <Typography variant="h6">{label}</Typography>
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        </Box>
        <Fab color="primary" size="medium" onClick={openCreate} aria-label={`add ${label.toLowerCase()}`}>
          <AddIcon />
        </Fab>
      </Stack>

      <TextField
        placeholder="Search..."
        size="small"
        value={query}
        onChange={(e) => {
          setQuery(e.target.value);
          load(e.target.value);
        }}
        sx={{ mb: 2, width: 320 }}
        slotProps={{
          input: {
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon fontSize="small" />
              </InputAdornment>
            ),
          },
        }}
      />

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Title</TableCell>
              <TableCell>Content</TableCell>
              <TableCell>Updated</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {records.map((r) => (
              <TableRow key={r.id} hover>
                <TableCell>{r.title}</TableCell>
                <TableCell sx={{ maxWidth: 320, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                  {r.content}
                </TableCell>
                <TableCell>{new Date(r.updatedAtUtc).toLocaleString()}</TableCell>
                <TableCell align="right">
                  <Tooltip title="Edit">
                    <IconButton size="small" onClick={() => openEdit(r)}>
                      <EditIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Delete">
                    <IconButton size="small" onClick={() => handleDelete(r.id)}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
            {records.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} align="center">
                  <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
                    No records{query ? " match your search" : ""}.
                  </Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
        <Chip
          size="small"
          label={kind.endsWith("confidential") ? "Encrypted (AES-256-GCM)" : "Stored as plain text"}
          color={kind.endsWith("confidential") ? "warning" : "default"}
          variant="outlined"
        />
        <Chip
          size="small"
          label={kind.startsWith("memory-") ? "RAM only — not persisted, lost on restart" : "Persisted in SQLite"}
          color={kind.startsWith("memory-") ? "error" : "default"}
          variant="outlined"
        />
      </Stack>

      <RecordDialog open={dialogOpen} record={editing} onClose={() => setDialogOpen(false)} onSave={handleSave} />
    </Box>
  );
}
