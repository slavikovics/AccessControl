import { useEffect, useState } from "react";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from "@mui/material";
import type { RecordItem } from "../api/types";

interface Props {
  open: boolean;
  record: RecordItem | null;
  onClose: () => void;
  onSave: (title: string, content: string) => Promise<void>;
}

export default function RecordDialog({ open, record, onClose, onSave }: Props) {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      setTitle(record?.title ?? "");
      setContent(record?.content ?? "");
    }
  }, [open, record]);

  async function handleSave() {
    setSaving(true);
    try {
      await onSave(title, content);
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{record ? "Edit record" : "New record"}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField label="Title" value={title} onChange={(e) => setTitle(e.target.value)} autoFocus fullWidth />
          <TextField
            label="Content"
            value={content}
            onChange={(e) => setContent(e.target.value)}
            multiline
            minRows={4}
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained" disabled={saving || !title.trim()}>
          Save
        </Button>
      </DialogActions>
    </Dialog>
  );
}
