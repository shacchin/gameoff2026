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

## ドキュメント

- 機能ごとの仕様: `Docs/features/`
- 書き方: `Docs/README.md`
