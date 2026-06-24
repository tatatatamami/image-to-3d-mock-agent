# image-to-3d-mock-agent アプリケーション仕様書

## 1. 概要

ユーザーが自然言語や参考画像を入力すると、Microsoft Foundry 上の Agent がその意図を解釈してモックイメージを AI 生成し、さらにその画像を 3D モデル（GLB 形式）に変換してユーザーへ届けるエンドツーエンドのシステム。

**ユーザーストーリー:**
> ユーザーが「こんな形の商品モックが欲しい」と自然言語（または参考画像）で伝えると、Agent が AI で画像を生成し、その画像から 3D モデルを作成して返してくれる。

---

## 2. エンドツーエンドフロー

```
ユーザー
  │  自然言語 / 参考画像
  ▼
┌─────────────────────────────────────────────────────────┐
│  Microsoft Foundry Agent                                │
│  （ユーザー意図の解釈・ツール呼び出しのオーケストレーション）│
└────────┬─────────────────────────┬───────────────────────┘
         │ ① 画像生成ツール呼び出し  │ ② 3D変換ツール呼び出し
         ▼                          ▼
┌──────────────────┐    ┌──────────────────────────────────┐
│ ImageTo3DMock    │    │  ImageTo3DMockAgent.Api           │
│ Agent.Functions  │    │  (Azure Functions v4 / .NET 8)   │
│ ─────────────── │    │  ────────────────────────────── │
│ POST             │    │  POST /api/generate-3d           │
│  /api/generate-  │    │  ┌──────────────────────────┐   │
│  image           │    │  │ MockGenerate3DAssetService│   │
│                  │    │  │  (エンドポイント未設定時)  │   │
│  Azure OpenAI    │    │  ├──────────────────────────┤   │
│  gpt-image-2     │    │  │TrellisGenerate3DAsset     │   │
│  ↓               │    │  │Service (エンドポイント    │   │
│  Blob Storage    │    │  │ 設定時)                   │   │
│  (images/)       │    │  └────────────┬─────────────┘   │
└──────────────────┘    └───────────────┼──────────────────┘
         │ 生成画像 URL                   │ POST /generate-3d
         └──────────────────────────────┘
                                         ▼
                        ┌────────────────────────────────┐
                        │  trellis_api (Python FastAPI)  │
                        │  ポート 8080                    │
                        └────────────────┬───────────────┘
                                         │ gradio_client
                                         ▼
                        ┌────────────────────────────────┐
                        │  HuggingFace Space             │
                        │  microsoft/TRELLIS.2           │
                        └────────────────────────────────┘
                                         │ GLB バイナリ
                                         ▼
                                  Blob Storage
                                  (models/)
                                         │ モデル URL
                                         ▼
                                      ユーザー

┌──────────────────────────────────────────────────────────┐
│  inspector_agent.py  (デバッグ用 Foundry Agent)           │
│  ポート 8088                                              │
│  ツール: get_service_status / get_quality_steps          │
└──────────────────────────────────────────────────────────┘
```

---

## 3. 処理ステップ詳細

### Step 1: ユーザー入力

ユーザーは Foundry Agent に以下いずれかの形式で入力する。

| 入力形式 | 例 |
|---|---|
| 自然言語テキスト | 「角が丸い白い箱型の商品モックを作って」 |
| 参考画像 + テキスト | 画像ファイルを添付し「このデザインで 3D モックを作って」 |

### Step 2: AI 画像生成（`ImageTo3DMockAgent.Functions`）

Foundry Agent が `POST /api/generate-image` を呼び出す。

- **内部処理**: Azure OpenAI `gpt-image-2` にプロンプトを送信し、TRELLIS 向けに最適化されたモックイメージを生成する
- **出力**: 生成画像を Azure Blob Storage の `images/` コンテナに保存し、Blob URL を返す

### Step 3: 3D モデル生成（`ImageTo3DMockAgent.Api` → `trellis_api`）

Foundry Agent が Step 2 で得た画像 URL を使って `POST /api/generate-3d` を呼び出す。

- **内部処理**: trellis_api が HuggingFace Space `microsoft/TRELLIS.2` に推論を委譲し GLB を生成する
- **出力**: 生成された GLB を Azure Blob Storage の `models/` コンテナに保存し、モデル URL を返す

### Step 4: ユーザーへ返却

Foundry Agent が 3D モデルの URL（または埋め込み）をユーザーに提示する。

---

## 3. コンポーネント詳細

### 3.1 ImageTo3DMockAgent.Functions（.NET Azure Functions v4）

| 項目 | 値 |
|---|---|
| ランタイム | .NET 8 / dotnet-isolated |
| フレームワーク | Azure Functions v4 |
| ポート（ローカル）| 7072 |
| 役割 | Step 2: AI 画像生成 |
| 実装状況 | 完成 |

