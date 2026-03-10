import { createTheme } from '@mui/material/styles';

export const darkTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#22d3ee' },
    secondary: { main: '#ef4444' },
    background: { default: '#080e1a', paper: '#0f172a' },
    text: { primary: '#f1f5f9', secondary: '#94a3b8' },
    divider: '#1e293b',
  },
  shape: { borderRadius: 8 },
  typography: { 
    // Cambiamos a Inter
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif' 
  },
});