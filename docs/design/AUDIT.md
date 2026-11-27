# PeopleHub UI audit and D1 style guide (2026-11-27)

Status: D1 of the design phase. Proposal only. The application UI has not changed. All data is fictional.

## What exists today (measured on the repo at the stage 3 commit)
- One stylesheet, `wwwroot/css/site.css`, 230 lines, with 91 distinct hard-coded hex colours. Variables exist for the main colours only (`--brand` #5b5bd6, `--side` #0f1226, radius 14px).
- Fonts: Inter is named but not shipped, so it falls back to the system font on most devices. Arabic falls back to Noto Arabic or Tahoma, also unshipped. Arabic and Latin therefore look different on every device.
- No dark mode, no `:focus-visible` styles, no `prefers-reduced-motion` handling.
- Spacing, radius and type sizes are repeated literals, not tokens.
- 319 inline `style="..."` attributes across 63 views, so a visual change means editing views, not one stylesheet.
- Direction: the CSS already uses logical properties (no left/right margins or padding), which keeps Arabic right-to-left cheap. This is a strength to keep.
- Layout: dark indigo sidebar plus light content, cards with soft shadows, a demo banner. It is tidy but looks like many generic admin templates, and the brand does not say "people" or "Egypt".

## Proposal (D1)
Open the style guide at `/design/styleguide.html` (static, anonymous, fictional content). It has switches for palette (A, B, C), light and dark, English and Arabic.

- Palettes: **B Nile** (deep teal #0b7a85, warm sand background, amber accent) is recommended. **A Indigo** keeps today's brand, refined. **C Terracotta** is a warmer option. Dark variants exist for all three. Text and button pairs were checked for WCAG AA contrast.
- Type: Inter (Latin) and IBM Plex Sans Arabic (Arabic), both SIL OFL, served from `/fonts` with licence files. Tabular numerals for money and time.
- Tokens first (colour, spacing 4 to 48px, radius 8/12/18, motion 120 and 200ms). Components read tokens only. Logical properties everywhere.
- Components shown: buttons, inputs with error state, badges, KPI cards, table with avatar ring, empty state, skeleton loading, list and detail split, four alert kinds, app shell with sidebar.
- Accessibility: visible focus ring, reduced-motion respected, colour is never the only signal (badges carry text).
- Icons: the app's existing inline SVG icon set is kept for now; any addition must be open licence (Lucide, ISC).

## Not done (later units)
- D2: apply tokens to `site.css`, sign-in page, shell and dashboard, then each module in small pushed units with the 244 render check and pixel inspection.
- D3: dark mode toggle in the app (stored per user), inline styles moved to classes.
- Decision for Ahmed: pick palette A, B or C (B recommended). Everything else was delegated to the recommendation.
