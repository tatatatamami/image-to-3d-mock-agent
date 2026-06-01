# image-to-3d-mock-agent
Microsoft Foundry Agentから画像生成とImage-to-3D変換を実行し、3Dモックを生成するデモコード

## API

### POST `/generate-3d`

```json
{
  "imageUrl": "https://<storage-account>.blob.core.windows.net/<container>/images/sample.png",
  "imageBlobPath": "images/sample.png",
  "outputFormat": "glb",
  "quality": "preview"
}
```

- `imageUrl` または `imageBlobPath` のどちらかが必須です
- `outputFormat` は `glb` / `obj` に対応し、既定値は `glb` です
- `quality` は `preview` / `standard` / `high` に対応し、既定値は `preview` です

レスポンス:

```json
{
  "modelUrl": "https://<storage-account>.blob.core.windows.net/<container>/models/sample.glb",
  "modelBlobPath": "models/sample.glb",
  "sourceImageUrl": "https://<storage-account>.blob.core.windows.net/<container>/images/sample.png"
}
```

必要に応じて `MockAssetStorage__SourceImageBaseUrl` と `MockAssetStorage__ModelBaseUrl` 環境変数で画像/モデルのベース URL を上書きできます。
