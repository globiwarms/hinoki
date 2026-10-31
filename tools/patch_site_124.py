"""Site v1.2.4: lucide icons in cards, version bump, minimal copy fixes."""
import io

p = "docs/index.html"
h = io.open(p, encoding="utf-8").read()

# version strings
h = h.replace("v1.2 · бесплатно и открыто", "v1.2.4 · бесплатно и открыто")
h = h.replace("Скачать Hinoki · v1.2", "Скачать Hinoki · v1.2.4")
h = h.replace("Трей-утилита для Windows 10 / 11 · v1.2</p>",
              "Трей-утилита для Windows 10 / 11 · v1.2.4</p>")

# lucide icons in cards: replace empty glyph divs with inline SVG
ICONS = {
    1: '<svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 19v3"/><path d="M15 9.34V5a3 3 0 0 0-5.68-1.33"/><path d="M16.95 16.95A7 7 0 0 1 5 12v-2"/><path d="M18.89 13.23A7 7 0 0 0 19 12v-2"/><path d="m2 2 20 20"/><path d="M9 9v3a3 3 0 0 0 5.12 2.12"/></svg>',
    2: '<svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="4" width="20" height="16" rx="2"/><path d="M10 4v4"/><path d="M2 8h20"/><path d="M6 4v4"/></svg>',
    3: '<svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22a1 1 0 0 1 0-20 10 9 0 0 1 10 9 5 5 0 0 1-5 5h-2.25a1.75 1.75 0 0 0-1.4 2.8l.3.4a1.75 1.75 0 0 1-1.4 2.8z"/><circle cx="13.5" cy="6.5" r=".5" fill="currentColor"/><circle cx="17.5" cy="10.5" r=".5" fill="currentColor"/><circle cx="6.5" cy="12.5" r=".5" fill="currentColor"/><circle cx="8.5" cy="7.5" r=".5" fill="currentColor"/></svg>',
    4: '<svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 17H7A5 5 0 0 1 7 7h2"/><path d="M15 7h2a5 5 0 1 1 0 10h-2"/><line x1="8" x2="16" y1="12" y2="12"/></svg>',
}
for i in range(1, 5):
    h = h.replace('<div class="card-glyph" aria-hidden="true"></div>',
                  '<div class="card-glyph" aria-hidden="true">' + ICONS[i] + '</div>', 1)

# card titles/content update for 4 cards (accurate to v1.2.4)
h = h.replace("Мут и деафен в Discord, громкость, треки, скриншоты, блокировка, сон, калькулятор, эмодзи — на каждый лепесток.",
              "Мут и деафен в Discord, громкость, треки, скриншоты экрана и окна, блокировка, сон, папки, эмодзи — 33 действия.")
h = h.replace("Мягкий «блум» при раскрытии, тик при выборе. Громкость и каждый звук — отдельно.",
              "Мягкий «блум» при раскрытии, тик при выборе. Громкость и каждый звук — отдельно, с прослушиванием.")
h = h.replace("Палитра акцентов, прозрачность, свечение, светлое стекло, размер меню, глобальный хоткей.",
              "Палитра акцентов, прозрачность лепестков и центра, свечение, светлое стекло, хоткей, анимации.")

io.open(p, "w", encoding="utf-8").write(h)
print("index v1.2.4")

# css: card glyph hosts the svg now
p2 = "docs/style.css"
s = io.open(p2, encoding="utf-8").read()
s = s.replace(""".card-glyph::after {
  content: ""; position: absolute; inset: 0;
  background: currentColor;
  color: var(--sage-bright);
  -webkit-mask: var(--icon) center / 22px no-repeat;
  mask: var(--icon) center / 22px no-repeat;
}""",
""".card-glyph svg {
  position: absolute; inset: 0;
  width: 22px; height: 22px;
  margin: auto;
  color: var(--sage-bright);
}""")
# remove per-card --icon rules (obsolete)
import re
s = re.sub(r"\.card:nth-child\(\d\) \.card-glyph \{ --icon:[^}]+\}\n", "", s)
io.open(p2, "w", encoding="utf-8").write(s)
print("style v1.2.4")
