# 描画順（Order in Layer）の帯域

2D の重なり順は、Sorting Layer を `Default` 1 つだけにして、Order in Layer を下の帯域で分けて決める。
新しく画面に出るものを作ったら、まずどの帯域に入るかを決めて、この表に書き足す。

- 理由: Sorting Layer を増やすとエディタでの設定が増える。ジャムの規模なら数値の帯域で十分
- Screen Space の UI（Canvas）はこの表とは別。いつもワールドより手前に出る

## 帯域

| 帯域 | 範囲 | 入れるもの |
|---|---|---|
| 背景 | -300〜-201 | マップの外の背景、背景のヒーロー戦 |
| 地面 | -200〜-101 | Tilemap（地面・道路） |
| 路面 | -100〜-1 | 路面に置くもの（ポータル、ブレーキ跡など） |
| 車 | 0〜99 | 車 |
| 車の上の演出 | 100〜199 | 事故の爆発、ニアミスの火花など |
| マーク | 200〜299 | 車の上に出す記号（復帰マーク、事故予告の「！」） |

- 帯域の先頭（-200, -100, 0, 100, 200）をそのグループの基準にし、同じ帯域の中で前後させたいときは +1, +2… と足す
- 1 つの Prefab の中に絵が複数あるとき（例: 復帰マークのアイコンと残り秒数の文字）は、同じ値にしない。奥のものから +1 ずつ並べる
  - 理由: 同じ値だと、どちらが手前に出るかが決まらず、フレームごとに入れ替わることがある
- マークの中では、事故予告を復帰マークより手前にする
  - 理由: 事故予告のほうが見落とすと困る

## 現在の割り当て

| オブジェクト | 設定する場所 | Order in Layer |
|---|---|---|
| `Grid > Map`（Tilemap） | シーン / Tilemap Renderer | -200 |
| `Portal` | Prefab / `Visual` の Sprite Renderer | -100 |
| `Car` | Prefab / `Visual` の Sprite Renderer | 0 |
| `AccidentMark`（事故の地点） | Prefab / `Visual` の Sprite Renderer | 100 |
| `ReturnMark` のアイコン | Prefab / `Visual` の Sprite Renderer | 200 |
| `ReturnMark` の残り秒数 | Prefab / `TimeText` の TextMeshPro（Extra Settings） | 201 |
| `WarningMark` | Prefab / `Visual` の Sprite Renderer | 210 |
