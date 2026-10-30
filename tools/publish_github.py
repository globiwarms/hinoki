"""Publish helper: uses the machine's stored GitHub credentials (git credential fill)
to call the REST API. The token never appears in output."""
import json
import subprocess
import sys
import urllib.request

API = "https://api.github.com"


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
    elif cmd == "release":
        status, rel = api("POST", "/repos/arseniyru90-afk/hinoki/releases", tok, {
            "tag_name": "v1.0.0",
            "target_commitish": "main",
            "name": "Hinoki v1.0.0",
            "body": (
                "## Первый релиз 檜\n\n"
                "**Hinoki** — средняя кнопка мыши распускает стеклянный цветок: мут в Discord, музыка, приложения.\n\n"
                "### Возможности\n"
                "- 6 настраиваемых лепестков: Discord мут/деафен, мут системы, Play/Pause, запуск .exe и steam://, ссылки, свои хоткеи\n"
                "- Настройки в центре цветка — живое превью, иконки, размер меню\n"
                "- Автозапуск с Windows, acrylic-размытие, сидит в трее\n"
                "- Ноль зависимостей: работает на любом Windows 10/11\n\n"
                "### Установка\n"
                "Скачайте `Hinoki.exe` ниже и запустите. Иконка появится в трее — средняя кнопка мыши откроет меню."
            ),
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
        status, body = api("POST", "/repos/arseniyru90-afk/hinoki/pages", tok, {
            "source": {"branch": "main", "path": "/docs"}
        })
        print(status, body if isinstance(body, dict) else body)
    elif cmd == "check":
        status, body = api("GET", "/repos/arseniyru90-afk/hinoki", tok)
        print(status, body.get("html_url") if isinstance(body, dict) else body)
