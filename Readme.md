# TBSK modem for C#


English documente 👉[Readme.en.md](Readme.en.md)


C#で実装したTBSKmodemです。
🐓[TBSKmodem](https://github.com/nyatla/TBSKmodem)


TBSK (Trait Block Shift Keying) modemは、FFT/IFTTを使わない、低速、短距離の音響通信の実装です。
バイト/ビットストリームの振幅信号への変調、振幅信号からバイト/ビットストリームへの復調ができます。


![preview_tbsk](https://user-images.githubusercontent.com/2483108/194768184-cecddff0-1fa4-4df8-af3f-f16ed4ef1718.gif)


[Youtube](https://www.youtube.com/watch?v=4cB3hWATDUQ)でみる（信号音付きです。）

※Python版のプレビュー


## Python版との差分

APIは概ねPythonと同一です。一部、C#の標準クラスライブラリに適合させるための変更があります。
オーディオインタフェイスはNAudioに対応しています。


## ライセンス

本ソフトウェアは、MITライセンスで提供します。ホビー・研究用途では、MITライセンスに従って適切に運用してください。
産業用途では、特許の取り扱いに注意してください。

このライブラリはMITライセンスのオープンソースソフトウェアですが、特許フリーではありません。

## GetStarted

VisualStadioで作成したSolutionがあります。

### ソースコードのセットアップ
サンプルを含めたソースコードは、githubからcloneします。

```
>git clone https://github.com/nyatla/TBSKmodemCS.git
```


### サンプルプログラムの場所

Python版と同等なGetstartedがあります。TBSKmodem.slnのgetstartedフォルダーを探索してください。


サンプルの説明はpython版を参考にしてください。
[TBSKmodem#サンプルプログラムの場所](https://github.com/nyatla/TBSKmodem#%E3%82%B5%E3%83%B3%E3%83%97%E3%83%AB%E3%83%97%E3%83%AD%E3%82%B0%E3%83%A9%E3%83%A0%E3%81%AE%E5%A0%B4%E6%89%80)


1. step1_modulate データをwaveファイルに変換。
2. step2_demodulate wavファイルから復調。
3. step3_bytedata バイトデータの変調と復調
4. step4_text 文字列の変調と復調
5. step5_microphone マイク入力のテスト
6. step6_realtime_receive リアルタイム送受信

