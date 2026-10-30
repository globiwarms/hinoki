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


RELEASE_11_BODY = (
    "## 檜 Hinoki v1.1.0\n\n"
    "### Главное\n"
    "- **Чистый цветок**: никакого затемнения экрана — меню появляется компактным окном ровно вокруг цветка\n"
    "- **Лепестки не наезжают** друг на друга и не уезжают с орбиты\n"
    "- **В центре — шестерёнка** настроек вместо логотипа\n"
    "- **29 действий**: мут/деафен Discord, громкость, треки, скриншоты, блокировка/сон/перезагрузка, диспетчер задач, параметры Windows, калькулятор, эмодзи, буфер обмена, steam://, ссылки, свои хоткеи\n"
    "- **Звуки стекла**: мягкий «блум» раскрытия и тик выбора — громкость и каждый звук настраиваются\n"
    "- **Внешний вид**: палитра акцентов, прозрачность лепестков, сила свечения, светлое стекло, размер меню, подписи\n"
    "- **Глобальный хоткей** открытия меню без мыши\n"
    "- **2–8 лепестков**: добавляйте и убирайте в настройках\n"
    "- **Клик мимо** закрывает меню, Esc — тоже; настройки — минималистичный glass-rail\n\n"
    "### Установка\n"
    "Скачайте `Hinoki.exe` и запустите. Шестерёнка в центре цветка — настройки.\n\n"
    "**Full Changelog**: https://github.com/arseniyru90-afk/hinoki/compare/v1.0.0...v1.1.0"
)


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
    elif cmd == "release11":
        status, rel = api("POST", "/repos/" + REPO + "/releases", tok, {
            "tag_name": "v1.1.0",
            "target_commitish": "main",
            "name": "Hinoki v1.1.0 — чистый цветок, 29 действий, звуки",
            "body": RELEASE_11_BODY,
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
