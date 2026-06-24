"""Demo server – FastAPI proxy for Image-to-3D Mock Agent demo UI.

Serves index.html and proxies API calls to the two local Azure Function Apps,
injecting the function key automatically (fetched from the admin API).
"""

import os
import asyncio
import logging
from pathlib import Path

import httpx
from fastapi import FastAPI, Request, HTTPException
from fastapi.responses import HTMLResponse, JSONResponse
from fastapi.middleware.cors import CORSMiddleware

log = logging.getLogger("demo")
logging.basicConfig(level=logging.INFO, format="%(levelname)s  %(message)s")

app = FastAPI(title="Image→3D Demo")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── configuration ─────────────────────────────────────────────────────────────
FUNCTIONS_IMAGE_BASE = os.getenv("FUNCTIONS_IMAGE_BASE", "http://localhost:7072")
FUNCTIONS_3D_BASE    = os.getenv("FUNCTIONS_3D_BASE",    "http://localhost:7071")
DEMO_PORT            = int(os.getenv("DEMO_PORT", "9000"))

_keys: dict[str, str] = {}   # {"image": "...", "model3d": "..."}

UI_FILE = Path(__file__).parent / "index.html"


# ── helpers ───────────────────────────────────────────────────────────────────
async def _fetch_host_key(base_url: str) -> str:
    """Return the first host key from the Functions admin API, or '' on failure."""
    try:
        async with httpx.AsyncClient(timeout=5) as client:
            r = await client.get(f"{base_url}/admin/host/keys")
            if r.status_code == 200:
                keys = r.json().get("keys", [])
                if keys:
                    return keys[0]["value"]
    except Exception as exc:
        log.warning("Could not fetch function key from %s: %s", base_url, exc)
    return ""


@app.on_event("startup")
async def _startup() -> None:
    _keys["image"]   = await _fetch_host_key(FUNCTIONS_IMAGE_BASE)
    _keys["model3d"] = await _fetch_host_key(FUNCTIONS_3D_BASE)
    log.info("image key  : %s", "OK" if _keys["image"]   else "MISSING")
    log.info("model3d key: %s", "OK" if _keys["model3d"] else "MISSING")


# ── UI ────────────────────────────────────────────────────────────────────────
@app.get("/", response_class=HTMLResponse)
async def serve_ui() -> HTMLResponse:
    return HTMLResponse(UI_FILE.read_text(encoding="utf-8"))


# ── proxy endpoints ───────────────────────────────────────────────────────────
@app.post("/api/generate-image")
async def proxy_generate_image(request: Request) -> JSONResponse:
    body = await request.json()
    async with httpx.AsyncClient(timeout=180) as client:
        r = await client.post(
            f"{FUNCTIONS_IMAGE_BASE}/generate-image",
            json=body,
            headers={"x-functions-key": _keys.get("image", "")},
        )
    return JSONResponse(content=r.json(), status_code=r.status_code)


@app.post("/api/generate-3d")
async def proxy_generate_3d(request: Request) -> JSONResponse:
    body = await request.json()
    async with httpx.AsyncClient(timeout=660) as client:
        r = await client.post(
            f"{FUNCTIONS_3D_BASE}/generate-3d",
            json=body,
            headers={"x-functions-key": _keys.get("model3d", "")},
        )
    return JSONResponse(content=r.json(), status_code=r.status_code)


# ── entry point ───────────────────────────────────────────────────────────────
if __name__ == "__main__":
    import uvicorn
    uvicorn.run("server:app", host="127.0.0.1", port=DEMO_PORT, reload=False, log_level="info")
