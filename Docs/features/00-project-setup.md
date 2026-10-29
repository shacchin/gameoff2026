---
status: 完了
started: 2026-10-29
completed: 2026-10-29
---

# プロジェクトの初期設定

## 目的

テーマ発表（11/1）の前に、Web ビルドから itch.io 公開までの流れと、ピクセルアートの表示環境を整えておく。

## 仕様

- Web (WebGL) でビルドして、itch.io のブラウザ上で起動できる
- 自作アセットを外部アセットと分けて管理できる
- 16px のドット絵が、ぼやけずに整数倍で表示される
- Aseprite / PNG を置くだけで、ピクセルアート向けの設定で取り込まれる

## 決定事項

### Web ビルド

- ビルドターゲットは Web
- Compression Format: **Gzip** + Decompression Fallback ON。Brotli は itch.io で読み込みエラーになりやすいため
- キャンバスサイズ: **960×540**（16:9。内部解像度 320×180 のちょうど 3 倍）
- Company Name: `Shacchin`

### エディタ

- Enter Play Mode Settings はデフォルト（Domain / Scene をリロードする）のまま
  - 理由: Unity に不慣れなうちは、static 変数が前回のプレイから残ることによる「エディタでだけ起きるバグ」を避けたい
  - Play 開始の待ち時間が気になったら、途中で変更しても影響は小さい。その場合は static のリセット処理（`[RuntimeInitializeOnLoadMethod]`）が必要になる

### フォルダ構成

- 自作アセットは `Assets/_Project/{Scripts, Scenes, Prefabs, Art, Audio}` にまとめる
  - 理由: Asset Store などの外部アセットと混ざらないようにするため。`_` を付けると Project ウィンドウの一番上に並ぶ
- ドキュメントは `Assets/` の外の `Docs/` に置く（`Assets/` の中だと Unity がアセットとして読み込むため）

### ピクセルアート

- 1 マス = **16px**（Pixels Per Unit 16）、内部解像度 **320×180**（画面に横 20 マス × 縦 11.25 マス）
- Main Camera に Pixel Perfect Camera を追加
  - Assets PPU 16 / Reference Resolution 320×180 / Crop Frame None / Grid Snapping **Upscale Render Texture**
  - Upscale Render Texture を選んだ理由: 320×180 で描画してから拡大するので、回転や拡大縮小をしてもドットが崩れない
- アンチエイリアス（MSAA）は URP の設定で最初からオフ
- PNG: `Assets/_Project/Art/` に入れると、エディタ拡張が自動で Sprite / PPU 16 / Point / 圧縮なし / ミップマップなし にする
  - 最初のインポート時だけ適用する。あとから Inspector で変えた設定は上書きしない
- Aseprite: `.aseprite` は専用の Importer で読み込まれるため、上のエディタ拡張は効かない。代わりに Preset をデフォルトに登録した
  - デフォルトから変える必要があったのは Pixels Per Unit（100 → 16）だけ
  - 取り込み手順の詳細は Notion「Aseprite → Unity 取り込みメモ」

### itch.io

- ページ: https://shacchin.itch.io/shacchin-gameoff-proto（Draft）。提出時にタイトルや説明を書き換えて、ビルドを差し替える
- アップロード設定: Kind = HTML / 「This file will be played in the browser」にチェック / Viewport 960×540 / Fullscreen button ON
- zip にはビルドフォルダの**中身**（`index.html` など）を入れる。フォルダごと zip にすると起動しない
- 16×16 のサンプル画像を置いたビルドで、ブラウザでの起動を確認済み

## 関連ファイル

- `ProjectSettings/ProjectSettings.asset`（Player 設定）
- `ProjectSettings/EditorBuildSettings.asset`（Scene List）
- `ProjectSettings/PresetManager.asset`（Aseprite の Preset 登録）
- `Assets/_Project/Scenes/SampleScene.unity`（Main Camera の Pixel Perfect Camera）
- `Assets/_Project/Scripts/Editor/PixelArtTextureImporter.cs`
- `Assets/_Project/Art/AsepritePixelImporter.preset`
- `Assets/_Project/Art/sample.aseprite`（表示確認用）

## 未決・TODO

- [ ] テーマが決まったら `sample.aseprite` とシーン上のサンプルを削除する
- [ ] Product Name をゲーム名に変える（ゲーム名決定後）
- [ ] Aseprite の取り込みで Compression のデフォルト値を確認する（色がにじんだら）
