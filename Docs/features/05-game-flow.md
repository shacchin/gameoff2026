---
status: 完了
started: 2026-11-04
completed: 2026-11-05
---

# ゲーム進行

## 目的

1 プレイを始めて終わらせる。60 秒耐えればクリア、事故 1 回でゲームオーバーにして、すぐ再挑戦できるようにする。

## 仕様

- シーンを開くとすぐにゲームが始まり、残り時間（60 秒、`[SerializeField]`）を画面に表示する
- 残り時間が 0 になったらクリア（ヒーローがヴィランを制圧した、という扱い）
- 事故が起きたらゲームオーバー
  - 車の動きを止め、事故の地点を強調してから結果を表示する
- 結果画面に「クリア」か「ゲームオーバー」を出し、クリックかキー入力でリトライする（シーンを読み込み直す）

## 決定事項

- ゲームの状態（プレイ中／クリア／ゲームオーバー）は 1 つのクラス（GameManager）が持つ
  - 事故は GameManager に事故の位置を渡して知らせる
  - クリア・ゲームオーバーになったことは、イベントで他のクラス（車の出現、ポータル配置、UI）に知らせる
- 終わったら（クリアもゲームオーバーも）`Time.timeScale = 0` で全体を止める（11/4）
  - 理由: 車・ポータル・復帰マークがどれも `Time.deltaTime` で動いているので、1 か所で全部止まる
  - 止めたあとの演出と待ち時間は、timeScale を無視する設定（`WaitForSecondsRealtime`、DOTween の `SetUpdate(true)`）で動かす
- イベントは 2 段にする（11/4）
  - `GameEnded`: 終わった瞬間。車の出現・ポータル配置・事故予告が止まる
  - `ResultShown`: 少し待ってから（ゲームオーバー 1.5 秒・クリア 0.5 秒）。UI が結果画面を出す。この間に事故の地点のマークを拡大縮小させて見せる
- 結果を出してから 0.3 秒はリトライの入力を受け付けない（慌てたクリックで結果を見ずにリトライしないように）
- 画面の文字は英語にする（TextMeshPro の標準フォントに日本語が入っていないため。日本語にするならフォントの用意が必要）
- タイトル画面はプロトタイプでは作らない（α版で追加。WebGL では音を鳴らす前にクリックが必要なので、タイトル画面でその入力を受ける）→ 09 で追加。結果画面の選択（リトライ／タイトルへ）も 09 で変えた

## 関連ファイル

- `Assets/_Project/Scripts/GameManager.cs`
- `Assets/_Project/Scripts/GameUI.cs`
- `Assets/_Project/Prefabs/AccidentMark.prefab`
- `Assets/_Project/Scripts/AccidentPredictor.cs`・`CarSpawner.cs`・`PortalPlacer.cs`（`GameEnded` で止まる）

## 未決・TODO

- [ ] 背景でヒーローとヴィランが戦う演出（作り込みフェーズで検討）

## 実装メモ

- 変更: `GameManager`（Systems）が状態・残り時間・リトライを持ち、`GameUI`（Canvas）は `RemainingTime` を読んで表示し `ResultShown` で結果画面を出すだけ。事故の地点には `AccidentMark` を出す
- 設計判断: 事故は `AccidentPredictor.AccidentHappened` を GameManager が購読して受け取る（03 のイベントをそのまま使い、GameManager への参照を持たせない）
- 設計判断: 止めるのは timeScale に任せつつ、`CarSpawner`・`PortalPlacer`・`AccidentPredictor` は `GameEnded` で `enabled = false` にして「終わった」ことを明示する
- 注意点: `Time.timeScale` はシーンを読み込み直しても戻らないので `GameManager.Awake` で 1 に戻している。タイトル画面など別シーンを足すときもどこかで戻すこと
- 注意点: 終わったあとに動かす演出は timeScale を無視する設定にしないと止まる（`WaitForSecondsRealtime`、DOTween の `SetUpdate(true)`）。AccidentMark の拡大縮小は子の `Visual` を名前で探している
- 注意点: 結果画面の文字は英語。日本語にするなら TextMeshPro 用の日本語フォントを用意する
- 動作確認: エディタで Play し、時間切れでクリア、事故で停止→マーク→ゲームオーバー表示、入力でリトライ（リトライ後も車が動く）、終了後にポータルが置けないことをユーザーが確認した（11/5）。WebGL ビルドは未確認
