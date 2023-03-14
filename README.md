# TBSK modem for C#


日本語ドキュメント 👉[README.ja.md](README.ja.md)


This is pure C# implementation of TBSKmodem. 

🐓[TBSKmodem](https://github.com/nyatla/TBSKmodem)

The API is largely identical to Python.

Audio interface can use [NAudio](https://github.com/naudio/NAudio).




# License

This software is provided under the MIT license. For hobby and research purposes, use it according to the MIT license.

For industrial applications, be careful with patents.

This library is MIT licensed open source software, but not patent free.



# GetStarted

There are some sample project for visual studios.

## Setup

Clone source codes from Github.


```
>git clone https://github.com/nyatla/TBSKmodemCS.git
```


## Programs

There is a sample programs same as  the Python version.

### Modulation
Modulates binary data into a playable audio signal.
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step1_modulate/Program.cs

### Demodulation
Extract data from wav file.
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step2_demodulate/Program.cs

### Byte data modulation and demodulation
Samples of bytes modulation and demodulation.
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step3_bytedata/Program.cs

###  Text data modulation and demodulation
Samples of texts modulation and demodulation.
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step4_text/Program.cs

### Audio input test
Displays the microphone input level.
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step5_microphone/Program.cs
### Realtime demodulation.
Read the signal in real time from the microphone.
https://github.com/nyatla/TBSKmodemCS/blob/master/windows/TBSKmodem/getstarted/step6_realtime_receive/Program.cs
