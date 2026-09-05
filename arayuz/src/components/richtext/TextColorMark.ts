import { Mark } from '@tiptap/core'
import type { RichTextColorToken } from '../../theme/tokens'

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    textColor: {
      setTextColor: (token: RichTextColorToken) => ReturnType
      unsetTextColor: () => ReturnType
    }
  }
}

/**
 * docs/MIMARI.md · A-71/A-72: belge HTML'e hiç çevrilmediği için parseHTML/renderHTML yok —
 * yalnızca JSON düğüm ağacında {"type":"textColor","attrs":{"token":"..."}} olarak durur.
 * `token`, sunucudaki RichTextSchema.AllowedColorTokens ile aynı beş isimden biri olmalıdır.
 */
export const TextColorMark = Mark.create({
  name: 'textColor',

  addAttributes() {
    return {
      token: {
        default: null,
        parseHTML: () => null,
        renderHTML: () => ({}),
      },
    }
  },

  addCommands() {
    return {
      setTextColor:
        (token: RichTextColorToken) =>
        ({ commands }) =>
          commands.setMark(this.name, { token }),
      unsetTextColor:
        () =>
        ({ commands }) =>
          commands.unsetMark(this.name),
    }
  },
})
