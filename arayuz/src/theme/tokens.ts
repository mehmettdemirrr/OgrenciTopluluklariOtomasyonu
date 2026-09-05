// docs/PLAN-V2.md §0 · A-37 · Y-56 — marka renkleri yalnızca burada tanımlanır, bileşen kodunda hex yazılmaz.
export const brand = {
  turquoise: '#12A7CD',
  turquoiseDark: '#0B6E87', // beyaz metinle 5.9:1 (AA) — contained buton zemini; turkuaz düz haliyle 2.8:1, AA'yı geçmez
  navy: '#262F59',
  orange: '#EF7F1A',
  gold: '#B99C71',
  grey: '#A0A0A0', // kaynak belgedeki RGB 160·160·160; basılı hex #727271 ile tutarsız — RGB kanonik kabul edildi
  greyDark: '#727271', // PLAN-V2: text.secondary
} as const

/** Y-56: yüzey ve metin renkleri de yalnızca burada. */
export const surfaces = {
  light: {
    default: '#F4F6FA',
    paper: '#ffffff',
    text: brand.navy,
    textMuted: brand.greyDark,
  },
  dark: {
    default: '#12162A',
    paper: '#1B2140',
    text: '#E8EAF4',
    textMuted: '#A8ADC2',
  },
} as const

/**
 * docs/MIMARI.md · A-72: zengin metin içeriğindeki renk anlamsal token'dır, hex değildir.
 * Yazar bu beş isimden seçer; hangi hex'e karşılık geldiğine mod (açık/koyu) karar verir.
 * Sunucudaki RichTextSchema.AllowedColorTokens ile AYNI beş isim olmalıdır — biri değişirse
 * kullanıcının seçtiği renk kaydedilemez (Architecture.Tests bu eşleşmeyi kilitler).
 */
export const richTextColors = {
  light: {
    accent: brand.turquoiseDark,
    success: '#2E7D32',
    warning: '#B45309',
    danger: '#C62828',
    muted: brand.greyDark,
  },
  dark: {
    accent: brand.turquoise,
    success: '#7BC67E',
    warning: '#E5A13A',
    danger: '#F28B82',
    muted: surfaces.dark.textMuted,
  },
} as const

export type RichTextColorToken = keyof typeof richTextColors.light
