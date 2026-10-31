"""Publish helper: uses the machine's stored GitHub credentials (git credential fill)
to call the REST API. The token never appears in output."""
import json
import subprocess
import sys
import urllib.request

API = "https://api.github.com"
REPO = "arseniyru90-afk/hinoki"


def token():
    out = subprocess.run(
        ["git", "credential", "fill"],
        input="protocol=https\nhost=github.com\n\n",
        capture_output=True, text=True,
    ).stdout
    for line in out.splitlines():
        if line.startswith("password="):
            return line.split("=", 1)[1]
    raise SystemExit("no github credentials found")


def api(method, path, tok, payload=None, raw=None, content_type=None):
    url = API + path if path.startswith("/") else path
    data = None
    if payload is not None:
        data = json.dumps(payload).encode()
    elif raw is not None:
        data = raw
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", "token " + tok)
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("User-Agent", "hinoki-publisher")
    if content_type:
        req.add_header("Content-Type", content_type)
    elif payload is not None:
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req) as r:
            body = r.read()
            return r.status, json.loads(body) if body else None
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()[:600]


NL = chr(10)
RELEASE_12_BODY = NL.join([
    "## 檜 Hinoki v2.1.1",
    "",
    "### Редизайн настроек",
    "- Субтитры под заголовками вкладок — сразу понятно, что настраивается",
    "- Акцентная кнопка «+ Лепесток», выровненные отступы и разделители",
    "- Цветокоррекция фона и аккуратные пропорции по всей странице",
    "",
    "### Новые настройки",
    "- **Глубокая тема настроек** — ещё более тёмный фон окна",
    "- **Шестерёнка в центре** — можно скрыть кнопку настроек в цветке",
    "- **Уведомления о действиях** — всплывающие подсказки у часов",
    "- **Папка после скриншота** — автоматически открывает «Изображения»",
    "",
    "### Установка",
    "Скачайте `Hinoki.exe` и запустите. Если в трее висит старый Hinoki — «Выход», затем новый.",
    "",
    "**Full Changelog**: https://github.com/arseniyru90-afk/hinoki/compare/v2.1.0...v2.1.1",
])

if __name__ == "__main__":
    cmd = sys.argv[1]
    tok = token()
    if cmd == "create-repo":
        status, body = api("POST", "/user/repos", tok, {
            "name": "hinoki",
            "description": "檜 Средняя кнопка мыши — и всё под рукой. Liquid-glass цветок-меню для Windows: мут в Discord, музыка, приложения.",
            "private": False,
            "has_issues": True,
            "has_wiki": False,
        })
        print(status, body.get("full_name") if isinstance(body, dict) else body)
        if isinstance(body, dict) and body.get("full_name"):
            print("URL:", body["html_url"])
    elif cmd == "release211":
        status, rel = api("POST", "/repos/" + REPO + "/releases", tok, {
            "tag_name": "v2.1.1",
            "target_commitish": "main",
            "name": "Hinoki v2.1.1 — редизайн настроек, новые тумблеры",
            "body": RELEASE_12_BODY,
            "draft": False,
            "prerelease": False,
        })
        print(status, rel.get("html_url") if isinstance(rel, dict) else rel)
        if isinstance(rel, dict) and rel.get("upload_url"):
            up = rel["upload_url"].split("{")[0] + "?name=Hinoki.exe"
            data = open("build/Hinoki.exe", "rb").read()
            st, asset = api("POST", up, tok, raw=data,
                            content_type="application/vnd.microsoft.portable-executable")
            print(st, asset.get("browser_download_url") if isinstance(asset, dict) else asset)
    elif cmd == "pages":
        status, body = api("POST", "/repos/" + REPO + "/pages", tok, {
            "source": {"branch": "main", "path": "/docs"}
        })
        print(status, body if isinstance(body, dict) else body)
    elif cmd == "check":
        status, body = api("GET", "/repos/" + REPO, tok)
        print(status, body.get("html_url") if isinstance(body, dict) else body)
