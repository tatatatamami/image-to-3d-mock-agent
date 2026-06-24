"""
TRELLIS Image-to-3D FastAPI wrapper (HuggingFace Space backend).

TRELLIS の推論を HuggingFace Space (microsoft/TRELLIS.2) に委譲します。
ローカル GPU / CUDA インストールは不要です。

API フロー（microsoft/TRELLIS.2）:
  1. /start_session  - ZeroGPU セッション初期化
  2. /preprocess_image - 背景除去・前処理
  3. /image_to_3d    - 3D 生成（状態はサーバー側セッションに保持）
  4. /extract_glb    - GLB 抽出（状態引数不要）

依存パッケージ:
    pip install fastapi uvicorn[standard] gradio_client pillow pydantic

起動:
    uvicorn trellis_api.main:app --host 0.0.0.0 --port 8080

環境変数:
    TRELLIS_HF_SPACE  : 使用する HuggingFace Space（デフォルト: microsoft/TRELLIS.2）
    HF_TOKEN          : HuggingFace トークン（省略可。混雑時の優先アクセスに使用）
"""

import io
import logging
import os
import tempfile
import urllib.request
from typing import Literal

import huggingface_hub
from fastapi import FastAPI, HTTPException
from fastapi.responses import Response
from gradio_client import Client, handle_file
from PIL import Image
from pydantic import BaseModel, field_validator

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# 設定
# ---------------------------------------------------------------------------
HF_SPACE: str = os.environ.get("TRELLIS_HF_SPACE", "microsoft/TRELLIS.2")
HF_TOKEN: str | None = os.environ.get("HF_TOKEN")

logger.info("HF token status: %s", "configured" if HF_TOKEN else "not configured")
logger.info("HF space: %s", HF_SPACE)

# HF_TOKEN が設定されていれば huggingface_hub にログイン（Client が自動で利用する）
if HF_TOKEN:
    logger.info("Attempting Hugging Face login with configured token")
    huggingface_hub.login(token=HF_TOKEN, add_to_git_credential=False)
else:
    logger.info("No HF token found; using anonymous access path")

app = FastAPI(title="TRELLIS Image-to-3D API (HuggingFace Space backend)")

QUALITY_STEPS: dict[str, int] = {
    "preview": 12,
    "standard": 25,
    "high": 50,
}


# ---------------------------------------------------------------------------
# Request スキーマ
# ---------------------------------------------------------------------------
class GenerateRequest(BaseModel):
    imageUrl: str
    outputFormat: Literal["glb"] = "glb"  # HF Space は GLB のみサポート
    quality: Literal["preview", "standard", "high"] = "preview"
    seed: int = 0
    # microsoft/TRELLIS.2 では decimation_target（ポリゴン数上限）を使用
    # meshSimplify (0.0-1.0) は後方互換のために残す（内部で decimationTarget に変換）
    meshSimplify: float = 0.95
    decimationTarget: int = 300000
    textureSize: int = 1024

    @field_validator("imageUrl")
    @classmethod
    def validate_image_url(cls, v: str) -> str:
        if not v.startswith(("http://", "https://")):
            raise ValueError("imageUrl must be an http/https URL")
        return v


# ---------------------------------------------------------------------------
# エンドポイント
# ---------------------------------------------------------------------------
@app.post(
    "/generate-3d",
    response_class=Response,
    responses={
        200: {
            "content": {"model/gltf-binary": {}},
            "description": "Generated GLB binary",
        }
    },
)
async def generate_3d(req: GenerateRequest) -> Response:
    """
    imageUrl で指定された画像を HuggingFace Space 上の TRELLIS で 3D に変換し、
    GLB バイナリを返す。
    """
    logger.info(
        "Generating 3D via HF Space=%s imageUrl=%s quality=%s token_status=%s",
        HF_SPACE,
        req.imageUrl,
        req.quality,
        "configured" if HF_TOKEN else "not configured",
    )

    # ---- 画像をダウンロードして一時ファイルに保存 -------------------------
    try:
        with urllib.request.urlopen(req.imageUrl, timeout=30) as resp:  # noqa: S310
            image_bytes = resp.read()
    except Exception as exc:
        logger.error("Failed to download image: %s", exc)
        raise HTTPException(
            status_code=422, detail=f"Failed to download imageUrl: {exc}"
        ) from exc

    try:
        image = Image.open(io.BytesIO(image_bytes)).convert("RGBA")
    except Exception as exc:
        raise HTTPException(
            status_code=422, detail=f"Failed to decode image: {exc}"
        ) from exc

    tmp_path: str | None = None
    try:
        # Gradio Client は URL か ローカルファイルパスを受け付ける
        with tempfile.NamedTemporaryFile(suffix=".png", delete=False) as tmp:
            tmp_path = tmp.name
            image.save(tmp_path, format="PNG")

        steps = QUALITY_STEPS[req.quality]

        # ---- Gradio Client で HuggingFace Space を呼び出す ----------------
        client = Client(HF_SPACE)

        # Step 0: ZeroGPU セッション初期化
        logger.info("Step 0: start_session")
        client.predict(api_name="/start_session")

        # Step 1: 背景除去などの前処理
        logger.info("Step 1: preprocess_image")
        preprocessed = client.predict(
            handle_file(tmp_path),
            api_name="/preprocess_image",
        )
        # preprocess_image は str（ローカルキャッシュパス）または dict を返す
        # image_to_3d には handle_file() でラップして渡す
        logger.info("Preprocessed type=%s value=%s", type(preprocessed), preprocessed)
        preprocessed_input = (
            handle_file(preprocessed) if isinstance(preprocessed, str) else preprocessed
        )

        # Step 2: 3D 生成（状態はサーバー側セッションに保持）
        logger.info("Step 2: image_to_3d (steps=%d)", steps)
        client.predict(
            preprocessed_input,  # image: FileData dict
            req.seed,         # seed
            "1024",           # resolution
            7.5,              # ss_guidance_strength
            0.7,              # ss_guidance_rescale
            steps,            # ss_sampling_steps
            5,                # ss_rescale_t
            7.5,              # shape_slat_guidance_strength
            0.5,              # shape_slat_guidance_rescale
            steps,            # shape_slat_sampling_steps
            3,                # shape_slat_rescale_t
            1,                # tex_slat_guidance_strength
            0,                # tex_slat_guidance_rescale
            steps,            # tex_slat_sampling_steps
            3,                # tex_slat_rescale_t
            api_name="/image_to_3d",
        )

        # Step 3: GLB 抽出（状態は前ステップからセッションに保持されている）
        logger.info("Step 3: extract_glb (decimation=%d texture=%d)",
                    req.decimationTarget, req.textureSize)
        glb_path, _glb_download = client.predict(
            req.decimationTarget,  # decimation_target
            req.textureSize,       # texture_size
            api_name="/extract_glb",
        )

        with open(glb_path, "rb") as f:
            asset_bytes = f.read()

    except HTTPException:
        raise
    except Exception as exc:
        logger.error("HuggingFace Space call failed: %s", exc)
        raise HTTPException(
            status_code=500, detail=f"TRELLIS Space error: {exc}"
        ) from exc
    finally:
        if tmp_path and os.path.exists(tmp_path):
            os.unlink(tmp_path)

    logger.info("Generated %d bytes (glb)", len(asset_bytes))
    return Response(content=asset_bytes, media_type="model/gltf-binary")


@app.get("/health")
async def health() -> dict:
    return {"status": "ok", "space": HF_SPACE}
