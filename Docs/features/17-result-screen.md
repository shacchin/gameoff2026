---
status: 完了
started: 2026-11-23
completed: 2026-11-23
---

# 結果画面の演出

## 目的

ゲームオーバーのあと、プレイの成果を 1 項目ずつ見せてから最後にスコアを出し、結果を見る楽しみを作る。

## 仕様

- 結果画面では、次の順に 1 行ずつ出す。各行が出るたびに SE（`金属タイトル表示2`）を鳴らす
  1. `GAME OVER`（結果画面を開いたときに出ている）
  2. `PLAY TIME 03:42`（ゲーム開始からゲームオーバーまでの時間。`mm:ss`）
  3. `ACCIDENTS 12`（事故を起こした回数）
  4. `POLICE DESTROYED 2`（事故で破壊した警察車両の数）
  5. `SCORE 1200`（スコア専用の SE `金属タイトル表示1` を鳴らす。アニメーションはつけず、数え上げもしない）
  6. RETRY / TITLE のボタン（ここから入力を受け付ける）
- 間隔はインスペクターで調整できる（最初の行までの待ち、行と行の間、スコアまでの待ち、ボタンまでの待ち）
  - スコアの前だけ間を空けることはしない（初期値は行と行の間と同じ）
- 演出の途中でクリック（または何かのキー）をしたら、スコア用の SE を鳴らして残りをすべて出す
  - スコアがもう出ていれば SE は鳴らさず、ボタンだけ出す

## 決定事項

- 「破壊した車の台数」は出さず、「破壊した警察車両の数」を出す（11/23）
  - 理由: 1 回の事故で必ず 2 台消えるので、破壊した台数は事故の回数の 2 倍になり、情報が増えない
- 数を数えるのは `GameManager`、見せ方（順番・間隔・SE・飛ばす処理）は `GameUI` が受け持つ（11/23）
  - 演出が終わってボタンを出したら、`GameUI` が `GameManager.EnableChoice` を呼んでリトライ／タイトルへの入力を受け付け始める
- 警察車両を破壊したことは `Car.PoliceDestroyed` で知らせる（`Car.PoliceEscaped` と同じ形）
- 各行もスコアも、今はアニメーションをつけずに表示するだけにする（11/23）
- 演出は `Time.timeScale = 0` の間に動くので、`WaitForSecondsRealtime` で待つ（あとでアニメーションを足すなら DOTween は `SetUpdate(true)` にする）

## 関連ファイル

- `Assets/_Project/Scripts/GameUI.cs`
- `Assets/_Project/Scripts/GameManager.cs`
- `Assets/_Project/Scripts/Car.cs`
- `Assets/_Project/Scripts/AudioManager.cs`

## 未決・TODO

- [ ] 項目を増やすか（最大同時ポータル数、最長の連鎖など）
- [ ] 各行・スコアが出るときのアニメーションを足すか

## 実装メモ

- 変更: `GameManager` がプレイ時間・事故の回数・破壊した警察車両の数を数え、`GameUI` がコルーチンで 1 行ずつ出す。SE は `AudioManager.Se.ResultItem` / `ResultScore`
- 設計判断: リトライ／タイトルへの入力は、演出が終わって `GameUI` が `GameManager.EnableChoice` を呼んでから `choiceInputDelay` 後に受け付ける（以前は結果を出した直後から数えていた）
- 設計判断: 警察車両の破壊は `Car.Crash` の中で `Car.PoliceDestroyed` を出して知らせる（`AccidentHappened` は位置しか渡さないため）
- 注意点: 項目の並びに Vertical Layout Group を使わない。行を順に表示すると、まだ出ていない行が詰められて位置がずれる
- 注意点: `Choices Root`（ボタンの親）が未設定だと、ボタンは最初から見えたまま（演出が終わるまで押しても反応しない）
- 注意点: 演出を飛ばす入力は、クリックに加えて何かのキーでも受け付ける（タイトル画面と同じ）
- 動作確認: エディタで結果画面の順番表示・SE を確認し、ユーザーが OK を出した（11/23）。WebGL ビルドは未確認
