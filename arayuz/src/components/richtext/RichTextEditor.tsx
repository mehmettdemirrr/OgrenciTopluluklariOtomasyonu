import { useEffect, useState } from 'react'
import { EditorContent, useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import TextAlign from '@tiptap/extension-text-align'
import { TextStyle } from '@tiptap/extension-text-style'
import {
  Box,
  Divider,
  IconButton,
  Menu,
  MenuItem,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  useTheme,
} from '@mui/material'
import FormatBoldIcon from '@mui/icons-material/FormatBoldRounded'
import FormatItalicIcon from '@mui/icons-material/FormatItalicRounded'
import FormatUnderlinedIcon from '@mui/icons-material/FormatUnderlinedRounded'
import FormatStrikethroughIcon from '@mui/icons-material/FormatStrikethroughRounded'
import FormatListBulletedIcon from '@mui/icons-material/FormatListBulletedRounded'
import FormatListNumberedIcon from '@mui/icons-material/FormatListNumberedRounded'
import FormatAlignLeftIcon from '@mui/icons-material/FormatAlignLeftRounded'
import FormatAlignCenterIcon from '@mui/icons-material/FormatAlignCenterRounded'
import FormatAlignRightIcon from '@mui/icons-material/FormatAlignRightRounded'
import LinkIcon from '@mui/icons-material/LinkRounded'
import LinkOffIcon from '@mui/icons-material/LinkOffRounded'
import FormatColorTextIcon from '@mui/icons-material/FormatColorTextRounded'
import FormatClearIcon from '@mui/icons-material/FormatClearRounded'
import { richTextColors, type RichTextColorToken } from '../../theme/tokens'
import { TextColorMark } from './TextColorMark'

const COLOR_LABELS: Record<RichTextColorToken, string> = {
  accent: 'Vurgu',
  success: 'Başarı',
  warning: 'Uyarı',
  danger: 'Tehlike',
  muted: 'Soluk',
}

/**
 * docs/MIMARI.md · K-42/A-71/A-72: kalın, italik, altı çizili, üstü çizili, başlık, liste,
 * hizalama, bağlantı ve renk. `onChange` her değişiklikte `editor.getJSON()` çıktısını
 * JSON.stringify ile verir — sunucudaki RichTextDocumentValidator aynı ağacı doğrular.
 */
export function RichTextEditor({ value, onChange }: { value: string | null; onChange: (json: string) => void }) {
  const theme = useTheme()
  const colors = richTextColors[theme.palette.mode]
  const [colorMenuAnchor, setColorMenuAnchor] = useState<HTMLElement | null>(null)

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        // Sunucudaki izin listesinde yoklar — açık kalırlarsa kullanıcı yazar, kaydet hata verir.
        codeBlock: false,
        code: false,
        horizontalRule: false,
        heading: { levels: [3, 4] },
        link: { protocols: ['https'], autolink: false, openOnClick: false },
      }),
      TextAlign.configure({ types: ['paragraph', 'heading'] }),
      TextStyle,
      TextColorMark,
    ],
    content: value ? (JSON.parse(value) as object) : '',
    onUpdate: ({ editor: current }) => {
      onChange(JSON.stringify(current.getJSON()))
    },
  })

  useEffect(
    () => () => {
      editor?.destroy()
    },
    [editor],
  )

  if (!editor) {
    return null
  }

  const setLink = () => {
    const previousUrl = editor.getAttributes('link').href as string | undefined
    // eslint-disable-next-line no-alert
    const url = window.prompt('Bağlantı adresi (https://...)', previousUrl ?? 'https://')
    if (url === null) {
      return
    }
    if (url.trim().length === 0 || !url.startsWith('https://')) {
      editor.chain().focus().extendMarkRange('link').unsetLink().run()
      return
    }
    editor.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
  }

  return (
    <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, overflow: 'hidden' }}>
      <Stack
        direction="row"
        spacing={0.5}
        sx={{ flexWrap: 'wrap', alignItems: 'center', p: 0.75, bgcolor: 'action.hover', borderBottom: '1px solid', borderColor: 'divider' }}
      >
        <ToggleButtonGroup size="small">
          <ToggleButton value="bold" selected={editor.isActive('bold')} onClick={() => editor.chain().focus().toggleBold().run()}>
            <Tooltip title="Kalın"><FormatBoldIcon fontSize="small" /></Tooltip>
          </ToggleButton>
          <ToggleButton value="italic" selected={editor.isActive('italic')} onClick={() => editor.chain().focus().toggleItalic().run()}>
            <Tooltip title="İtalik"><FormatItalicIcon fontSize="small" /></Tooltip>
          </ToggleButton>
          <ToggleButton value="underline" selected={editor.isActive('underline')} onClick={() => editor.chain().focus().toggleUnderline().run()}>
            <Tooltip title="Altı çizili"><FormatUnderlinedIcon fontSize="small" /></Tooltip>
          </ToggleButton>
          <ToggleButton value="strike" selected={editor.isActive('strike')} onClick={() => editor.chain().focus().toggleStrike().run()}>
            <Tooltip title="Üstü çizili"><FormatStrikethroughIcon fontSize="small" /></Tooltip>
          </ToggleButton>
        </ToggleButtonGroup>

        <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

        <ToggleButtonGroup size="small">
          <ToggleButton value="h3" selected={editor.isActive('heading', { level: 3 })} onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}>
            <Tooltip title="Başlık"><Box sx={{ fontSize: 13, fontWeight: 800, px: 0.5 }}>H3</Box></Tooltip>
          </ToggleButton>
          <ToggleButton value="h4" selected={editor.isActive('heading', { level: 4 })} onClick={() => editor.chain().focus().toggleHeading({ level: 4 }).run()}>
            <Tooltip title="Alt başlık"><Box sx={{ fontSize: 13, fontWeight: 800, px: 0.5 }}>H4</Box></Tooltip>
          </ToggleButton>
        </ToggleButtonGroup>

        <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

        <ToggleButtonGroup size="small">
          <ToggleButton value="bulletList" selected={editor.isActive('bulletList')} onClick={() => editor.chain().focus().toggleBulletList().run()}>
            <Tooltip title="Madde listesi"><FormatListBulletedIcon fontSize="small" /></Tooltip>
          </ToggleButton>
          <ToggleButton value="orderedList" selected={editor.isActive('orderedList')} onClick={() => editor.chain().focus().toggleOrderedList().run()}>
            <Tooltip title="Numaralı liste"><FormatListNumberedIcon fontSize="small" /></Tooltip>
          </ToggleButton>
        </ToggleButtonGroup>

        <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

        <ToggleButtonGroup size="small">
          <ToggleButton value="left" selected={editor.isActive({ textAlign: 'left' })} onClick={() => editor.chain().focus().setTextAlign('left').run()}>
            <Tooltip title="Sola hizala"><FormatAlignLeftIcon fontSize="small" /></Tooltip>
          </ToggleButton>
          <ToggleButton value="center" selected={editor.isActive({ textAlign: 'center' })} onClick={() => editor.chain().focus().setTextAlign('center').run()}>
            <Tooltip title="Ortala"><FormatAlignCenterIcon fontSize="small" /></Tooltip>
          </ToggleButton>
          <ToggleButton value="right" selected={editor.isActive({ textAlign: 'right' })} onClick={() => editor.chain().focus().setTextAlign('right').run()}>
            <Tooltip title="Sağa hizala"><FormatAlignRightIcon fontSize="small" /></Tooltip>
          </ToggleButton>
        </ToggleButtonGroup>

        <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

        <Tooltip title="Bağlantı ekle/düzenle">
          <IconButton size="small" color={editor.isActive('link') ? 'primary' : 'default'} onClick={setLink}>
            <LinkIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        {editor.isActive('link') && (
          <Tooltip title="Bağlantıyı kaldır">
            <IconButton size="small" onClick={() => editor.chain().focus().unsetLink().run()}>
              <LinkOffIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )}

        <Tooltip title="Renk">
          <IconButton size="small" onClick={(e) => setColorMenuAnchor(e.currentTarget)}>
            <FormatColorTextIcon
              fontSize="small"
              sx={{ color: editor.isActive('textColor') ? colors[(editor.getAttributes('textColor').token as RichTextColorToken) ?? 'accent'] : undefined }}
            />
          </IconButton>
        </Tooltip>
        <Menu anchorEl={colorMenuAnchor} open={Boolean(colorMenuAnchor)} onClose={() => setColorMenuAnchor(null)}>
          {(Object.keys(colors) as RichTextColorToken[]).map((token) => (
            <MenuItem
              key={token}
              onClick={() => {
                editor.chain().focus().setTextColor(token).run()
                setColorMenuAnchor(null)
              }}
            >
              <Box sx={{ width: 14, height: 14, borderRadius: '50%', bgcolor: colors[token], mr: 1.25 }} />
              {COLOR_LABELS[token]}
            </MenuItem>
          ))}
          <MenuItem
            onClick={() => {
              editor.chain().focus().unsetTextColor().run()
              setColorMenuAnchor(null)
            }}
          >
            Rengi kaldır
          </MenuItem>
        </Menu>

        <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

        <Tooltip title="Biçimi temizle">
          <IconButton size="small" onClick={() => editor.chain().focus().unsetAllMarks().clearNodes().run()}>
            <FormatClearIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Stack>

      <Box
        sx={{
          p: 1.5,
          minHeight: 140,
          '& .ProseMirror': { outline: 'none' },
          '& .ProseMirror p': { margin: 0, marginBottom: '0.5em' },
        }}
      >
        <EditorContent editor={editor} />
      </Box>
    </Box>
  )
}