#### 3.1.1 エンドポイント

**`POST /api/generate-image`**

リクエストボディ（JSON）:

| フィールド | 型 | 必須 | 説明 |
|---|---|---|---|
| `prompt` | string | 必須 | 生成したい画像の自然言語説明 |
| `referenceImageUrl` | string | 任意 | 参考画像の URL（スタイル転写に使用） |
| `size` | string | 任意 | 出力画像サイズ（デフォルト: `1024x1024`） |

レスポンスボディ（JSON）:

| フィールド | 型 | 説明 |
|---|---|---|
| `imageUrl` | string | 生成・保存された画像の Blob URL |
| `imageBlobPath` | string | Blob Storage 上の相対パス |

#### 3.1.2 設定項目

| 環境変数 | 説明 | ローカルデフォルト |
|---|---|---|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI エンドポイント | `https://...services.ai.azure.com/` |
| `AZURE_OPENAI_IMAGE_DEPLOYMENT` | 画像生成モデルのデプロイ名 | `gpt-image-2` |
| `AzureWebJobsStorage` | Blob Storage 接続文字列 | `UseDevelopmentStorage=true` |
| `BlobStorage__ConnectionString` | 画像保存先 Blob Storage 接続文字列 | `UseDevelopmentStorage=true` |
| `BlobStorage__ContainerName` | 画像保存先コンテナ名 | `assets` |

---

### 3.2 ImageTo3DMockAgent.Api（.NET Azure Functions v4）

| 項目 | 値 |
|---|---|
| ランタイム | .NET 8 / dotnet-isolated |
| フレームワーク | Azure Functions v4 |
| ポート（ローカル）| 7071（Functions デフォルト） |

#### 3.1.1 エンドポイント

**`POST /api/generate-3d`**

リクエストボディ（JSON）:

| フィールド | 型 | 必須 | 説明 |
|---|---|---|---|
| `imageUrl` | string | imageUrl または imageBlobPath のいずれか必須 | 元画像の絶対 URL（http/https） |
| `imageBlobPath` | string | imageUrl または imageBlobPath のいずれか必須 | Azure Blob Storage 上の相対パス（パストラバーサル禁止） |
| `outputFormat` | string | 任意 | `glb`（デフォルト）または `obj` |
| `quality` | string | 任意 | `preview`（デフォルト）/ `standard` / `high` |

レスポンスボディ（JSON）:

| フィールド | 型 | 説明 |
|---|---|---|
| `modelUrl` | string | 生成された 3D モデルの URL |
| `modelBlobPath` | string | Blob Storage 上のモデルパス |
| `sourceImageUrl` | string | 元画像の URL |

バリデーションエラー時（400 Bad Request）:

```json
{
  "errors": {
    "imageUrl": ["Either imageUrl or imageBlobPath is required."],
    "outputFormat": ["outputFormat must be one of: glb, obj."]
  }
}
```

#### 3.2.2 サービス切り替えロジック

`Program.cs` が起動時に `IMAGE_TO_3D_API_ENDPOINT`（または `Trellis:ApiEndpoint`）の有無を確認し、以下のとおりサービスを選択する。

| 条件 | 使用サービス | 動作 |
|---|---|---|
| エンドポイント **未設定** | `MockGenerate3DAssetService` | 実際の変換を行わず、URL を組み立てて即返却 |
| エンドポイント **設定あり** | `TrellisGenerate3DAssetService` | trellis_api を呼び出し、結果を Blob Storage にアップロードして URL を返却 |

#### 3.2.3 設定項目

| 環境変数 / 設定キー | 説明 | デフォルト |
|---|---|---|
| `IMAGE_TO_3D_API_ENDPOINT` | trellis_api のベース URL | （未設定＝モックモード） |
| `IMAGE_TO_3D_API_KEY` | trellis_api の Bearer トークン | （省略可） |
| `MockAssetStorage__SourceImageBaseUrl` | モック時の元画像ベース URL | `https://mockstorage.local/assets` |
| `MockAssetStorage__ModelBaseUrl` | モック時のモデルベース URL | `https://mockstorage.local/assets` |
| `BlobStorage__ConnectionString` | Azure Blob Storage 接続文字列 | `UseDevelopmentStorage=true` |
| `BlobStorage__ContainerName` | アップロード先コンテナ名 | `assets` |
| `Trellis__BlobBaseUrl` | 元画像 URL 構築用 Blob ベース URL | — |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights 接続文字列（省略可） | — |

#### 3.2.4 バリデーションルール

- `imageUrl` と `imageBlobPath` の両方が空の場合はエラー
- `imageUrl`: 絶対 URL（http または https スキームのみ）
- `imageBlobPath`: 相対パス形式。`/` 始まり、`.`、`..` セグメント、`?`、`#` を含む場合はエラー
- `outputFormat`: `glb` / `obj` のみ許可
- `quality`: `preview` / `standard` / `high` のみ許可

