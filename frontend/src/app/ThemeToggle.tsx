import BrightnessAutoIcon from '@mui/icons-material/BrightnessAuto';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import LightModeIcon from '@mui/icons-material/LightMode';
import { IconButton, Tooltip } from '@mui/material';
import { useColorScheme } from '@mui/material/styles';
import { t } from '@/i18n';

const order = ['system', 'light', 'dark'] as const;

/** Sistem → açık → koyu. Seçim MUI tarafından localStorage'a yazılır (hassas veri değil, sadece tercih). */
export function ThemeToggle() {
  const { mode, setMode } = useColorScheme();
  const current = mode ?? 'system';
  const next = order[(order.indexOf(current) + 1) % order.length]!;
  const icon = current === 'light' ? <LightModeIcon /> : current === 'dark' ? <DarkModeIcon /> : <BrightnessAutoIcon />;

  return (
    <Tooltip title={`${t.app.theme[current]} · ${t.app.theme.toggle}`}>
      <IconButton color="inherit" onClick={() => setMode(next)} aria-label={`${t.app.theme.toggle} (${t.app.theme[current]})`}>
        {icon}
      </IconButton>
    </Tooltip>
  );
}
