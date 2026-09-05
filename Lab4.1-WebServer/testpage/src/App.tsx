import {
  Box,
  Chip,
  Container,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import DnsOutlinedIcon from "@mui/icons-material/DnsOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import ArticleOutlinedIcon from "@mui/icons-material/ArticleOutlined";

const BUILD_DATE = __BUILD_DATE__;

export default function App() {
  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        justifyContent: "center",
        alignItems: "flex-start",
        bgcolor: "background.default",
        py: { xs: 4, sm: 8 },
        px: 2,
      }}
    >
      <Container maxWidth="sm">
        <Paper elevation={3} sx={{ p: { xs: 3, sm: 5 } }}>
          <Stack spacing={1} sx={{ alignItems: "center", mb: 3 }}>
            <DnsOutlinedIcon color="primary" fontSize="large" />
            <Typography variant="h5" component="h1" align="center">
              Тестовая страница — Лабораторная работа №4.1
            </Typography>
            <Chip size="small" label="Caddy + статический React-билд" variant="outlined" />
          </Stack>

          <Typography variant="body1" sx={{ mb: 2 }}>
            Эта страница развёрнута на удалённом сервере как тестовая
            страница для лабораторной работы «Безопасность работы
            удалённых серверов в сети Internet. Часть 1. Анализ запросов
            из сети Internet» по дисциплине «Проектирование защищенных
            интеллектуальных информационных систем».
          </Typography>

          <Typography variant="body1" sx={{ mb: 2 }}>
            Сервер преднамеренно доступен из сети Internet по SSH и
            HTTP(S), чтобы за несколько дней в логах SSH-демона и
            веб-сервера накопился реальный фоновый трафик (автоматические
            сканеры, попытки подбора пароля и т.&nbsp;д.) для последующего
            анализа в отчёте.
          </Typography>

          <Divider sx={{ my: 3 }} />

          <Stack spacing={2}>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: "flex-start" }}>
              <SecurityOutlinedIcon color="action" sx={{ mt: 0.3 }} />
              <Box>
                <Typography variant="subtitle2">Веб-сервер</Typography>
                <Typography variant="body2" color="text.secondary">
                  Caddy — раздаёт этот статический билд, автоматически
                  получает и продлевает TLS-сертификат (Let&apos;s
                  Encrypt) и пишет структурированные JSON-логи доступа.
                </Typography>
              </Box>
            </Stack>

            <Stack direction="row" spacing={1.5} sx={{ alignItems: "flex-start" }}>
              <ArticleOutlinedIcon color="action" sx={{ mt: 0.3 }} />
              <Box>
                <Typography variant="subtitle2">Логи</Typography>
                <Typography variant="body2" color="text.secondary">
                  Анализируются access-логи Caddy и логи аутентификации
                  SSH (<code>/var/log/auth.log</code> или{" "}
                  <code>/var/log/secure</code>, в зависимости от
                  дистрибутива).
                </Typography>
              </Box>
            </Stack>
          </Stack>

          <Divider sx={{ my: 3 }} />

          <Typography variant="caption" color="text.secondary" align="center" sx={{ display: "block" }}>
            Сборка: {BUILD_DATE}
          </Typography>
        </Paper>
      </Container>
    </Box>
  );
}
