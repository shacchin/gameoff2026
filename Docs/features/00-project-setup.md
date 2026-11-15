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
- 1 マス 32px のドット絵が、ぼやけずに整数倍で表示される（16px の仮アートも 2 倍で並べられる）
- Aseprite / PNG を置くだけで、ピクセルアート向けの設定で取り込まれる

## 決定事項

### Web ビルド

- ビルドターゲットは Web
- Compression Format: **Gzip** + Decompression Fallback ON。Brotli は itch.io で読み込みエラーになりやすいため
- キャンバスサイズ: **960×540**（16:9。内部解像度と同じで等倍表示）
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

- 1 マス = **32px**（Pixels Per Unit 32）、内部解像度 **960×540**（画面に横 30 マス × 縦 16.875 マス）
  - 最初は 320×180（3 倍表示）にしていたが、企画確定後（11/2）に 480×270（2 倍表示）へ変更
    - 理由: 交差点を先読みして判断するゲームなので、見える範囲を広げる必要があった（詳細は `01-road-and-routes.md`）
  - 11/15 に 1 マス 16px → 32px へ変更。見える範囲（マス数）は変えずに、PPU と内部解像度をどちらも 2 倍にした
    - 理由: アートスタイルの検討で、描き込みを細かくできる余地を残したかった
    - ゲームのロジックはワールド単位（1 = 1 マス）で書いているので、コード・経路・Tilemap は変更なし
- 16px の仮アート（`*_proto.aseprite`）は PPU 16 のまま使い続ける
  - PPU 16 の絵は 1 ドットが 2×2 px で描かれる。整数倍なのでぼやけない
  - 内部解像度が上がったため、16px 時代より移動が半ドット刻みになり、回転した絵のギザギザも減る（仮の段階では許容）
  - 32px で描き直すときは、その画像の Pixels Per Unit を 32 に変える。同じファイルを上書きした場合、取り込み設定は PPU 16 のまま残る
  - 16px と 32px の絵を同じ画面に混ぜると描き込みの細かさが揃わないので、描き直すときはまとめて切り替える
- Main Camera に Pixel Perfect Camera を追加（Game・Title の両シーン）
  - Assets PPU 32 / Reference Resolution 960×540 / Crop Frame None / Grid Snapping **Upscale Render Texture**
  - Upscale Render Texture を選んだ理由: 内部解像度で描画してから拡大するので、回転や拡大縮小をしてもドットが崩れない
- アンチエイリアス（MSAA）は URP の設定で最初からオフ
- PNG: `Assets/_Project/Art/` に入れると、エディタ拡張が自動で Sprite / PPU 32 / Point / 圧縮なし / ミップマップなし にする
  - 最初のインポート時だけ適用する。あとから Inspector で変えた設定は上書きしない
- Aseprite: `.aseprite` は専用の Importer で読み込まれるため、上のエディタ拡張は効かない。代わりに Preset をデフォルトに登録した
  - デフォルトから変える必要があったのは Pixels Per Unit（100 → 32）だけ
  - Preset も最初の取り込み時だけ適用される。既存の仮アートは PPU 16 のまま
  - 取り込み手順の詳細は Notion「Aseprite → Unity 取り込みメモ」

### DOTween

- 演出・UI アニメーション用に **DOTween (HOTween v2) 無料版 v1.3.045** を Asset Store から導入（11/3）
- `Assets/Plugins/Demigiant/` は `.gitignore` で除外し、リポジトリに含めない
  - 理由: Asset Store 版を公開リポジトリに置くと再配布にあたるため。公式サイト版（改変しなければ再配布可）をコミットする案もあったが、Asset Store 版を除外する方を選んだ
  - clone したら Asset Store から入れ直す必要がある。手順と使用バージョンは `README.md` に書く。更新したら README のバージョンも直す
- 設定ファイル `Assets/Resources/DOTweenSettings.asset` はコミットする（DOTween 本体ではなく、このプロジェクト用の設定のため）
- モジュール: Audio / Physics2D / Sprite / UI / UI Toolkit を ON、Physics を OFF
  - TextMesh Pro のモジュールは Pro 版のみなので使えない
  - UI を uGUI と UI Toolkit のどちらで作るかは未定なので、両方 ON にしておく
- Create ASMDEF は使わない（プロジェクトで asmdef を使っていないため）
- Safe Mode: ON（デフォルト）。破棄済みのオブジェクトを tween が触ってもエラーで止まらない

### itch.io

- ページ: https://shacchin.itch.io/shacchin-gameoff-proto（Draft）。提出時にタイトルや説明を書き換えて、ビルドを差し替える
- アップロード設定: Kind = HTML / 「This file will be played in the browser」にチェック / Viewport 960×540 / Fullscreen button ON
- zip にはビルドフォルダの**中身**（`index.html` など）を入れる。フォルダごと zip にすると起動しない
- 16×16 のサンプル画像を置いたビルドで、ブラウザでの起動を確認済み

## 関連ファイル

- `ProjectSettings/ProjectSettings.asset`（Player 設定）
- `ProjectSettings/EditorBuildSettings.asset`（Scene List）
- `ProjectSettings/PresetManager.asset`（Aseprite の Preset 登録）
- `Assets/_Project/Scenes/Game.unity` / `Title.unity`（Main Camera の Pixel Perfect Camera）
- `Assets/_Project/Scripts/Editor/PixelArtTextureImporter.cs`
- `Assets/_Project/Art/AsepritePixelImporter.preset`
- `Assets/Resources/DOTweenSettings.asset`
- `.gitignore`（DOTween の除外）
- `README.md`（DOTween の導入手順）

## 未決・TODO

- [x] Pixel Perfect Camera の Reference Resolution を 320×180 → 480×270 に変更する（11/2）
- [x] 1 マス 16px → 32px に変更する（11/15。仮アートは PPU 16 のまま）
- [x] テーマが決まったら `sample.aseprite` とシーン上のサンプルを削除する（11/2）
- [ ] Product Name をゲーム名に変える（ゲーム名決定後）
- [ ] Aseprite の取り込みで Compression のデフォルト値を確認する（色がにじんだら）
