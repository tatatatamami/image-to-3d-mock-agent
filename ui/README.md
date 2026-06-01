# 3D Mock Studio — UI モック

AI Creative Studio のフロントエンド UI モック。  
自然言語プロンプトから画像生成・3D モデル生成までのワークフローを体験できる静的プロトタイプです。

## 技術スタック

- [React 19](https://react.dev/) + [TypeScript](https://www.typescriptlang.org/)
- [Vite](https://vitejs.dev/)
- [Tailwind CSS v4](https://tailwindcss.com/)

## セットアップ

```bash
# 依存関係のインストール
npm install

# 開発サーバー起動 (http://localhost:5173)
npm run dev

# 本番ビルド
npm run build
```

## 画面構成

3 カラムレイアウト + 固定ヘッダー:

| カラム | 内容 |
|--------|------|
| 左 | プロンプト入力 / 参考画像アップロード / 生成ボタン / クイック提案 |
| 中央 | 画像プレビュー / 3D プレビュー (タブ切り替え) / GLB ダウンロード |
| 右 | ワークフロー Stepper / 生成設定 / 最近の履歴 |

## 注意事項

このモックは **API 未接続の静的プロトタイプ** です。  
実際の画像生成・3D モデル生成 API との接続は別 Issue で実装予定。
