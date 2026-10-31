"""v2.1 settings test: picker expand, icon change double-icon check, tabs."""
import ctypes, subprocess, time, os
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
    time.sleep(0.3)


def shot(name, box=None):
    img = ImageGrab.grab()
    if box:
        img = img.crop(box)
    img.save(os.path.join(OUT, name))
    print("shot", name)


subprocess.run(["powershell", "-NoProfile", "-Command",
                "Stop-Process -Name Hinoki -Force -ErrorAction SilentlyContinue"])
time.sleep(1.2)
subprocess.Popen([EXE, "--settings"])
time.sleep(2.8)

hwnd = user32.FindWindowW(None, "Hinoki")
assert hwnd, "no settings window"


class WRECT(ctypes.Structure):
    _fields_ = [("L", ctypes.c_long), ("T", ctypes.c_long), ("R", ctypes.c_long), ("B", ctypes.c_long)]


wr = WRECT()
user32.GetWindowRect(hwnd, ctypes.byref(wr))
LX, LY = wr.L, wr.T
user32.SetWindowPos(hwnd, -1, 0, 0, 0, 0, 0x0013)
user32.SetForegroundWindow(hwnd)
time.sleep(0.3)
WINBOX = (LX, LY, LX + 940, LY + 610)

# 1) petals: collapsed picker + new logo
shot("v21_petals.png", WINBOX)

# 2) expand icons (button at window y~327)
click(LX + 700, LY + 327)
time.sleep(0.7)
shot("v21_expanded.png", WINBOX)

# 3) pick "star" tile (row 3, col 2 of expanded grid: tiles start y~246, step 42/46)
click(LX + 541, LY + 351)
time.sleep(0.6)
shot("v21_icon_star.png", WINBOX)

# 4) pick mic back (row 1 col 1)
click(LX + 518, LY + 267)
time.sleep(0.6)
shot("v21_icon_mic_back.png", WINBOX)

# collapse back (button moved down after expansion)
click(LX + 700, LY + 508)
time.sleep(0.4)

# 5) look tab
click(LX + 150, LY + 156)
time.sleep(0.7)
shot("v21_look.png", WINBOX)

subprocess.run(["powershell", "-NoProfile", "-Command",
                "Stop-Process -Name Hinoki -Force -ErrorAction SilentlyContinue"])
print("done")
