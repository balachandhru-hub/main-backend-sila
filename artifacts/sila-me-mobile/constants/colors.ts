import { silaTokens } from '@workspace/design-tokens';

const colors = {
  light: {
    text: silaTokens.textPrimary,
    tint: silaTokens.brandPrimary,
    background: silaTokens.background,
    foreground: silaTokens.textPrimary,
    card: silaTokens.surface,
    cardForeground: silaTokens.textPrimary,
    primary: silaTokens.brandPrimaryDark,
    primaryForeground: '#FFFFFF',
    primaryLight: silaTokens.brandPrimaryLight,
    secondary: silaTokens.surfaceSubtle,
    secondaryForeground: silaTokens.textPrimary,
    muted: silaTokens.surfaceSubtle,
    mutedForeground: silaTokens.textSecondary,
    accent: silaTokens.brandPrimary,
    accentForeground: '#FFFFFF',
    destructive: silaTokens.error,
    destructiveForeground: '#FFFFFF',
    error: silaTokens.error,
    success: silaTokens.success,
    warning: silaTokens.warning,
    warningSurface: silaTokens.warningSurface,
    border: silaTokens.borderColor,
    input: silaTokens.borderColor,
  },
  radius: silaTokens.radiusMd,
};

export default colors;