---
status: 完了
started: 2026-11-22
completed: 2026-11-22
---

# ポータルを置く位置のカーソル

## 目的

マウスの位置と、クリックしたらどこにポータルが出るかが画面に出ていないので、置く前に分かるようにする。

## 仕様

- マウスのあるマスの中心に、カーソル（`Art/cursor.aseprite`、32×32 = 1 マス）を出す
- クリックすると、カーソルと同じ位置（マスの中心）にポータルを置く
- ポータルを置ける数を使い切っているときは、カーソルを薄くする（`cursorFullAlpha`、初期値 0.3）
- ゲームが終わったら（置けなくなったら）カーソルを消す
- OS のマウスカーソルはそのまま出しておく

## 決定事項

- 置く位置の吸着を「車線の中心から 0.5 マス以内なら車線の中心へ」から「いつもマスの中心へ」に変えた（11/22）
  - 理由: カーソルの位置と置かれる位置を必ず一致させるため。車線の中心はどれもマスの中心（x.5）にあるので、道路の上なら車線の中心に置かれ、吸い込み損ねない
  - これで `PortalPlacer` は経路（`CarSpawner`）を使わなくなり、代わりにシーンの `Grid` を参照する
- カーソルは `PortalPlacer` が Prefab から作って動かす（置く位置を決めるのと同じ場所で、ずれないように）
- 描画順は新しい帯域「カーソル（300〜）」にする。車やマークより手前に出す

## 関連ファイル

- `Assets/_Project/Scripts/PortalPlacer.cs`
- `Assets/_Project/Prefabs/PortalCursor.prefab`
- `Assets/_Project/Art/cursor.aseprite`

## 未決・TODO

- [ ] OS のマウスカーソルを隠すか決める（隠すと結果画面のボタン操作が分かりにくくなる）

## 実装メモ

- 変更: `PortalPlacer` がカーソルの Prefab を `Start` で自分の子として作り、`Update` で毎フレーム `SnapToGrid` の位置へ動かす。クリックで置く位置にも同じ値を使う。車線への吸着（`SnapToLane`、`carSpawner`、`snapDistance`）は削除した
- 設計判断: マスの中心は `Grid.GetCellCenterWorld(WorldToCell)` で求める。Grid の原点やマスの大きさが変わっても追従させるため
- 設計判断: カーソルの表示・非表示は `PortalPlacer` の `OnEnable` / `OnDisable` に合わせる。ゲーム終了で `enabled = false` になると一緒に消える
- 注意点: 置けない状態の薄さは、スクリプトが `Visual` の `SpriteRenderer.color` の alpha を書き換えて出している。色を DOTween で動かす演出を足すときはぶつかる
- 注意点: `cursor.aseprite` は余白を切り抜いて 28×28px（0.875 マス）で読み込まれる。ピボットはキャンバスの中心なので位置はずれない。大きさを変えるときは `Visual` の Scale ではなく絵を描き直す（Pixel Perfect Camera でドットが崩れるため）
- 動作確認: カーソルがマスの中心に吸着して表示されることをユーザーが確認した（11/22）。置けない数のときの薄さ、マスの中心に置いたポータルが縦・横の車線の車を吸い込むこと、WebGL ビルドは未確認
