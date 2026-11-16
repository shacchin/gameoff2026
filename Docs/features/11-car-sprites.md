---
status: 進行中
started: 2026-11-16
completed:
---

# 車の見た目を本素材にする

## 目的

仮の四角い車を、TopDownVehicles の本素材に差し替える。どの向きに走っていても自然に見えるようにする。

## 仕様

- 素材は `Assets/_Project/Art/TopDownVehicles/` の絵を使う（TopDownCity の車は使わない）
  - どれも斜め上から見た絵（3/4 ビュー）で、1 枚に向き違いの絵が 48 コマ（7.5° ずつ）並んでいる
  - 1 コマの大きさ: 乗用車（CIVIC・SEDAN・HatchBack）は 100px、トラック（MEDIUMTRUCK）は 140px
  - 並び順: 右向きから始まり、時計回り（右 → 手前 → 左 → 奥）
- まずはブラウンセダン（`Brown_SEDAN_CLEAN_All_000-sheet.png`）だけを使い、全部の車をこの見た目にする
- 絵は回さず、進行方向にいちばん近い向きのコマに差し替える
  - ルート（`Car`）は今までどおり進行方向に回す。当たり判定はルートの向きで決まるので変えない
  - `Visual` だけ回転を打ち消し、向きに合ったコマを表示する（`DirectionalSprite`）
- 戻りのゴースト（10）は、経路を走りながら向きに合ったコマに差し替える
- 吸い込み演出（08）は、飛ばした瞬間のコマを写して、そこから回しながら縮める

## 決定事項

- TopDownCity の車は使わない（11/16）
  - 理由: 真横から見た絵で左右の向きしかなく、縦の道路を走らせられない
- 絵を回さず、コマを差し替える（11/16）
  - 理由: 斜めから見た絵なので、回すと屋根や影の向きまで回って不自然になる
- 導入はブラウンセダン 1 種だけにする（11/16）
  - 理由: まず見た目の差し替えの仕組みを確かめる。ほかの色・トラックは「車の種類」として後で足す
- Pixels Per Unit は 100（1 コマ 100px = 1 マス）（11/16）
  - 理由: 素材が 32×32 表示を想定している。車体は横向きで約 0.83×0.40 マスになり、当たり判定（0.75×0.5）とほぼ合う
- コマの割り当ては、`DirectionalSprite` の右クリックメニュー「シートからコマを読み込む」で行う
  - 理由: 48 コマを手で配列に並べるのは手間で、順番も間違えやすい

## 関連ファイル

- `Assets/_Project/Art/TopDownVehicles/Brown_SEDAN_CLEAN_All_000-sheet.png`
- `Assets/_Project/Prefabs/Car.prefab`
- `Assets/_Project/Scripts/DirectionalSprite.cs`（向きでコマを差し替える）
- `Assets/_Project/Scripts/Car.cs`（`CreateVisualCopy` で DirectionalSprite も写す）
- `Assets/_Project/Scripts/ReturnMark.cs`（ゴーストの向きを追わせる）

## 未決・TODO

- [ ] 当たり判定の `size` を絵に合わせるか（今は 0.75×0.5。絵は約 0.83×0.40）
- [ ] 交差点で重なったとき、手前（画面の下）の車を前に描くか（案: Transparency Sort Axis を Y にする）
- [ ] ほかの色の乗用車・トラックを足す（車の種類）
