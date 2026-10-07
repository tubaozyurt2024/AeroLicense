import LogoutIcon from '@mui/icons-material/Logout';
import MenuIcon from '@mui/icons-material/Menu';
import {
  AppBar, Box, Divider, Drawer, IconButton, List, ListItemButton, ListItemIcon, ListItemText, Toolbar, Tooltip, Typography,
  useMediaQuery,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { Suspense, useState } from 'react';
import { NavLink, Outlet } from 'react-router';
import { LoadingState } from '@/components/LoadingState';
import { useCurrentUser, useAuth } from '@/features/auth/AuthContext';
import { t } from '@/i18n';
import { navigationFor } from './navigation';
import { ThemeToggle } from './ThemeToggle';

const DRAWER_WIDTH = 260;

/** Giriş yapılmış sayfaların iskeleti: üst çubuk + rol menüsü (masaüstünde sabit, mobilde açılır) + içerik. */
export function AppLayout() {
  const user = useCurrentUser();
  const { logout } = useAuth();
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'));
  const [mobileOpen, setMobileOpen] = useState(false);

  const menu = (
    <nav aria-label="Ana menü">
      <List>
        {navigationFor(user.role).map((item) => (
          <ListItemButton
            key={item.path}
            component={NavLink}
            to={item.path}
            end // "/applications" aktifken "/applications/new" de aktif görünmesin
            onClick={() => setMobileOpen(false)}
            sx={{ '&.active': { bgcolor: 'action.selected', fontWeight: 600, '& .MuiListItemIcon-root': { color: 'primary.main' } } }}
          >
            <ListItemIcon aria-hidden>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} />
          </ListItemButton>
        ))}
      </List>
    </nav>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <a href="#main" className="skip-link">{t.app.skipToContent}</a>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        className="no-print"
        sx={{ zIndex: (th) => th.zIndex.drawer + 1, borderBottom: 1, borderColor: 'divider' }}
      >
        <Toolbar sx={{ gap: 1 }}>
          {!isDesktop && (
            <IconButton edge="start" onClick={() => setMobileOpen(true)} aria-label={t.app.openMenu}>
              <MenuIcon />
            </IconButton>
          )}
          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography component="div" sx={{ fontWeight: 700, color: 'primary.main' }}>{t.app.name}</Typography>
            <Typography variant="caption" color="text.secondary" component="div" noWrap>{t.app.authority}</Typography>
          </Box>
          <ThemeToggle />
          <Box sx={{ textAlign: 'right', display: { xs: 'none', sm: 'block' }, mx: 1 }}>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>{user.fullName}</Typography>
            <Typography variant="caption" color="text.secondary">
              {t.roles[user.role]}
              {user.organizationName ? ` · ${user.organizationName}` : ''}
            </Typography>
          </Box>
          <Tooltip title={t.app.logout}>
            <IconButton onClick={() => void logout()} aria-label={t.app.logout}>
              <LogoutIcon />
            </IconButton>
          </Tooltip>
        </Toolbar>
      </AppBar>

      <Drawer
        className="no-print"
        variant={isDesktop ? 'permanent' : 'temporary'}
        open={isDesktop || mobileOpen}
        onClose={() => setMobileOpen(false)}
        sx={{ width: DRAWER_WIDTH, flexShrink: 0, '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' } }}
      >
        <Toolbar />
        <Divider />
        {menu}
      </Drawer>

      {/* tabIndex=-1: "içeriğe geç" bağlantısı odağı buraya taşıyabilsin. */}
      <Box component="main" id="main" tabIndex={-1} sx={{ flexGrow: 1, minWidth: 0, p: { xs: 2, md: 3 }, outline: 'none' }}>
        <Toolbar className="no-print" />
        <Suspense fallback={<LoadingState />}>
          <Outlet />
        </Suspense>
      </Box>
    </Box>
  );
}
