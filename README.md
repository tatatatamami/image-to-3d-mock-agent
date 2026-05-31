# image-to-3d-mock-agent

Microsoft Foundry Agentから画像生成とImage-to-3D変換を実行し、3Dモックを生成するデモコードです。

## 実装済み API

### `POST /generate-image`

Azure OpenAI `gpt-image-2` デプロイメントでコンセプト画像を生成し、PNG にデコードして Azure Blob Storage の `images/` 配下へ保存します。

#### リクエスト例

```json
{
  "prompt": "A small futuristic robot mascot, full body, white background",
  "size": "1024x1024",
  "quality": "high",
  "n": 1
}
```

#### レスポンス例

```json
{
  "imageUrl": "https://<storage-account>.blob.core.windows.net/<container>/images/<file-name>.png",
  "imageBlobPath": "images/<file-name>.png",
  "prompt": "A small futuristic robot mascot, full body, white background",
  "size": "1024x1024",
  "quality": "high",
  "status": "succeeded",
  "createdAt": "2026-05-31T00:00:00Z"
}
```

## 必要な環境変数

アプリケーションコードでは以下を使用します。

- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_API_KEY`
- `AZURE_OPENAI_IMAGE_DEPLOYMENT`
- `AZURE_STORAGE_CONNECTION_STRING`
- `AZURE_STORAGE_CONTAINER_NAME`

Azure Functions をローカル実行するため、あわせて以下も設定してください。

- `AzureWebJobsStorage`
- `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`

## ローカル実行手順

1. .NET 8 SDK と Azure Functions Core Tools をインストールします。
2. `/tmp/workspace/tatatatamami/image-to-3d-mock-agent/src/ImageTo3DMockAgent.Functions/local.settings.json` を作成または編集し、必要な環境変数を設定します。
3. 依存関係を復元・ビルドします。

   ```bash
   cd /tmp/workspace/tatatatamami/image-to-3d-mock-agent
   dotnet build image-to-3d-mock-agent.slnx
   ```

4. Functions ホストを起動します。

   ```bash
   cd /tmp/workspace/tatatatamami/image-to-3d-mock-agent/src/ImageTo3DMockAgent.Functions
   func start
   ```

5. 別ターミナルから API を呼び出します。

   ```bash
   curl -X POST http://localhost:7071/generate-image \
     -H "Content-Type: application/json" \
     -d '{
       "prompt": "A small futuristic robot mascot, full body, white background",
       "size": "1024x1024",
       "quality": "high",
       "n": 1
     }'
   ```

## OpenAPI

`/tmp/workspace/tatatatamami/image-to-3d-mock-agent/openapi.yaml` に `/generate-image` を OpenAPI 3.0 形式で定義しています。`operationId` は `generate_image` です。
