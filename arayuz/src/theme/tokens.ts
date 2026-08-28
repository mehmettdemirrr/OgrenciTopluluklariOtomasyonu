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