---

### 3.3 trellis_api（Python FastAPI）

| 項目 | 値 |
|---|---|
| 言語 | Python 3.10 以上 |
| フレームワーク | FastAPI + uvicorn |
| ポート（デフォルト）| 8080 |
| 役割 | Step 3: 画像→3D モデル変換 |
| 推論バックエンド | HuggingFace Space `microsoft/TRELLIS.2`（gradio_client 経由） |

#### 3.3.1 エンドポイント

**`POST /generate-3d`**

リクエストボディ（JSON）:

| フィールド | 型 | デフォルト | 説明 |
|---|---|---|---|
| `imageUrl` | string | 必須 | 元画像の http/https URL |
| `outputFormat` | string | `glb` | `glb` のみサポート |
| `quality` | string | `preview` | `preview` / `standard` / `high` |
| `seed` | int | `0` | 乱数シード |
| `meshSimplify` | float | `0.95` | 後方互換パラメータ（内部では decimationTarget に変換） |
| `decimationTarget` | int | `300000` | ポリゴン数上限 |
| `textureSize` | int | `1024` | テクスチャ解像度（px） |

レスポンス: GLB バイナリ（`Content-Type: model/gltf-binary`）

**`GET /health`**

レスポンス:
```json
{ "status": "ok", "space": "microsoft/TRELLIS.2" }
```

#### 3.3.2 推論フロー（HuggingFace Space 呼び出し順序）

1. **`/start_session`** — ZeroGPU セッション初期化
2. **`/preprocess_image`** — 背景除去・前処理（PNG 一時ファイルを渡す）
3. **`/image_to_3d`** — 3D 生成（セッション状態はサーバー側で保持）
   - quality に応じたサンプリングステップ数を使用（下表）
4. **`/extract_glb`** — GLB ファイルの抽出

| quality | サンプリングステップ数 |
|---|---|
| preview | 12 |
| standard | 25 |
| high | 50 |

#### 3.3.3 環境変数

| 変数 | デフォルト | 説明 |
|---|---|---|
| `TRELLIS_HF_SPACE` | `microsoft/TRELLIS.2` | 使用する HuggingFace Space |
| `HF_TOKEN` | （なし） | HuggingFace アクセストークン（省略可） |

---

### 3.4 inspector_agent.py（デバッグ用 Foundry Agent）

| 項目 | 値 |
|---|---|
| 言語 | Python |
| フレームワーク | Microsoft Agent Framework (azure-ai-agentserver) |
| ポート（デフォルト）| 8088 |
| 用途 | trellis_api の動作確認・デバッグ支援 |

#### 3.4.1 提供ツール

| ツール名 | 説明 | 引数 |
|---|---|---|
| `get_service_status` | `/health` エンドポイントを呼び出してサービス疎通を確認 | `endpoint: str`（例: `http://127.0.0.1:8000`） |
| `get_quality_steps` | quality 名に対応するサンプリングステップ数を返す | `quality: str`（`preview` / `standard` / `high`） |

#### 3.4.2 必要な環境変数（`.env` ファイル）

| 変数 | 説明 |
|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Azure AI Foundry プロジェクトエンドポイント URL |
| `FOUNDRY_MODEL_DEPLOYMENT_NAME` | 使用するモデルのデプロイ名 |

---

## 4. データフロー

### 4.1 モックモード（`IMAGE_TO_3D_API_ENDPOINT` 未設定）

```
クライアント
  → POST /api/generate-3d { imageUrl, ... }
  → [バリデーション]
  → MockGenerate3DAssetService: URL組み立て（実変換なし）
  → 200 OK { modelUrl, modelBlobPath, sourceImageUrl }
```

## 4. データフロー

### 4.1 エンドツーエンド（フル実装後）

```
ユーザー
  → Foundry Agent「〇〇のような商品モックを作って」+ 参考画像（任意）
  → Agent: POST /api/generate-image { prompt, referenceImageUrl }
    → Azure OpenAI gpt-image-2 で画像生成
    → Blob Storage images/ に保存
  ← { imageUrl, imageBlobPath }
  → Agent: POST /api/generate-3d { imageUrl }
    → TrellisGenerate3DAssetService
      → POST http://<trellis_api>/generate-3d
        → 画像ダウンロード → PNG 一時保存
        → HuggingFace Space gradio_client
          → /start_session → /preprocess_image → /image_to_3d → /extract_glb
        ← GLB バイナリ
      → Blob Storage models/ に保存
    ← { modelUrl, modelBlobPath, sourceImageUrl }
  ← Agent がユーザーに 3D モデル URL を提示
```

### 4.2 開発用モックモード（`IMAGE_TO_3D_API_ENDPOINT` 未設定）

