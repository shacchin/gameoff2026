---
status: 完了
started: 2026-11-04
completed: 2026-11-05
---

# ゲーム進行

## 目的

1 プレイを始めて終わらせる。60 秒のあいだに事故を起こしてスコアを稼ぎ、時間切れで結果を見せて、すぐ再挑戦できるようにする。

> 11/16 にルールを「事故を防ぐ（事故 1 回でゲームオーバー）」から「事故を起こしてスコアを稼ぐ」に変えた（12）。下の仕様と決定事項は転換後の内容。転換前の内容は git の履歴と下の実装メモを参照。

## 仕様

- シーンを開くとすぐにゲームが始まり、残り時間（60 秒、`[SerializeField]`）と今のスコアを画面に表示する
- 事故が起きてもゲームは止めない。事故 1 件ごとにスコアを加える（くわしくは 12）
- 残り時間が 0 になったら終わり。車の動きを止めてから結果を表示する
- 結果画面に「TIME UP!」と最終スコアを出し、RETRY か TITLE を選ぶ（ボタンでもキーでも。09）

## 決定事項

- ゲームの状態（プレイ中／終了）は 1 つのクラス（GameManager）が持つ（11/16 にクリア／ゲームオーバーを「終了」1 つにまとめた）
  - 事故は GameManager に事故の位置を渡して知らせる。GameManager はスコアを加え、事故マークと「+100」を出す
  - 終わったことは、イベントで他のクラス（車の出現、ポータル配置、事故の判定、UI）に知らせる
- 終わったら `Time.timeScale = 0` で全体を止める（11/4）
  - 理由: 車・ポータル・復帰マークがどれも `Time.deltaTime` で動いているので、1 か所で全部止まる
  - 止めたあとの演出と待ち時間は、timeScale を無視する設定（`WaitForSecondsRealtime`、DOTween の `SetUpdate(true)`）で動かす
- イベントは 2 段にする（11/4）
  - `GameEnded`: 終わった瞬間。車の出現・ポータル配置・事故の判定が止まる
  - `ResultShown`: 少し待ってから（0.5 秒）。UI が結果画面を出す
- 結果を出してから 0.3 秒は選択の入力を受け付けない（慌てたクリックで結果を見ずに進まないように）
- 画面の文字は英語にする（TextMeshPro の標準フォントに日本語が入っていないため。日本語にするならフォントの用意が必要）
- タイトル画面はプロトタイプでは作らない（α版で追加。WebGL では音を鳴らす前にクリックが必要なので、タイトル画面でその入力を受ける）→ 09 で追加。結果画面の選択（リトライ／タイトルへ）も 09 で変えた

## 関連ファイル

- `Assets/_Project/Scripts/GameManager.cs`
- `Assets/_Project/Scripts/GameUI.cs`
- `Assets/_Project/Prefabs/AccidentMark.prefab`・`ScorePopup.prefab`
- `Assets/_Project/Scripts/AccidentPredictor.cs`・`CarSpawner.cs`・`PortalPlacer.cs`・`WaveDirector.cs`（`GameEnded` で止まる）

## 未決・TODO

- [ ] 背景でヒーローとヴィランが戦う演出（作り込みフェーズで検討）

## 実装メモ

- 変更: `GameManager`（Systems）が状態・残り時間・リトライを持ち、`GameUI`（Canvas）は `RemainingTime` を読んで表示し `ResultShown` で結果画面を出すだけ。事故の地点には `AccidentMark` を出す
- 設計判断: 事故は `AccidentPredictor.AccidentHappened` を GameManager が購読して受け取る（03 のイベントをそのまま使い、GameManager への参照を持たせない）
- 設計判断: 止めるのは timeScale に任せつつ、`CarSpawner`・`PortalPlacer`・`AccidentPredictor` は `GameEnded` で `enabled = false` にして「終わった」ことを明示する
- 注意点: `Time.timeScale` はシーンを読み込み直しても戻らないので `GameManager.Awake` で 1 に戻している。タイトル画面など別シーンを足すときもどこかで戻すこと
- 注意点: 終わったあとに動かす演出は timeScale を無視する設定にしないと止まる（`WaitForSecondsRealtime`、DOTween の `SetUpdate(true)`）。AccidentMark の拡大縮小は子の `Visual` を名前で探している
- 注意点: 結果画面の文字は英語。日本語にするなら TextMeshPro 用の日本語フォントを用意する
- 注意点: ここまでは転換前（事故でゲームオーバー）の記録。11/16 の転換で、状態は `Playing` / `Finished` の 2 つになり、`gameOverResultDelay` はなくなった（`clearResultDelay` は `resultDelay` に名前を変えた）。事故マークはゲーム中に `accidentMarkDuration` で消え、timeScale を無視しない（12 の実装メモ）
- 動作確認: エディタで Play し、時間切れでクリア、事故で停止→マーク→ゲームオーバー表示、入力でリトライ（リトライ後も車が動く）、終了後にポータルが置けないことをユーザーが確認した（11/5）。WebGL ビルドは未確認
