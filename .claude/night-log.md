# Night runner log

- 2026-06-30T19:30:00Z | #3 | night/2026-06-30 | Added TypeGlyphs type-channel + rendered role glyph in tooltip header; new EditMode test + FakeConverter | VERIFY: TMP atlas renders ⚔◈◆▸⇄↻ (else flip UseAsciiFallback) & run TypeGlyphsTests/ChainResolverTests
- 2026-06-30T21:06:30Z | #4 | night/2026-06-30 | Added DeliverySentence verb-led builder over Pattern×Affinity×Anchor(+radius); replaced AxesLine/word maps in tooltip; new EditMode table | VERIFY: run DeliverySentenceTests + ChainResolverTests; hover weapons read as sentences not robot output
- 2026-07-01T00:00:00Z | #5 | night/2026-07-01 | Factored the tooltip diff into pure PositionalDelta (Totals + Pieces); reframed weapon-hover as terminal totals + glyph piece list; trimmed payload output to own delivery+cost; new EditMode tests | VERIFY: compiles; run PositionalDeltaTests + ChainResolverTests; hover a driving weapon (terminal totals + piece list) and a payload (header just "Payload")
