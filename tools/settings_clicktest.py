"""v1.3.0 full click test: tabs, icon search typing, dropdown, screenshots."""
import ctypes, subprocess, time, os, json
user32 = ctypes.windll.user32
try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except Exception:
    pass
from PIL import ImageGrab

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXE = os.path.join(ROOT, "build", "Hinoki.exe")
OUT = os.path.join(ROOT, "build")


class MI(ctypes.Structure):
    _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long), ("m", ctypes.c_ulong),
                ("f", ctypes.c_ulong), ("t", ctypes.c_ulong), ("e", ctypes.POINTER(ctypes.c_ulong))]


class KI(ctypes.Structure):
    _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort), ("dwFlags", ctypes.c_ulong),
                ("time", ctypes.c_ulong), ("e", ctypes.POINTER(ctypes.c_ulong))]


class KU(ctypes.Union):
    _fields_ = [("ki", KI), ("pad", ctypes.c_ubyte * 32)]


class KINPUT(ctypes.Structure):
    _fields_ = [("t", ctypes.c_ulong), ("u", KU)]


class U(ctypes.Union):
    _fields_ = [("mi", MI)]


class I(ctypes.Structure):
    _fields_ = [("t", ctypes.c_ulong), ("u", U)]


def move(x, y):
    user32.SetCursorPos(int(x), int(y))
    time.sleep(0.06)


def click(x, y):
    move(x, y)
    i1 = I(); i1.t = 0; i1.u.mi = MI(0, 0, 0, 0x0002, 0, None)
    i2 = I(); i2.t = 0; i2.u.mi = MI(0, 0, 0, 0x0004, 0, None)
    user32.SendInput(1, ctypes.byref(i1), ctypes.sizeof(I))
    time.sleep(0.04)
    user32.SendInput(1, ctypes.byref(i2), ctypes.sizeof(I))
    time.sleep(0.25)


def type_text(text):
    arr = (KINPUT * (len(text) * 2))()
    for idx, ch in enumerate(text):
        d = arr[idx * 2]
        d.t = 1; d.u.ki = KI(0, ord(ch), 4, 0, None)
        u = arr[idx * 2 + 1]
        u.t = 1; u.u.ki = KI(0, ord(ch), 4 | 2, 0, None)
    user32.SendInput(len(text) * 2, arr, ctypes.sizeof(KINPUT))
    time.sleep(0.3)


def shot(name, box=None):
    img = ImageGrab.grab()
    if box:
        img = img.crop(box)
    img.save(os.path.join(OUT, name))
    print("shot", name)


def rail_click(index):
    click(LX + 150, LY + 120 + index * 36)


subprocess.run(["taskkill", "/f", "/im", "Hinoki.exe"], capture_output=True)
time.sleep(0.6)
subprocess.Popen([EXE, "--settings"])
time.sleep(2.6)

hwnd = user32.FindWindowW(None, "Hinoki")
assert hwnd, "settings window not found"


class WRECT(ctypes.Structure):
    _fields_ = [("L", ctypes.c_long), ("T", ctypes.c_long), ("R", ctypes.c_long), ("B", ctypes.c_long)]


wr = WRECT()
user32.GetWindowRect(hwnd, ctypes.byref(wr))
LX, LY = wr.L, wr.T
WIN_W, WIN_H = wr.R - wr.L, wr.B - wr.T
print("window at", LX, LY, WIN_W, WIN_H)

cfg_path = os.path.join(os.environ["APPDATA"], "Hinoki", "config.json")
cfg = json.load(open(cfg_path, encoding="utf-8"))
WINBOX = (LX, LY, LX + WIN_W, LY + WIN_H)
shot("t1_petals.png", WINBOX)

# icon search
click(LX + 700, LY + 213)
type_text("микрофон")
time.sleep(0.5)
shot("t_icon_search.png", WINBOX)
rail_click(0)
time.sleep(0.5)

# tabs
rail_click(1); time.sleep(0.7); shot("t2_look.png", WINBOX)
rail_click(2); time.sleep(0.7); shot("t3_sounds.png", WINBOX)
rail_click(3); time.sleep(0.7); shot("t4_behavior.png", WINBOX)
rail_click(4); time.sleep(0.7); shot("t5_about.png", WINBOX)

# dropdown stays open + select
rail_click(0)
time.sleep(0.6)
click(LX + 689, LY + 339)
time.sleep(0.7)
shot("t6_dropdown_open.png", WINBOX)
click(LX + 689, LY + 339 + 20 + 7 * 33)
time.sleep(0.5)
shot("t7_dropdown_selected.png", WINBOX)
click(LX + 689, LY + 200)
time.sleep(0.4)

subprocess.run(["taskkill", "/f", "/im", "Hinoki.exe"], capture_output=True)
time.sleep(0.5)
cfg["Petals"] = [
    {"Label": "Мут микро", "Icon": "E720", "Action": "discord_mute", "Target": "", "Args": ""},
    {"Label": "Мут звука ДС", "Icon": "E767", "Action": "discord_deafen", "Target": "", "Args": ""},
    {"Label": "Мут ПК", "Icon": "E74F", "Action": "volume_mute", "Target": "", "Args": ""},
    {"Label": "Музыка", "Icon": "E768", "Action": "media", "Target": "", "Args": ""},
    {"Label": "Приложение", "Icon": "E71D", "Action": "app", "Target": "", "Args": ""},
    {"Label": "Ссылка", "Icon": "E71B", "Action": "url", "Target": "", "Args": ""},
]
json.dump(cfg, open(cfg_path, "w", encoding="utf-8"), ensure_ascii=False)
subprocess.Popen([EXE])
print("done — config restored, app relaunched")
