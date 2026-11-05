# Docs

機能ごとの仕様と、決めたことの理由をまとめる。タスク・スケジュール・日々のメモは Notion「Game Off 2026」ページで管理し、ここには書かない。

## 書き方のルール

- 1 機能 = 1 ファイル。`features/NN-機能名.md`（NN は作った順の連番）
- `_template.md` をコピーして作る
- 実装を始める前に「目的」と「仕様」を書く。実装しながら「決定事項」と「関連ファイル」を埋める
- 先頭の `status` / `started` / `completed` を必ず更新する
  - `status`: `未着手` / `進行中` / `完了`
  - `started`: 着手日（`YYYY-MM-DD`）。未着手なら空
  - `completed`: 完了日（`YYYY-MM-DD`）。完了していなければ空
- 仕様を変えたら、コードと同じコミットでドキュメントも更新する
- ジャムなので書きすぎない。1 ファイル数十行が目安

## 共通ルール

機能をまたいで守る決まりごと。

| ファイル | 内容 |
|---|---|
| [sorting-order.md](sorting-order.md) | 描画順（Order in Layer）の帯域と、今の割り当て |

## 機能一覧

| ファイル | 内容 |
|---|---|
| [00-project-setup.md](features/00-project-setup.md) | プロジェクトの初期設定（WebGL・フォルダ構成・ピクセルアート） |
| [01-road-and-routes.md](features/01-road-and-routes.md) | 道路と経路（ウェイポイントで車のルートを決める） |
| [02-car-spawn-and-move.md](features/02-car-spawn-and-move.md) | 車の出現と移動（時間経過で車が増える） |
| [03-collision-and-warning.md](features/03-collision-and-warning.md) | 衝突判定と事故予告 |
| [04-portal.md](features/04-portal.md) | ポータル（車を別次元へ飛ばし、数秒後に同じ位置へ戻す） |
| [05-game-flow.md](features/05-game-flow.md) | ゲーム進行（60 秒タイマー・クリア／ゲームオーバー・リトライ） |
| [06-refactor.md](features/06-refactor.md) | 要リファクタ（05 までの見直しで出た、コードと Editor の整理） |
