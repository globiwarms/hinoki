"""Patch the landing page to v1.1: app-like orbit, gear center, fewer cards, no marquee."""
import io

# ================= index.html v1.1 =================
p = "docs/index.html"
h = io.open(p, encoding="utf-8").read()

# hero flower: petals on the app's orbit, gear in the center
start = h.index('  <div class="hero-flower')
end = h.index('  <div class="scroll-hint"')
new_flower = (
    '  <div class="hero-flower reveal d2" id="flower" aria-hidden="true">\n'
    '    <div class="petal p1"><div class="content"><span class="glyph g1"></span><span class="tag">Мут микро</span></div></div>\n'
    '    <div class="petal p2"><div class="content"><span class="glyph g2"></span><span class="tag">Мут звука ДС</span></div></div>\n'
    '    <div class="petal p3"><div class="content"><span class="glyph g3"></span><span class="tag">Мут ПК</span></div></div>\n'
    '    <div class="petal p4"><div class="content"><span class="glyph g4"></span><span class="tag">Музыка</span></div></div>\n'
    '    <div class="petal p5"><div class="content"><span class="glyph g5"></span><span class="tag">Приложение</span></div></div>\n'
    '    <div class="petal p6"><div class="content"><span class="glyph g6"></span><span class="tag">Ссылка</span></div></div>\n'
    '    <div class="center">\n'
    '      <span class="center-gear"></span>\n'
    '      <span class="center-badge">Настройки</span>\n'
    '    </div>\n'
    '  </div>\n\n'
)
h = h[:start] + new_flower + h[end:]

# remove marquee section
mstart = h.index('<!-- marquee -->')
mend = h.index('<!-- features -->')
h = h[:mstart] + h[mend:]

# update lede + micro
h = h.replace(
    "Нажмите среднюю кнопку мыши — распускается стеклянный цветок.\n      Мут микрофона в Discord, музыка, любимые приложения. Настраивается каждый лепесток.",
    "Средняя кнопка мыши — распускается стеклянный цветок. 29 действий,\n      мягкие звуки, своя палитра. Настройки — прямо в центре цветка.")
h = h.replace("Без установки · ~0,8 МБ · бесплатно и открыто",
              "Без установки · меньше 1 МБ · v1.1 · бесплатно и открыто")

# features: 4 minimal cards
fstart = h.index('<div class="cards">')
fend = h.index('</section>', fstart)
cards_new = (
    '<div class="cards">\n'
    '    <article class="card">\n'
    '      <div class="card-glyph"></div>\n'
    '      <h3>29 действий</h3>\n'
    '      <p>Мут и деафен в Discord, громкость, треки, скриншоты, блокировка, сон, калькулятор, эмодзи — на каждый лепесток.</p>\n'
    '    </article>\n'
    '    <article class="card">\n'
    '      <div class="card-glyph"></div>\n'
    '      <h3>Приложения и Steam</h3>\n'
    '      <p>Любой .exe, ярлык или steam://-ссылка. Аргументы запуска настраиваются.</p>\n'
    '    </article>\n'
    '    <article class="card">\n'
    '      <div class="card-glyph"></div>\n'
    '      <h3>Звуки стекла</h3>\n'
    '      <p>Мягкий «блум» при раскрытии, тик при выборе. Громкость и каждый звук — отдельно.</p>\n'
    '    </article>\n'
    '    <article class="card">\n'
    '      <div class="card-glyph"></div>\n'
    '      <h3>Свой характер</h3>\n'
    '      <p>Палитра акцентов, прозрачность, свечение, светлое стекло, размер меню, глобальный хоткей.</p>\n'
    '    </article>\n'
    '  </div>\n'
)
h = h[:fstart] + cards_new + h[fend:]

h = h.replace("Hinoki весит меньше фотографии и не требует установки. Запустили — он уже в трее, тихо и незаметно.",
              "Меньше 1 МБ, без установки. Запустили — он уже в трее.")
h = h.replace("Клик по центру цветка открывает настройки. Каждому лепестку — действие, иконка, название. Меняется всё на лету.",
              "Клик по шестерёнке — настройки. Лепесткам — действия, иконки, цвета. Всё меняется на лету.")
h = h.replace("Скачать Hinoki · v1.0", "Скачать Hinoki · v1.1")

io.open(p, "w", encoding="utf-8").write(h)
print("html v1.1 ok")

# ================= style.css v1.1 =================
p2 = "docs/style.css"
s = io.open(p2, encoding="utf-8").read()

s = s.replace(
""".petal {
  position: absolute;
  width: 40%;
  height: 35%;
  left: 30%;
  top: 8%;
  border-radius: 999px;""",
""".petal {
  position: absolute;
  width: 30%;
  height: 27%;
  left: 35%;
  top: 3%;
  border-radius: 999px;""")

