"""End-to-end visual test for Hinoki: launch, middle-click, screenshot overlay + settings."""
import ctypes, subprocess, sys, time, os
from ctypes import wintypes

user32 = ctypes.windll.user32
try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except Exception:
    pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXE = os.path.join(ROOT, "build", "Hinoki.exe")
OUT = os.path.join(ROOT, "build")

class MOUSEINPUT(ctypes.Structure):
    _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long), ("mouseData", ctypes.c_ulong),
                ("dwFlags", ctypes.c_ulong), ("time", ctypes.c_ulong), ("dwExtraInfo", ctypes.POINTER(ctypes.c_ulong))]

class _INPUTunion(ctypes.Union):
    _fields_ = [("mi", MOUSEINPUT)]

class INPUT(ctypes.Structure):
    _fields_ = [("type", ctypes.c_ulong), ("union", _INPUTunion)]

INPUT_MOUSE = 0
MOUSEEVENTF_MIDDLEDOWN = 0x0020
MOUSEEVENTF_MIDDLEUP = 0x0040
MOUSEEVENTF_MOVE = 0x0001

def mouse(flags):
    inp = INPUT()
    inp.type = INPUT_MOUSE
    inp.union.mi = MOUSEINPUT(0, 0, 0, flags, 0, None)
    user32.SendInput(1, ctypes.byref(inp), ctypes.sizeof(INPUT))

def move(x, y):
    inp = INPUT()
    inp.type = INPUT_MOUSE
    inp.union.mi = MOUSEINPUT(x, y, 0, MOUSEEVENTF_MOVE | 0x8000, 0, None)  # absolute
    user32.SendInput(1, ctypes.byref(inp), ctypes.sizeof(INPUT))

def middle_click():
    mouse(MOUSEEVENTF_MIDDLEDOWN); time.sleep(0.03); mouse(MOUSEEVENTF_MIDDLEUP)

def shot(name):
    from PIL import ImageGrab
    img = ImageGrab.grab(all_screens=False)
    path = os.path.join(OUT, name)
    img.save(path)
    print("saved", path, img.size)

def key_esc():
    VK_ESC = 0x1B
    class KI(ctypes.Structure):
        _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort), ("dwFlags", ctypes.c_ulong),
                    ("time", ctypes.c_ulong), ("dwExtraInfo", ctypes.POINTER(ctypes.c_ulong))]
    class KUNION(ctypes.Union):
        _fields_ = [("ki", KI), ("pad", ctypes.c_ubyte * 32)]
    class KINPUT(ctypes.Structure):
        _fields_ = [("type", ctypes.c_ulong), ("u", KUNION)]
    down = KINPUT(); down.type = 1; down.u.ki = KI(VK_ESC, 0, 0, 0, None)
    up = KINPUT(); up.type = 1; up.u.ki = KI(VK_ESC, 0, 2, 0, None)
    arr = (KINPUT * 2)(down, up)
    user32.SendInput(2, arr, ctypes.sizeof(KINPUT))

# kill leftovers
subprocess.run(["taskkill", "/f", "/im", "Hinoki.exe"], capture_output=True)
time.sleep(0.5)

print("launching", EXE)
proc = subprocess.Popen([EXE])
time.sleep(2.0)

w = user32.GetSystemMetrics(0) // 2
h = user32.GetSystemMetrics(1) // 2
print("clicking middle at", w, h)

# 1) overlay test
move(w, h); time.sleep(0.2)
middle_click()
time.sleep(0.9)
shot("shot_overlay.png")

# move cursor away then esc
key_esc()
time.sleep(0.6)
shot("shot_after_close.png")

# 2) settings test via second instance --settings
subprocess.Popen([EXE, "--settings"])
time.sleep(1.4)
shot("shot_settings.png")

subprocess.run(["taskkill", "/f", "/im", "Hinoki.exe"], capture_output=True)
print("done")
