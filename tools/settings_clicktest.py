"""Interactive click-through test of Hinoki settings: opens the window,
clicks rail tabs / dropdown / toggles via SendInput, screenshots each state."""
import ctypes, subprocess, time, os, sys
user32 = ctypes.windll.user32
try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except Exception:
    pass
from PIL import ImageGrab

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXE = os.path.join(ROOT, "build", "Hinoki.exe")
OUT = os.path.join(ROOT, "build")

# window metrics (96 dpi primary, workarea centered)
class RECTC(ctypes.Structure):
    _fields_ = [("L", ctypes.c_long), ("T", ctypes.c_long), ("R", ctypes.c_long), ("B", ctypes.c_long)]

wa = RECTC()
user32.SystemParametersInfoW(0x0048, 0, ctypes.byref(wa), 0)  # SPI_GETWORKAREA
WA_W, WA_H = wa.R - wa.L, wa.B - wa.T
WIN_W, WIN_H = 940, 610
LX = wa.L + (WA_W - WIN_W) // 2
LY = wa.T + (WA_H - WIN_H) // 2


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
    time.sleep(0.25)


def shot(name, box=None):
    img = ImageGrab.grab()
    if box:
        img = img.crop(box)
    img.save(os.path.join(OUT, name))
    print("shot", name)


def rail_click(index):
    # rail items start under the brand (~y=LY+212), step 40px
    y = LY + 120 + index * 36
    click(LX + 150, y)


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

# 1) petals tab (default)
shot("t1_petals.png")

# 2) look tab
rail_click(1)
time.sleep(0.8)
shot("t2_look.png")

# 3) sounds tab
rail_click(2)
time.sleep(0.8)
shot("t3_sounds.png")

# 4) behavior tab
rail_click(3)
time.sleep(0.8)
shot("t4_behavior.png")

# 5) about tab
rail_click(4)
time.sleep(0.8)
shot("t5_about.png")

# 6) back to petals: open action dropdown, then pick another action (must stay open)
rail_click(0)
time.sleep(0.6)
dropdown_y = LY + 339  # action dropdown row (window coords)
click(LX + 640, dropdown_y)
time.sleep(0.7)
shot("t6_dropdown_open.png")

# pick "Play / Pause — музыка" (item index 7) inside popup list
list_x = LX + 689
first_item_y = dropdown_y + 42
click(list_x, first_item_y + 7 * 33)
time.sleep(0.5)
shot("t7_dropdown_selected.png")
# close popup by clicking elsewhere
click(LX + 968, LY + 200)
time.sleep(0.4)

# 7) toggle "Перехватывать среднюю кнопку" on behavior page
rail_click(3)
time.sleep(0.6)
shot("t8_behavior_after.png")

subprocess.run(["taskkill", "/f", "/im", "Hinoki.exe"], capture_output=True)
print("done")
