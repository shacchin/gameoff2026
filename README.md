# GameOff2026

[Game Off 2026](https://itch.io/jam/game-off-2026)（テーマ: SHIFT）の参加作品。次元ポータルで交通事故を防ぐトップビューの避けゲー。

## 環境

- Unity 6000.3.17f1（URP 2D）
- ビルドターゲット: Web (WebGL)

## セットアップ

clone したあと、Unity で開く前後に次の作業が必要。

### DOTween

DOTween は Asset Store のライセンスの都合でリポジトリに含めていない（`.gitignore` で `Assets/Plugins/Demigiant/` を除外）。clone 直後は DOTween を使っているスクリプトがコンパイルエラーになるので、次の手順で入れる。

- 使用バージョン: **DOTween (HOTween v2) 無料版 v1.3.045**
- 入手先: Unity Asset Store「DOTween (HOTween v2)」（Demigiant）

1. Window > Package Manager > My Assets で DOTween を Download → Import（全ファイル）
2. Tools > Demigiant > DOTween Utility Panel > **Setup DOTween...** を押す
3. モジュールは Audio / Physics2D / Sprite / UI / UI Toolkit を ON、Physics を OFF にして Apply（TextMesh Pro は Pro 版のみ）
4. Create ASMDEF は押さない

設定ファイル `Assets/Resources/DOTweenSettings.asset` はリポジトリに含めているので、Preferences の設定はやり直さなくてよい。

### 外部の絵素材

次の素材はライセンスの都合でリポジトリに含めていない（`.gitignore` で除外）。clone 直後は、これらを使っている Prefab やタイルの絵が抜けて表示される。

- `Assets/_Project/Art/TopDownVehicles/`（車）
- `Assets/_Project/Art/TopDownCity/`（街）

インポート設定（`.meta`）も含まれないので、入れ直したら次の設定をやり直す。

**TopDownVehicles（今はブラウンセダンだけ使用）**

1. `Brown_SEDAN_CLEAN_All_000-sheet.png` を次の設定にして Apply
   - Texture Type: `Sprite (2D and UI)` / Sprite Mode: `Multiple`
   - Pixels Per Unit: `100` / Filter Mode: `Point (no filter)` / Compression: `None`
2. Sprite Editor で `Slice` → Type `Grid By Cell Count`、`7` × `7`、Pivot `Center` → Slice → Apply。49 個目の空のコマができたら消す
3. `Prefabs/Car.prefab` の `Visual` の Sprite Renderer にコマを 1 つ入れ、`DirectionalSprite` の右クリックメニュー「シートからコマを読み込む」を実行する

### BGM・効果音

BGM と効果音はライセンスの都合でリポジトリに含めていない（`.gitignore` で除外）。clone 直後は音が鳴らない（参照が外れる）。

- `Assets/_Project/Audio/BGM/`（BGM）
- `Assets/_Project/Audio/SE/`（効果音）

インポート設定（`.meta`）も含まれないので、入れ直したら設定をやり直す。

ポータルから車が戻るときの音は、飲み込みの音を逆再生した WAV を使う。これも含まれないので、飲み込みの音を Project ウィンドウで右クリック →「逆再生の WAV を作る」で作り直す（`Docs/features/16-audio.md`）。

## ドキュメント

- 機能ごとの仕様: `Docs/features/`
- 書き方: `Docs/README.md`
