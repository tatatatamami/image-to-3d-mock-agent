"""
3D モック生成 Foundry Agent

自然言語または参考画像を元に AI 画像を生成し、
さらに 3D モデル（GLB）に変換してユーザーに届ける
エンドツーエンドのオーケストレーションエージェント。

必要な環境変数（.env ファイルに記載）:
    FOUNDRY_PROJECT_ENDPOINT      : Azure AI Foundry プロジェクトエンドポイント
    FOUNDRY_MODEL_DEPLOYMENT_NAME : 使用するモデルのデプロイ名
    IMAGE_FUNCTIONS_ENDPOINT      : ImageTo3DMockAgent.Functions のベース URL
    GENERATE_3D_ENDPOINT          : ImageTo3DMockAgent.Api のベース URL
    FUNCTIONS_API_KEY             : Azure Functions のアクセスキー（省略可）
"""

import asyncio
import json
import os
import urllib.error
import urllib.request
from typing import Annotated

from agent_framework.azure import AzureAIClient
from azure.ai.agentserver.agentframework import from_agent_framework
from azure.identity.aio import AzureCliCredential
from dotenv import load_dotenv

load_dotenv(override=False)

# エンドポイント設定（起動前に環境変数で上書き）
_IMAGE_FUNCTIONS_ENDPOINT: str = os.environ.get(
    "IMAGE_FUNCTIONS_ENDPOINT", "http://localhost:7072"
)
_GENERATE_3D_ENDPOINT: str = os.environ.get(
    "GENERATE_3D_ENDPOINT", "http://localhost:7071"
)
_FUNCTIONS_API_KEY: str = os.environ.get("FUNCTIONS_API_KEY", "")


# ---------------------------------------------------------------------------
# HTTP ヘルパー
# ---------------------------------------------------------------------------

