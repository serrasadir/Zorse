"""Sonraki iOS build numarası (GitHub Actions, testflight.yml).

max(App Store Connect'e yüklenmiş son build'ler, ProjectSettings iPhone build no) + 1.
Ortam: ASC_KEY_ID, ASC_ISSUER_ID, ASC_KEY_PATH (.p8). Çıktı: sadece sayı.
"""
import json
import os
import re
import time
import urllib.parse
import urllib.request

import jwt

BUNDLE_ID = "com.zorsestudio.leftnocrumbs"
API = "https://api.appstoreconnect.apple.com/v1/"


def get(path, token, **params):
    url = API + path + ("?" + urllib.parse.urlencode(params) if params else "")
    req = urllib.request.Request(url, headers={"Authorization": "Bearer " + token})
    with urllib.request.urlopen(req) as r:
        return json.load(r)["data"]


def main():
    now = int(time.time())
    token = jwt.encode({"iss": os.environ["ASC_ISSUER_ID"], "iat": now, "exp": now + 600, "aud": "appstoreconnect-v1"},
                       open(os.environ["ASC_KEY_PATH"]).read(), algorithm="ES256",
                       headers={"kid": os.environ["ASC_KEY_ID"], "typ": "JWT"})

    app_id = get("apps", token, **{"filter[bundleId]": BUNDLE_ID})[0]["id"]
    builds = get("builds", token, **{"filter[app]": app_id, "sort": "-uploadedDate", "limit": "20"})
    uploaded = max((int(b["attributes"]["version"]) for b in builds), default=0)

    settings = open("ProjectSettings/ProjectSettings.asset").read()
    local = int(re.search(r"buildNumber:\s*\n(?:\s+\w+:.*\n)*?\s+iPhone: (\d+)", settings).group(1))

    print(max(uploaded, local) + 1)


if __name__ == "__main__":
    main()
