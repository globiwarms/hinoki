"""Generate app/src/LucideData.cs — lucide icons as WPF Path.Data strings."""
import io
import os
import re

SRC = r"C:\Users\arsen\.zcode\skills\lucide-icons\references\icons"
OUT = r"C:\Users\arsen\OneDrive\Desktop\Projects\Hinoki\app\src\LucideData.cs"

NAMES = [
    "mic", "mic-off", "volume-2", "volume-1", "volume-x", "play", "pause", "music",
    "link-2", "app-window", "settings", "camera", "image", "lock", "star", "keyboard",
    "globe", "folder", "folder-open", "file-text", "power", "monitor", "monitor-off",
    "search", "bell", "download", "upload", "trash-2", "copy", "clipboard",
    "refresh-cw", "pin", "moon", "calculator", "smile", "gamepad-2", "headphones",
    "activity", "minimize-2", "skip-forward", "skip-back", "message-circle",
    "rotate-cw", "link", "grid-2x2", "sun", "cpu", "sliders-horizontal",
]


def num(tok):
    return float(tok) if "." in tok else int(tok)


def conv(el, attrs):
    """Convert an svg primitive to a path 'd' fragment."""
    a = attrs
    if el == "path":
        return a.get("d", "")
    if el == "circle":
        cx, cy, r = num(a["cx"]), num(a["cy"]), num(a["r"])
        return f"M{cx - r},{cy} a{r},{r} 0 1,0 {2 * r},0 a{r},{r} 0 1,0 {-2 * r},0"
    if el == "line":
        return f"M{num(a['x1'])},{num(a['y1'])} L{num(a['x2'])},{num(a['y2'])}"
    if el == "rect":
        x, y = num(a.get("x", 0)), num(a.get("y", 0))
        w, h = num(a["width"]), num(a["height"])
        rx = num(a.get("rx", 0)) or num(a.get("ry", 0))
        if rx <= 0:
            return f"M{x},{y} H{x + w} V{y + h} H{x} Z"
        return (f"M{x + rx},{y} H{x + w - rx} A{rx},{rx} 0 0 1 {x + w},{y + rx} "
                f"V{y + h - rx} A{rx},{rx} 0 0 1 {x + w - rx},{y + h} H{x + rx} "
                f"A{rx},{rx} 0 0 1 {x},{y + h - rx} V{y + rx} A{rx},{rx} 0 0 1 {x + rx},{y} Z")
    if el == "polyline":
        pts = a["points"].strip().split()
        d = "M" + " L".join(pts)
        return d
    if el == "polygon":
        pts = a["points"].strip().split()
        return "M" + " L".join(pts) + " Z"
    return ""


entries = []
for name in NAMES:
    fp = os.path.join(SRC, name + ".svg")
    svg = io.open(fp, encoding="utf-8").read()
    parts = []
    for m in re.finditer(r"<(path|circle|line|rect|polyline|polygon)\b([^>]*)/?>", svg):
        attrs = dict(re.findall(r'([\w-]+)="([^"]*)"', m.group(2)))
        d = conv(m.group(1), attrs)
        if d:
            parts.append(d)
    d_all = " ".join(parts)
    entries.append('            { "%s", "%s" }' % (name, d_all))
    print(name, len(d_all), "chars")

body = ",\n".join(entries)
cs = (
    "// Hinoki — Lucide icon geometry (converted from lucide-static, ISC license)\n"
    "// Rendered as stroked Path geometry; 24x24 viewBox.\n"
    "using System;\n"
    "using System.Collections.Generic;\n"
    "using System.Windows.Media;\n"
    "\n"
    "namespace Hinoki\n"
    "{\n"
    "    public static class Lucide\n"
    "    {\n"
    "        static readonly Dictionary<string, Geometry> Cache = new Dictionary<string, Geometry>();\n"
    "\n"
    "        static readonly Dictionary<string, string> Paths = new Dictionary<string, string>\n"
    "        {\n"
    + body + "\n"
    "        };\n"
    "\n"
    "        public static Geometry Get(string name)\n"
    "        {\n"
    "            Geometry g;\n"
    "            if (Cache.TryGetValue(name, out g)) return g;\n"
    "            string d;\n"
    "            if (!Paths.TryGetValue(name, out d)) d = Paths[\"settings\"];\n"
    "            g = Geometry.Parse(d);\n"
    "            g.Freeze();\n"
    "            Cache[name] = g;\n"
    "            return g;\n"
    "        }\n"
    "\n"
    "        public static bool Has(string name)\n"
    "        {\n"
    "            return Paths.ContainsKey(name);\n"
    "        }\n"
    "    }\n"
    "}\n"
)
io.open(OUT, "w", encoding="utf-8").write(cs)
print("written", OUT, len(cs), "chars")
