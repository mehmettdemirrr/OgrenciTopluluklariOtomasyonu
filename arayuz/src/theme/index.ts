import { alpha, createTheme } from '@mui/material'
import { brand } from './tokens'

declare module '@mui/material/styles' {
  interface Palette {
    accent: Palette['primary']
  }
  interface PaletteOptions {
    accent?: PaletteOptions['primary']
  }
}

export const theme = createTheme({
  palette: {
    primary: {
      main: brand.turquoise,
      dark: brand.turquoiseDark,
      contrastText: '#ffffff',
    },
    secondary: {
      main: brand.navy,
      contrastText: '#ffffff',
    },
    warning: {
      main: brand.orange,
    },
    accent: {
      main: brand.gold,
      contrastText: '#ffffff',
    },
    grey: {
      400: brand.grey,
    },
    divider: alpha(brand.grey, 0.4),
    background: {
      default: '#F7F8FA',
    },
  },
  shape: {
    borderRadius: 10,
  },
  typography: {
    fontFamily: '"InterVariable", "Inter", "Roboto", "Helvetica", "Arial", sans-serif',
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          borderRadius: 8,
          fontWeight: 600,
        },
      },
      variants: [
        {
          props: { variant: 'contained', color: 'primary' },
          style: {
            backgroundColor: brand.turquoiseDark,
            '&:hover': {
              backgroundColor: brand.navy,
            },
          },
        },
      ],
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: '0 1px 3px rgba(38, 47, 89, 0.08), 0 1px 2px rgba(38, 47, 89, 0.06)',
        },
      },
      defaultProps: {
        elevation: 0,
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: '0 1px 3px rgba(38, 47, 89, 0.08), 0 1px 2px rgba(38, 47, 89, 0.06)',
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          fontWeight: 600,
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          boxShadow: 'none',
          borderBottom: `1px solid ${alpha(brand.grey, 0.25)}`,
        },
      },
      defaultProps: {
        color: 'transparent',
      },
    },
    MuiTextField: {
      defaultProps: {
        size: 'small',
      },
    },
  },
})
