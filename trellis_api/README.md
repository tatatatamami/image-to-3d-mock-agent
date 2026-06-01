# TRELLIS Image-to-3D FastAPI Wrapper

**HuggingFace Space バックエンド版。ローカル GPU / CUDA インストール不要。**

推論は [microsoft/TRELLIS.2](https://huggingface.co/spaces/microsoft/TRELLIS.2) HuggingFace Space
（`gradio_client` 経由）で実行します。

## 動作環境

| 要件 | 詳細 |
|---|---|
| OS | Windows / Linux / macOS |
| GPU | 不要（推論は HuggingFace Space で実行） |
| Python | 3.10 以上 |

## セットアップ

```bash
cd trellis_api

# 依存パッケージをインストール（軽量）
pip install -r requirements.txt
```

## 起動

```bash
# 標準起動（ポート 8080）
uvicorn trellis_api.main:app --host 0.0.0.0 --port 8080

# 別の HuggingFace Space を使う場合
TRELLIS_HF_SPACE=<owner/space-name> uvicorn trellis_api.main:app --host 0.0.0.0 --port 8080
```

起動後、`http://localhost:8080/health` でヘルスチェックできます。

## 環境変数

| 変数 | デフォルト | 説明 |
|---|---|---|
| `TRELLIS_HF_SPACE` | `microsoft/TRELLIS.2` | 使用する HuggingFace Space |
| `HF_TOKEN` | （なし） | HuggingFace アクセストークン（省略可。混雑時の優先アクセスに使用） |

## エンドポイント

### POST /generate-3d

```json
{
  "imageUrl": "https://example.com/image.png",
  "outputFormat": "glb",
  "quality": "preview",
  "seed": 0,
  "meshSimplify": 0.95,
  "textureSize": 1024
}
```

| フィールド | 型 | デフォルト | 説明 |
|---|---|---|---|
| `imageUrl` | string | 必須 | 変換する画像の URL（http/https） |
| `outputFormat` | `"glb"` | `"glb"` | 出力フォーマット（現在は GLB のみ） |
| `quality` | `"preview"` \| `"standard"` \| `"high"` | `"preview"` | 生成品質（サンプリングステップ数: 12/25/50） |
| `seed` | int | `0` | 乱数シード |
| `decimationTarget` | int | `300000` | ポリゴン数上限（小さいほど軽量） |
| `textureSize` | int | `1024` | テクスチャ解像度 |

レスポンス: GLB バイナリ（`Content-Type: model/gltf-binary`）

### GET /health

```json
{ "status": "ok", "space": "microsoft/TRELLIS.2" }
```

## .NET アプリ側の設定

`src/ImageTo3DMockAgent.Api/local.settings.json` の `IMAGE_TO_3D_API_ENDPOINT` を
ローカルサーバーの URL に設定します:

```json
"IMAGE_TO_3D_API_ENDPOINT": "http://localhost:8080"
```
