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

## Agent Inspector でトレースとトークン集計を確認する

このリポジトリには Agent Inspector 対応のエントリーポイント
`trellis_api/inspector_agent.py` を追加しています。

### 1. 環境変数を設定

ルートの `.env.template` を参考に `.env` を作成し、以下を設定します。

| 変数 | 説明 |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Foundry Project の endpoint |
| `FOUNDRY_MODEL_DEPLOYMENT_NAME` | Foundry でデプロイ済みモデル名 |

### 2. 依存関係をインストール

```bash
pip install -r trellis_api/requirements.txt
```

### 3. VS Code から起動

Run and Debug で `Python: Attach to Inspector Agent` を実行します。

この構成では次を自動実行します。

- `agentdev` で `trellis_api/inspector_agent.py` を `--port 8088` で起動
- Agent Inspector を 8088 に接続して開く

### 4. 集計の確認

Agent Inspector でメッセージを送信すると、Traces で以下を確認できます。

- モデルターン
- ツール呼び出し
- 入力/出力トークン
- キャッシュ入力トークン
- 合計トークン
