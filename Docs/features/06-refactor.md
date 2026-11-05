---
status: 進行中
started: 2026-11-05
completed:
---

# 要リファクタ（05 ゲーム進行までの見直し）

## 目的

05 までで 1 プレイ遊べる状態になったので、機能を足す前に、あとで困りそうな箇所を整えておく（2026-11-05 の全体レビューで洗い出した）。

## 仕様

### コード（優先度 高: このあとの機能追加でぶつかる）

- [ ] **スプライトの色を毎フレーム上書きしないようにする**
  - `Car.UpdateBlink`（予告の点滅）と `Portal.UpdateLook`（開く前の半透明）が毎フレーム `sprite.color` を書いている
  - 今のままだと、Visual の色を DOTween で変える演出（フェード・フラッシュなど）が毎フレーム元に戻されて効かない
  - 状態が変わったときだけ色を書く。「元の色を覚えて掛け合わせる」処理が 2 クラスで重複しているので揃える（点滅を DOTween にするのも可）
- [x] **経路の取り方を 1 つにする**
  - `CarSpawner` は自分の子から、`PortalPlacer` は `FindObjectsByType<Route>` でシーン全体から取っていて、出どころが 2 つある
  - 複数レベルや道路封鎖を足すときに食い違わないよう、`CarSpawner` の経路を公開して `PortalPlacer` はそれを使う

### コード（優先度 中: 掃除）

- [x] 使っていないメソッドを消す: `Car.GetFuturePosition`、`Route.GetPosition`、`Route.GetDirection`。`CarSpawner.CurrentInterval` は private にする
- [ ] `GameUI.Update`（残り時間）と `ReturnMark.UpdateText`（残り秒数）の `ToString()` を、表示する値が変わったときだけにする（WebGL での GC によるカクつき対策）
- [ ] `GameManager.ShowAccidentMark` の `transform.Find("Visual")`（名前で探しているので、リネームすると演出が黙って消える）。AccidentMark にスクリプトを付けて、拡大縮小の演出をそちらへ移す

### Editor

- [x] Canvas の Canvas Scaler: Reference Resolution を 800×600 → 960×540（ゲーム画面と同じ 16:9）、Match を 0.5 にする
- [x] `SampleScene` を `Game` にリネームする（Project ウィンドウで F2）。`00-project-setup.md` の関連ファイルも直す
- [ ] （任意）URP テンプレートの残りを消す: `Assets/Settings/Lit2DSceneTemplate.scenetemplate`、`Assets/Settings/Scenes/`
- [ ] `CarSpawner` が `Systems` ではなく `Routes` に付いている。子の経路を拾う仕組みなので Routes に置いたままにし、例外として CLAUDE.md に書き足す

## 決定事項

- 今は直さないもの
  - `GameEnded` で `enabled = false` にする処理が 3 箇所（CarSpawner・PortalPlacer・AccidentPredictor）にあるが、1 行ずつなのでそのまま
  - AccidentPredictor の総当たり（車の数² × 調べる時刻の数）は、ジャムの車の数なら十分軽い
  - `Car.All` / `Portal.All` と static event は、Domain Reload が有効な設定なので問題ない

## 関連ファイル

- `Assets/_Project/Scripts/Car.cs` / `Portal.cs` / `CarSpawner.cs` / `PortalPlacer.cs` / `Route.cs`
- `Assets/_Project/Scripts/GameManager.cs` / `GameUI.cs` / `ReturnMark.cs`
- `Assets/_Project/Prefabs/AccidentMark.prefab`
- `Assets/_Project/Scenes/Game.unity`（Canvas）

## 未決・TODO

- [ ] 予告の点滅を DOTween に置き換えるか（色の上書きを直すときに決める）