s = s.replace("  transform-origin: 50% 120%;               /* petals hinge from the flower heart */",
              "  transform-origin: 50% 147%;               /* petals hinge from the flower heart */")

for old_a, new_a in [("30deg", "0deg"), ("90deg", "60deg"), ("150deg", "120deg"),
                     ("210deg", "180deg"), ("270deg", "240deg"), ("330deg", "300deg")]:
    s = s.replace(".p1 { transform: rotate(%s); }" % old_a if False else "", "")

# rotate pairs — do it by explicit replacements
pairs = [
    (".p1 { transform: rotate(30deg); }", ".p1 { transform: rotate(0deg); }"),
    (".p2 { transform: rotate(90deg); }", ".p2 { transform: rotate(60deg); }"),
    (".p3 { transform: rotate(150deg); }", ".p3 { transform: rotate(120deg); }"),
    (".p4 { transform: rotate(210deg); }", ".p4 { transform: rotate(180deg); }"),
    (".p5 { transform: rotate(270deg); }", ".p5 { transform: rotate(240deg); }"),
    (".p6 { transform: rotate(330deg); }", ".p6 { transform: rotate(300deg); }"),
    (".p1 .glyph, .p1 .tag { transform: rotate(-30deg); }", ".p1 .glyph, .p1 .tag { transform: rotate(0deg); }"),
    (".p2 .glyph, .p2 .tag { transform: rotate(-90deg); }", ".p2 .glyph, .p2 .tag { transform: rotate(-60deg); }"),
    (".p3 .glyph, .p3 .tag { transform: rotate(-150deg); }", ".p3 .glyph, .p3 .tag { transform: rotate(-120deg); }"),
    (".p4 .glyph, .p4 .tag { transform: rotate(-210deg); }", ".p4 .glyph, .p4 .tag { transform: rotate(-180deg); }"),
    (".p5 .glyph, .p5 .tag { transform: rotate(-270deg); }", ".p5 .glyph, .p5 .tag { transform: rotate(-240deg); }"),
    (".p6 .glyph, .p6 .tag { transform: rotate(-330deg); }", ".p6 .glyph, .p6 .tag { transform: rotate(-300deg); }"),
]
for old, new in pairs:
    assert old in s, old
    s = s.replace(old, new)

# center: gear button instead of logo image
cstart = s.index(".center {")
cend = s.index(".scroll-hint {")
gear_svg = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='black' stroke-width='1.7' stroke-linecap='round' stroke-linejoin='round'%3E%3Ccircle cx='12' cy='12' r='3.2'/%3E%3Cpath d='M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33h.01a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82v.01a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z'/%3E%3C/svg%3E"
new_center = (
".center {\n"
"  position: absolute; left: 50%; top: 50%;\n"
"  transform: translate(-50%, -50%);\n"
"  display: flex; flex-direction: column; align-items: center;\n"
"  z-index: 2;\n"
"}\n"
".center-gear {\n"
"  width: 96px; height: 96px; border-radius: 50%;\n"
"  background: linear-gradient(180deg, rgba(255, 255, 255, 0.16), rgba(232, 236, 242, 0.06));\n"
"  border: 1.4px solid rgba(255, 255, 255, 0.5);\n"
"  backdrop-filter: blur(18px);\n"
"  -webkit-backdrop-filter: blur(18px);\n"
"  box-shadow: 0 0 0 1.5px rgba(255, 255, 255, 0.14), 0 0 44px rgba(168, 184, 153, 0.4);\n"
"  animation: breathe 5s ease-in-out infinite;\n"
"  position: relative;\n"
"}\n"
".center-gear::after {\n"
"  content: \"\"; position: absolute; inset: 0;\n"
"  background: #fff;\n"
'  -webkit-mask: url("' + gear_svg + '") center / 34px no-repeat;\n'
'  mask: url("' + gear_svg + '") center / 34px no-repeat;\n'
"}\n"
"@keyframes breathe {\n"
"  0%, 100% { box-shadow: 0 0 0 1.5px rgba(255, 255, 255, 0.14), 0 0 44px rgba(168, 184, 153, 0.4); }\n"
"  50% { box-shadow: 0 0 0 1.5px rgba(255, 255, 255, 0.3), 0 0 64px rgba(168, 184, 153, 0.6); }\n"
"}\n"
".center-badge {\n"
"  margin-top: 12px;\n"
"  padding: 4.5px 14px 5.5px;\n"
"  border-radius: 999px;\n"
"  background: linear-gradient(180deg, #b7c6a7, var(--sage-deep));\n"
"  color: #1b2216;\n"
"  font-size: 11.5px; font-weight: 800;\n"
"  box-shadow: 0 6px 20px -6px rgba(150, 161, 130, 0.7);\n"
"}\n\n"
)
s = s[:cstart] + new_center + s[cend:]

io.open(p2, "w", encoding="utf-8").write(s)
print("css v1.1 ok")
