---
status: 完了
started: 2026-11-24
completed: 2026-11-24
---

# フォント

## 目的

TextMeshPro の標準フォント（LiberationSans）をやめて、ドット絵の見た目に合うピクセルフォントで文字をそろえる。

## 仕様

- 画面の文字はすべて Press Start 2P にする（スコア・コンボ・見逃し・結果画面・タイトル・事故のポップアップ）
  - 例外: タイトルの HELP / CREDITS の文章は Pixelify Sans（21）
- 文字は英語のまま（どちらのフォントにも日本語が入っていない）
- クレジットにフォントのライセンス表記を書く（いつどこに書くかは Notion のタスクで管理する）
  - `Press Start 2P — Copyright 2012 The Press Start 2P Project Authors, SIL Open Font License 1.1`

## 決定事項

- Press Start 2P を使う。先に Pixelify Sans を試したが、見た目がいまいちだった（11/24、ユーザーの判断）
  - Pixelify Sans は説明文に使うかもしれないので、アセットは残しておく
- どちらも SIL Open Font License 1.1。ゲームに入れて配ってよい。TTF の隣に `OFL.txt` を置き、クレジットに表記する
  - Press Start 2P は Reserved Font Name 付き。フォント自体を改変して配るときだけ名前が使えなくなる。TMP の Font Asset にするのは問題ない
- Font Asset の作り方（Font Asset Creator）
  - Sampling Point Size 64（8 ピクセルの格子のフォントなので 8 の倍数）、Padding 8、Atlas 1024×1024、Character Set ASCII、Render Mode SDFAA
  - atlas が小さいと入りきらない文字が出て、その文字だけ標準フォントで表示される（Pixelify Sans を 90 / 512 で作ったときに起きた）
  - Characters missing の 2 文字（U+200B 幅ゼロのスペース、U+25A1 □）はフォントに入っていないだけで、表示には影響しない
- 倍率の記号は引き続き半角の `x`（ASCII で作った Font Asset に `×` は入っていない）
- TMP Settings の Default Font Asset は Pixelify Sans のままにしておく（11/24、ユーザーの判断）
  - 新しく置いた TMP は Pixelify Sans で始まるので、画面に出す文字なら Press Start 2P に変える

## 関連ファイル

- `Assets/_Project/Art/Fonts/PressStart2P/`（TTF・`OFL.txt`・`PressStart2P-Regular SDF.asset`）
- `Assets/_Project/Art/Fonts/PixelifySans/`（TTF・`OFL.txt`・`PixelifySans-Regular SDF.asset`。HELP / CREDITS の文章で使う）
- `Assets/TextMesh Pro/Resources/TMP Settings.asset`（Default Font Asset）
- `Assets/_Project/Scenes/Game.unity` / `Title.unity` / `Assets/_Project/Prefabs/ScorePopup.prefab`

## 未決・TODO

- [x] 文字のはみ出し・位置のずれを見て Font Size を調整する

## 実装メモ

- 変更: コードの変更は `GameUI` のコメントだけ。フォントの差し替えと Font Size の調整は `Game.unity`・`Title.unity`・`ScorePopup.prefab` のインスペクターで行った
- 注意点: Font Asset は Static（ASCII だけを焼いてある）。ASCII 以外の文字（`×`・日本語など）を出すと Default Font Asset（今は Pixelify Sans）にフォールバックし、それもなければ表示されない
- 注意点: Font Asset を作り直すときは、既存のアセットに上書き保存すれば、シーンや Prefab からの参照はそのまま残る
- 注意点: Press Start 2P は等幅で横に広い。同じ Font Size でも LiberationSans より文字列が長くなる
- 動作確認: ユーザーがエディタで再生し、全画面の文字が Press Start 2P になっていること、はみ出しがないことを確認した（WebGL ビルドでは未確認）
