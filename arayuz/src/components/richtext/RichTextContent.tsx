import { Fragment, useMemo, type ReactNode } from 'react'
import { Box, Link as MuiLink, Typography, useTheme } from '@mui/material'
import { richTextColors, type RichTextColorToken } from '../../theme/tokens'

interface RichTextNode {
  type?: string
  attrs?: { textAlign?: string; level?: number }
  marks?: { type?: string; attrs?: Record<string, unknown> }[]
  text?: string
  content?: RichTextNode[]
}

interface RichTextDoc {
  type: 'doc'
  content?: RichTextNode[]
}

function safeParse(json: string | null): RichTextDoc | null {
  if (!json) {
    return null
  }
  try {
    const parsed = JSON.parse(json) as RichTextDoc
    return parsed && parsed.type === 'doc' ? parsed : null
  } catch {
    return null
  }
}

const isColorToken = (value: unknown, table: Record<string, string>): value is RichTextColorToken =>
  typeof value === 'string' && value in table

/**
 * docs/MIMARI.md · A-71/Y-78: HTML dizesi hiç ayrıştırılmaz, ham HTML enjekte eden React API'si
 * kullanılmaz. Ağaç gezilir, izin listesindeki düğümler React öğesine çevrilir; tanınmayan düğüm
 * ATLANIR (sunucu zaten reddetmiş olsa da, eski/bozuk bir kayıt ekranı çökertmesin diye).
 */
export function RichTextContent({ json, fallbackText }: { json: string | null; fallbackText: string }) {
  const theme = useTheme()
  const colors = richTextColors[theme.palette.mode]
  const doc = useMemo(() => safeParse(json), [json])

  if (!doc) {
    return (
      <Typography variant="body2" sx={{ whiteSpace: 'pre-line', lineHeight: 1.7 }}>
        {fallbackText}
      </Typography>
    )
  }

  return (
    <Box sx={{ '& > :first-of-type': { mt: 0 }, '& > :last-child': { mb: 0 } }}>
      {renderNodes(doc.content, colors)}
    </Box>
  )
}

function renderNodes(nodes: RichTextNode[] | undefined, colors: Record<string, string>): ReactNode {
  if (!nodes) {
    return null
  }
  return nodes.map((node, index) => <Fragment key={index}>{renderNode(node, colors)}</Fragment>)
}

function renderNode(node: RichTextNode, colors: Record<string, string>): ReactNode {
  switch (node.type) {
    case 'paragraph':
      return (
        <Typography variant="body2" sx={{ lineHeight: 1.7, mb: 1.5, textAlign: node.attrs?.textAlign as 'left' | 'center' | 'right' | undefined }}>
          {renderNodes(node.content, colors)}
        </Typography>
      )
    case 'heading': {
      const variant = node.attrs?.level === 4 ? 'subtitle1' : 'h6'
      return (
        <Typography variant={variant} sx={{ fontWeight: 800, mt: 2, mb: 1, textAlign: node.attrs?.textAlign as 'left' | 'center' | 'right' | undefined }}>
          {renderNodes(node.content, colors)}
        </Typography>
      )
    }
    case 'bulletList':
      return <Box component="ul" sx={{ pl: 3, mb: 1.5 }}>{renderNodes(node.content, colors)}</Box>
    case 'orderedList':
      return <Box component="ol" sx={{ pl: 3, mb: 1.5 }}>{renderNodes(node.content, colors)}</Box>
    case 'listItem':
      return <Box component="li" sx={{ mb: 0.5 }}>{renderNodes(node.content, colors)}</Box>
    case 'blockquote':
      return (
        <Box
          sx={{ borderLeft: '3px solid', borderColor: 'divider', pl: 2, py: 0.5, mb: 1.5, color: 'text.secondary' }}
        >
          {renderNodes(node.content, colors)}
        </Box>
      )
    case 'hardBreak':
      return <br />
    case 'text':
      return renderText(node)
    default:
      // Tanınmayan düğüm sessizce atlanır — sunucu zaten reddetmiş olurdu, bu yalnızca ikinci savunma.
      return null
  }

  function renderText(textNode: RichTextNode): ReactNode {
    let element: ReactNode = textNode.text ?? ''

    for (const mark of textNode.marks ?? []) {
      switch (mark.type) {
        case 'bold':
          element = <strong>{element}</strong>
          break
        case 'italic':
          element = <em>{element}</em>
          break
        case 'underline':
          element = <Box component="span" sx={{ textDecoration: 'underline' }}>{element}</Box>
          break
        case 'strike':
          element = <Box component="span" sx={{ textDecoration: 'line-through' }}>{element}</Box>
          break
        case 'link': {
          const href = mark.attrs?.href
          if (typeof href === 'string' && href.startsWith('https://')) {
            element = (
              <MuiLink href={href} target="_blank" rel="noopener noreferrer">
                {element}
              </MuiLink>
            )
          }
          break
        }
        case 'textColor': {
          const token = mark.attrs?.token
          if (isColorToken(token, colors)) {
            element = <Box component="span" sx={{ color: colors[token] }}>{element}</Box>
          }
          break
        }
        default:
          break
      }
    }

    return element
  }
}
