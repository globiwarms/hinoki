"""Generate hinoki.ico (multi-size) + web assets from the source logo."""
from PIL import Image
import shutil, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "app", "assets", "logo.png")

img = Image.open(SRC).convert("RGBA")

# Multi-resolution .ico for the exe + tray
sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)]
frames = []
for s in sizes:
    f = img.resize(s, Image.LANCZOS)
    frames.append(f)
ico_path = os.path.join(ROOT, "app", "assets", "hinoki.ico")
frames[0].save(ico_path, format="ICO", append_images=frames[1:], sizes=sizes)
print("wrote", ico_path, os.path.getsize(ico_path), "bytes")

# Site favicon + hero logo
docs = os.path.join(ROOT, "docs")
shutil.copy(os.path.join(ROOT, "app", "assets", "logo.svg"), os.path.join(docs, "favicon.svg"))
web = img.resize((512, 512), Image.LANCZOS)
web.save(os.path.join(docs, "logo512.png"))
print("wrote favicon.svg + logo512.png")
