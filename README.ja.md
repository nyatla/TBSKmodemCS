# TBSK modem for C#


English documente 👉[Readme.md](Readme.md)


C#で実装したTBSKmodemです。Python版の同等のAPIを備えています。

🐓[TBSKmodem](https://github.com/nyatla/TBSKmodem)

APIは概ねPythonと同一です。オーディオインタフェイスは[NAudio](https://github.com/naudio/NAudio)が使用できます。




# ライセンス

本ソフトウェアは、MITライセンスで提供します。ホビー・研究用途では、MITライセンスに従って適切に運用してください。

産業用途では、特許の取り扱いに注意してください。

このライブラリはMITライセンスのオープンソースソフトウェアですが、特許フリーではありません。



# GetStarted

VisualStadioで作成したSolutionがあります。

## ソースコードのセットアップ
サンプルを含めたソースコードは、githubからcloneします。

```
>git clone https://github.com/nyatla/TBSKmodemCS.git
```


## サンプルプログラム

Python版と同等なサンプルプログラムがあります。

### データをwaveファイルに変換
バイナリデータを再生可能な音声信号に変換します。
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step1_modulate/Program.cs

### wavファイルから復調
wavファイルからデータを取り出します。
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step2_demodulate/Program.cs

### バイトデータの変調と復調
バイト値の変調と復調のサンプルです。
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step3_bytedata/Program.cs

### 文字列の変調と復調
文字列の変調と復調のサンプルです。
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step4_text/Program.cs

### マイク入力のテスト
マイク入力が正常に動作するか確認できます。
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step5_microphone/Program.cs
### リアルタイム受信
マイクからリアルタイムに信号を読み取ります。
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step6_realtime_receive/Program.cs