def _post_json(url: str, payload: dict, timeout: int = 180) -> dict:
    """JSON POST を送信し、レスポンス dict を返す。エラー時は例外を送出。"""
    data = json.dumps(payload).encode("utf-8")
    headers: dict[str, str] = {"Content-Type": "application/json"}
    if _FUNCTIONS_API_KEY:
        headers["x-functions-key"] = _FUNCTIONS_API_KEY
    req = urllib.request.Request(url, data=data, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:  # noqa: S310
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(
            f"HTTP {exc.code} from {url}: {body[:300]}"
        ) from exc


# ---------------------------------------------------------------------------
# ツール定義
# ---------------------------------------------------------------------------

def generate_image(
    prompt: Annotated[
        str,
        "生成したいモック画像の説明（自然言語）。"
        "TRELLIS が 3D 変換しやすいよう「白背景・正面向き・シンプルな構図」を意識したプロンプトを渡すこと。",
    ],
    reference_image_url: Annotated[
        str | None,
        "スタイル参照用の画像 URL（http/https）。省略可。"
        "指定した場合は image edit API を試みる。",
    ] = None,
    size: Annotated[
        str,
        "出力画像サイズ。1024x1024 / 1024x1792 / 1792x1024",
    ] = "1024x1024",
) -> str:
    """
    Azure OpenAI（gpt-image-2）でモックイメージを生成し、Blob Storage に保存する。
    成功した場合は生成画像の URL と Blob パスを返す。
    失敗した場合はエラー内容を返す。
    """
    url = _IMAGE_FUNCTIONS_ENDPOINT.rstrip("/") + "/generate-image"
    payload: dict = {"prompt": prompt, "size": size}
    if reference_image_url:
        payload["referenceImageUrl"] = reference_image_url

    try:
        result = _post_json(url, payload)
        return (
            f"画像生成完了。\n"
            f"  imageUrl: {result['imageUrl']}\n"
            f"  imageBlobPath: {result['imageBlobPath']}"
        )
    except Exception as exc:  # noqa: BLE001
        return f"画像生成失敗: {exc}"


def generate_3d_model(
    image_url: Annotated[
        str,
        "3D 変換する画像の URL（http/https）。generate_image ツールで得た imageUrl を渡す。",
    ],
    quality: Annotated[
        str,
        "変換品質。preview（高速・デモ向け）/ standard / high",
    ] = "preview",
    output_format: Annotated[
        str,
        "出力形式。glb（デフォルト）/ obj",
    ] = "glb",
) -> str:
    """
    TRELLIS API で画像を 3D モデルに変換し、Blob Storage に保存する。
    成功した場合は 3D モデルの URL と Blob パスを返す。
    失敗した場合はエラー内容を返す。
    注意: HuggingFace ZeroGPU の混雑状況によっては数分かかる場合がある。
    """
    url = _GENERATE_3D_ENDPOINT.rstrip("/") + "/generate-3d"
    payload = {
        "imageUrl": image_url,
        "quality": quality,
        "outputFormat": output_format,
    }

    try:
        result = _post_json(url, payload)
        return (
            f"3D モデル生成完了。\n"
            f"  modelUrl: {result['modelUrl']}\n"
            f"  modelBlobPath: {result['modelBlobPath']}\n"
            f"  sourceImageUrl: {result['sourceImageUrl']}"
        )
    except Exception as exc:  # noqa: BLE001
        return f"3D モデル生成失敗: {exc}"


# ---------------------------------------------------------------------------
# エージェント指示
# ---------------------------------------------------------------------------

_INSTRUCTIONS = """\
あなたは「3D モック生成アシスタント」です。
ユーザーの要望を聞き、AI 画像生成 → 3D モデル変換の 2 ステップで 3D モックを作成します。

【作業手順】
1. ユーザーから作りたいモックの説明を確認する
   - テキストのみ、または参考画像 URL + テキストの両方に対応
   - 不明な点があれば簡潔に質問する（1〜2 点に絞る）

2. generate_image ツールで AI 画像を生成する
   - TRELLIS が最も精度よく 3D 変換できるプロンプトに変換する
     （例: 「白背景・正面向き・シングルオブジェクト・影なし」）
   - 参考画像がある場合は reference_image_url に渡す

3. generate_image の結果から imageUrl を取り出し、
   generate_3d_model ツールで 3D モデルに変換する
   - デモでは quality="preview" を使い処理時間を短縮する

4. 3D モデルの URL をユーザーに提示し、ダウンロード方法を案内する

【注意事項】
- TRELLIS による変換は混雑時に 2〜5 分かかることがある。ユーザーに伝えながら待つ
- いずれかのステップが失敗した場合はエラー内容をユーザーに伝え、
  リトライするか確認する
- モデルの URL は Blob Storage に保存されるため、しばらくアクセス可能である
"""


# ---------------------------------------------------------------------------
# エントリポイント
# ---------------------------------------------------------------------------

async def run_server() -> None:
    project_endpoint = os.getenv("FOUNDRY_PROJECT_ENDPOINT")
    deployment = os.getenv("FOUNDRY_MODEL_DEPLOYMENT_NAME")

    if not project_endpoint or not deployment:
        raise RuntimeError(
            "Missing Foundry settings. "
            "Set FOUNDRY_PROJECT_ENDPOINT and FOUNDRY_MODEL_DEPLOYMENT_NAME "
            "in environment variables or .env."
        )

    async with (
        AzureCliCredential() as credential,
        AzureAIClient(
            project_endpoint=project_endpoint,
            model_deployment_name=deployment,
            credential=credential,
        ).as_agent(
            name="mock3d-agent",
            instructions=_INSTRUCTIONS,
            tools=[generate_image, generate_3d_model],
        ) as agent,
    ):
        await from_agent_framework(agent).run_async()


if __name__ == "__main__":
    asyncio.run(run_server())


if __name__ == "__main__":
    asyncio.run(run_server())
