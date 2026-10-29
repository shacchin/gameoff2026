# GameOff2026

Game Off 2026（itch.io のゲームジャム）に参加するためのソロ開発プロジェクト。

- 期間: 2026-11-01〜2026-11-30（11/29 提出目標、11/30 は予備日）
- 公式: https://itch.io/jam/game-off-2026
- テーマ: （11/1 発表後に記入）
- 企画: （確定後に記入。コアループ・必須機能・削れる機能）
- タスク管理: Notion「Game Off 2026」ページ
- itch.io ページ: https://shacchin.itch.io/shacchin-gameoff-proto（Draft。提出時にタイトル等を書き換える）

応答・コメントは日本語で書く。

## 環境

- Unity 6000.3.17f1 / URP 2D（Renderer2D）
- Input System（`Assets/InputSystem_Actions.inputactions`）
- 2D パッケージ一式（Tilemap, Sprite, Animation, Aseprite / PSD Importer）
- **ビルドターゲット: Web (WebGL)**、itch.io にブラウザゲームとして公開

## WebGL の制約（実装時に必ず守る）

- マルチスレッド（`System.Threading`、`Task.Run` での並列処理）は使わない。非同期はコルーチンか `Awaitable` を使う
- `System.IO` でのファイル読み書きはしない。セーブは `PlayerPrefs` を使う
- 音声はユーザー操作（クリック／キー入力）の後でないと鳴らない。タイトル画面で入力を待ってから BGM を再生する
- ネイティブプラグインは使わない
- Publishing Settings の Compression Format は Gzip + Decompression Fallback（または Disabled）。Brotli にしない

## 開発方針

- ジャム作品なので、作り込みより「最後まで遊べる状態を早く作る」ことを優先する
- 過度な抽象化や汎用化はしない。シンプルで読みやすいコードにする
- 調整したい数値は `[SerializeField]` でインスペクターに出す

## フォルダ構成

自作アセットは `Assets/_Project/` 以下にまとめる。

```
Assets/_Project/
  Scripts/
  Scenes/
  Prefabs/
  Art/
  Audio/
```

## ドキュメント（Docs/）

機能ごとの仕様と決定事項は `Docs/features/` に 1 機能 1 ファイルでまとめる（書き方は `Docs/README.md`）。

- 機能の実装に入る前に、該当する `Docs/features/*.md` を読む。なければ `Docs/_template.md` から作り、「目的」と「仕様」をユーザーと確認してから実装する
- 着手したら `status: 進行中` と `started`、完了したら `status: 完了` と `completed` を更新する
- 仕様や決定事項が変わったら、コードと同じコミットでドキュメントも更新する
- タスク・スケジュール・日々のメモは Notion で管理し、Docs には書かない

## Claude への注意

- Claude は Unity エディタを操作できない。`.unity` / `.prefab` / `.asset` の YAML を直接編集するのは避け、インスペクターでの設定やシーン配置が必要な場合は手順をユーザーに伝える
- 新しいスクリプトを作るときに `.meta` ファイルは作らない（Unity が生成する）
- ファイルを移動・リネームするときは `.meta` も一緒に移動する（GUID が変わると参照が切れる）