trellis_api を起動せずに動作確認できる。実際の変換は行わず URL だけ組み立てて返す。

```
クライアント
  → POST /api/generate-3d { imageUrl }
  → MockGenerate3DAssetService: URL組み立てのみ
  ← 200 OK { modelUrl, modelBlobPath, sourceImageUrl }
```

---

## 5. 実装状況

| コンポーネント | 役割 | 実装状況 |
|---|---|---|
| `ImageTo3DMockAgent.Functions` | Step 2: AI 画像生成 | **完成** |
| `ImageTo3DMockAgent.Api` | Step 3: 3D モデル変換 API | **完成** |
| `trellis_api/main.py` | Step 3: HuggingFace Space ラッパー | **完成** |
| `trellis_api/inspector_agent.py` | デバッグ用 Agent | **完成** |
| `agent/main.py`（Foundry Agent）| ユーザー対話・ツール呼び出しオーケストレーション | **完成** |
| テスト（Api.Tests） | バリデーター・モックサービスの単体テスト | **完成** |

---

## 6. 依存ライブラリ

### .NET（`ImageTo3DMockAgent.Api.csproj`）

| パッケージ | バージョン | 用途 |
|---|---|---|
| `Azure.Storage.Blobs` | 12.22.2 | Azure Blob Storage アップロード |
| `Microsoft.Azure.Functions.Worker` | 2.52.0 | Azure Functions ホスト |
| `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` | 2.1.0 | HTTP トリガー |
| `Azure.Monitor.OpenTelemetry.Exporter` | 1.7.0 | Application Insights 連携 |
| `OpenTelemetry.Extensions.Hosting` | 1.15.3 | OpenTelemetry ホスト統合 |

### Python（`trellis_api/requirements.txt`）

| パッケージ | 用途 |
|---|---|
| `fastapi` | Web フレームワーク |
| `uvicorn` | ASGI サーバー |
| `gradio_client` | HuggingFace Space 呼び出し |
| `pillow` | 画像デコード・PNG 変換 |
| `pydantic` | リクエストバリデーション |
| `agent-framework-foundry` | Microsoft Foundry Agent フレームワーク |
| `agent-framework-foundry-hosting` | Foundry Hosted Agent サーバーホスト |
| `agent-framework-core` | Agent フレームワーク共通コア |
| `azure-ai-agentserver-core` | Azure AI Agent サーバーコア |
| `azure-identity` | Azure 認証 |
| `python-dotenv` | `.env` 読み込み |

---

## 7. テスト

`tests/ImageTo3DMockAgent.Api.Tests/` に以下のユニットテストが実装されている。

| テストクラス | テスト内容 |
|---|---|
| `Generate3DRequestValidatorTests` | imageUrl/imageBlobPath 未指定時のエラー検証、不正フィールドのエラー検証 |
| `MockGenerate3DAssetServiceTests` | デフォルト値での URL 組み立て確認、imageBlobPath から元画像 URL を導出する確認 |

---

## 8. ローカル起動手順

### trellis_api

```bash
cd trellis_api
pip install -r requirements.txt
uvicorn trellis_api.main:app --host 127.0.0.1 --port 8080
```

### ImageTo3DMockAgent.Functions（画像生成、ポート 7072）

```bash
cd src/ImageTo3DMockAgent.Functions
func start --port 7072
```

### ImageTo3DMockAgent.Api（モックモード、ポート 7071）

```bash
# local.settings.json の IMAGE_TO_3D_API_ENDPOINT を空文字に設定
cd src/ImageTo3DMockAgent.Api
func start --port 7071
```

> **ヒント**: リポジトリルートの `start-api.bat` を使うと `cd` 不要で Api を起動できます。

### ImageTo3DMockAgent.Api（実変換モード）

```bash
# local.settings.json の IMAGE_TO_3D_API_ENDPOINT=http://localhost:8080 を設定
cd src/ImageTo3DMockAgent.Api
func start --port 7071
```

### inspector_agent.py

```bash
# .env に FOUNDRY_PROJECT_ENDPOINT と FOUNDRY_MODEL_DEPLOYMENT_NAME を設定
cd trellis_api
python inspector_agent.py
```

---

## 9. 制約・注意事項

- trellis_api の `/generate-3d` は **GLB 形式のみ**返却する（HuggingFace Space の制約）。
- HuggingFace Space の ZeroGPU は混雑時にキューイングが発生するため、レスポンスまでに時間がかかる場合がある。`HF_TOKEN` を設定することで優先アクセスが可能になる。
- `TrellisGenerate3DAssetService` は API キーを `Authorization: Bearer` ヘッダーで送信するが、trellis_api 側での認証処理は現在未実装。
- TRELLIS が最も高品質な 3D を生成するには、入力画像の背景が透明または単色であることが推奨される。画像生成プロンプトを設計する際はこの点を考慮すること。
